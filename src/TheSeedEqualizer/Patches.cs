namespace TheSeedEqualizer;

[Harmony]
public static class Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameBalance), nameof(GameBalance.LoadGameBalance))]
    public static void GameBalance_LoadGameBalance()
    {
        Helpers.CaptureAndApply();
    }

    [HarmonyPrefix]
    [HarmonyBefore("p1xel8ted.GraveyardKeeper.FasterCraftReloaded")]
    [HarmonyPriority(1)]
    [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.ReallyUpdateComponent))]
    public static void CraftComponent_ReallyUpdateComponent(CraftComponent __instance, ref float delta_time)
    {
        if (__instance?.current_craft == null) return;
        if (!Plugin.BoostGrowSpeedWhenRaining.Value)
        {
            return;
        }
        if (!EnvironmentEngine.me.is_rainy)
        {
            return;
        }

        var craftId = __instance.current_craft.id;
        string[] refugee = ["garden", "planting", "refugee", "grow"];
        var isRefugeePlanting = refugee.All(craftId.Contains);
        var isVineyard = craftId.Contains("vineyard");
        var isPlayerGarden = craftId.StartsWith("garden") && craftId.EndsWith("growing");

        if (isRefugeePlanting || isVineyard || isPlayerGarden)
        {
            if (Plugin.DebugEnabled)
            {
                Helpers.Log($"[RainBoost] doubling delta_time for craft '{craftId}' (refugee={isRefugeePlanting}, vineyard={isVineyard}, playerGarden={isPlayerGarden}, before={delta_time:F3})");
            }
            delta_time *= 2f;
        }
        else
        {
            if (Plugin.DebugEnabled)
            {
                Helpers.Log($"[RainBoost] skip craft '{craftId}': not a recognised garden/vineyard/refugee planting craft");
            }
        }
    }

    // ── Per-instance ledger hooks ───────────────────────────────────────────
    //
    // Spend  = CraftReally postfix gated on __result == true (failed starts
    //          return false and must not be logged as plants).
    // Yield  = ProcessFinishedCraft postfix for craft-driven flows
    //          (zombie/vineyard/refugee). Player gardens harvest by direct
    //          interaction with the *_ready bed and don't go through a craft,
    //          so DropItems postfix covers that path. The two paths can't
    //          double-count: ProcessFinishedCraft's internal wgo.DropItems
    //          call has wgo.obj_id matching the desk/vineyard/refugee bed,
    //          which never matches the "garden_*_ready" filter on DropItems.

    internal static bool IsTrackedPlantCraft(string craftId)
    {
        if (string.IsNullOrEmpty(craftId)) return false;
        // Mirrors the filters used by Helpers.Apply (player gardens via
        // ObjectDefinition; the other three via CraftDefinition).
        if (craftId.StartsWith("garden") && (craftId.Contains("planting") || craftId.EndsWith("growing"))) return true;
        if (craftId.Contains("grow_desk_planting")) return true;
        if (craftId.Contains("grow_vineyard_planting")) return true;
        if (craftId.StartsWith("refugee_garden")) return true;
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.CraftReally))]
    public static void CraftComponent_CraftReally_Postfix(CraftComponent __instance, CraftDefinition craft, int amount, bool __result)
    {
        if (!__result) return;
        if (!Plugin.TrackPlantCycles.Value) return;
        if (craft == null || __instance == null) return;
        if (!IsTrackedPlantCraft(craft.id)) return;
        if (__instance.wgo == null) return;
        if (craft.needs == null || craft.needs.Count == 0) return;

        var seedNeeds = new List<(string id, int qty)>(craft.needs.Count);
        var perCraft = Math.Max(amount, 1);
        foreach (var n in craft.needs)
        {
            if (n?.id == null) continue;
            if (!n.id.Contains("seed")) continue;
            seedNeeds.Add((n.id, n.value * perCraft));
        }
        if (seedNeeds.Count == 0) return;

        try
        {
            Ledger.RecordSpend(__instance.wgo, craft.id, seedNeeds);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Ledger] CraftReally hook failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CraftComponent), nameof(CraftComponent.ProcessFinishedCraft))]
    public static void CraftComponent_ProcessFinishedCraft_Postfix(CraftComponent __instance)
    {
        if (!Plugin.TrackPlantCycles.Value) return;
        if (__instance == null || __instance.wgo == null) return;
        var craft = __instance.current_craft;
        if (craft == null) return;
        if (!IsTrackedPlantCraft(craft.id)) return;
        if (craft.output == null || craft.output.Count == 0) return;

        // Player-garden planting crafts have empty seed output (their harvest
        // comes from the future ready-bed's drop_items, hooked via
        // WorldGameObject.DropItems below). Bail before touching the ledger.
        var seedYields = new List<(string id, int qty)>();
        foreach (var o in craft.output)
        {
            if (o?.id == null) continue;
            if (!o.id.Contains("seed")) continue;
            if (o.value <= 0) continue;
            seedYields.Add((o.id, o.value));
        }
        if (seedYields.Count == 0) return;

        try
        {
            Ledger.RecordHarvest(__instance.wgo, seedYields, craft.id);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Ledger] ProcessFinishedCraft hook failed: {ex.Message}");
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WorldGameObject), nameof(WorldGameObject.DropItems))]
    public static void WorldGameObject_DropItems_Postfix(WorldGameObject __instance, List<Item> items)
    {
        if (!Plugin.TrackPlantCycles.Value) return;
        if (__instance == null || items == null || items.Count == 0) return;
        var objId = __instance.obj_id;
        if (string.IsNullOrEmpty(objId)) return;
        // Strict filter: only player-garden ready-state drops. ProcessFinishedCraft
        // also calls wgo.DropItems internally for craft-driven harvests, but the
        // wgo there is a grow_desk / vineyard / refugee bed whose obj_id doesn't
        // match this pattern, so the craft hook above stays the sole source.
        if (!(objId.StartsWith("garden") && objId.EndsWith("ready"))) return;

        var seedYields = new List<(string id, int qty)>();
        foreach (var it in items)
        {
            if (it?.id == null) continue;
            if (!it.id.Contains("seed")) continue;
            if (it.value <= 0) continue;
            seedYields.Add((it.id, it.value));
        }
        if (seedYields.Count == 0) return;

        try
        {
            Ledger.RecordHarvest(__instance, seedYields);
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Ledger] DropItems hook failed: {ex.Message}");
        }
    }
}
