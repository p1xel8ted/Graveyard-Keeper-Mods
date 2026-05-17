namespace RestInPatches.Patches;

// Stacked drops with non-trigger colliders are pushed apart by the physics resolver every
// frame. At the quarry that drifts stones, marble and ore across the map. At the elevator
// it does the same to crates. Flipping the collider to a trigger keeps queries/pickup
// working but stops the per-frame separation cascade.
[Harmony]
public static class DropColliderPatches
{
    private static readonly HashSet<string> QuarryResourceIds =
    [
        "stone",
        "marble",
        "ore_metal"
    ];

    // Cache the original isTrigger value the first time we touch a collider so a later
    // revert restores whatever was actually there, not just a hardcoded false. CWT entries
    // are GC'd when the collider is destroyed, so collected drops don't leak.
    private sealed class OriginalState
    {
        public bool IsTrigger;
    }
    private static readonly ConditionalWeakTable<Collider2D, OriginalState> OriginalTriggers = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DropResGameObject), nameof(DropResGameObject.DoDrop), typeof(Vector3), typeof(Item), typeof(Transform), typeof(Direction), typeof(float), typeof(int), typeof(bool))]
    public static void DropResGameObject_DoDrop_Postfix(Item item, DropResGameObject __result)
    {
        if (__result == null || item == null) return;
        if (!ShouldBeTrigger(item)) return;
        SetColliderTrigger(__result, true);
    }

    // Called from the Apply Trigger To All Drops SettingChanged subscriber - walk every
    // live drop and re-apply target state so toggling is live in both directions.
    internal static void ReapplyAllExistingDrops()
    {
        var list = DropsList._instance;
        if (list == null || list.drops == null) return;
        foreach (var drop in list.drops)
        {
            if (!drop) continue;
            var item = drop.res;
            if (item == null) continue;
            if (ShouldBeTrigger(item))
            {
                SetColliderTrigger(drop, true);
            }
            else
            {
                RevertColliderTrigger(drop);
            }
        }
    }

    // Quarry items and crates are always triggers (they're the two known repro cases).
    // Anything else only becomes a trigger when the player opts in via config.
    private static bool ShouldBeTrigger(Item item)
    {
        if (item == null) return false;
        if (QuarryResourceIds.Contains(item.id)) return true;
        if (item.definition != null && item.definition.is_crate) return true;
        return Plugin.ApplyTriggerToAllDrops.Value;
    }

    private static void SetColliderTrigger(DropResGameObject drop, bool isTrigger)
    {
        if (!drop) return;
        var cap = drop.GetComponent<CapsuleCollider2D>();
        if (cap)
        {
            ApplyToCollider(cap, isTrigger);
        }
        var circ = drop.GetComponent<CircleCollider2D>();
        if (circ)
        {
            ApplyToCollider(circ, isTrigger);
        }
    }

    private static void RevertColliderTrigger(DropResGameObject drop)
    {
        if (!drop) return;
        var cap = drop.GetComponent<CapsuleCollider2D>();
        if (cap)
        {
            RestoreCollider(cap);
        }
        var circ = drop.GetComponent<CircleCollider2D>();
        if (circ)
        {
            RestoreCollider(circ);
        }
    }

    private static void ApplyToCollider(Collider2D col, bool isTrigger)
    {
        if (!OriginalTriggers.TryGetValue(col, out _))
        {
            OriginalTriggers.Add(col, new OriginalState { IsTrigger = col.isTrigger });
        }
        col.isTrigger = isTrigger;
    }

    private static void RestoreCollider(Collider2D col)
    {
        // No cache entry = we never modified this collider, so there's nothing to restore.
        if (!OriginalTriggers.TryGetValue(col, out var orig)) return;
        col.isTrigger = orig.IsTrigger;
    }
}
