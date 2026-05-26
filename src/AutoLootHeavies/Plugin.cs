namespace AutoLootHeavies;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string FeaturesSection  = "── Features ──";
    private const string LocationsSection = "── Locations ──";
    private const string KeybindsSection  = "── Keybinds ──";
    private const string AdvancedSection  = "── Advanced ──";
    private const string UpdatesSection   = "── Updates ──";

    private const float EnergyRequirement = 3f;

    internal static TimestampedLogger Log { get; private set; }

    internal static List<Stockpile> SortedStockpiles { get; } = [];
    internal static float LastBubbleTime { get; set; }
    internal static Coroutine ActiveScan { get; set; }
    internal static ConfigEntry<bool> TeleportToDumpSiteWhenAllStockPilesFull { get; private set; }
    internal static ConfigEntry<Vector3> DesignatedTimberLocation { get; private set; }
    internal static ConfigEntry<Vector3> DesignatedOreLocation { get; private set; }
    internal static ConfigEntry<Vector3> DesignatedStoneLocation { get; private set; }
    internal static ConfigEntry<bool> ImmersionMode { get; private set; }
    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;
    internal static ConfigEntry<int> ScanChunkSize { get; private set; }
    internal static int CachedScanChunkSize;
    internal static ConfigEntry<KeyboardShortcut> SetTimberLocationKeybind { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> SetOreLocationKeybind { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> SetStoneLocationKeybind { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> TeleportToggleKeybind { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    internal static bool InitialFullUpdate { get; set; }

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);
        InitConfiguration();
        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        DebugWarningDialog.Register(MyPluginInfo.PLUGIN_NAME, () => DebugEnabled);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 2);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        ScanChunkSize = LocalizedConfig.Bind(Config, AdvancedSection, "Performance Smoothness", 250, "performance_smoothness", new AcceptableValueRange<int>(50, 2000), order: 1);
        CachedScanChunkSize = ScanChunkSize.Value;
        ScanChunkSize.SettingChanged += (_, _) => CachedScanChunkSize = ScanChunkSize.Value;

        TeleportToDumpSiteWhenAllStockPilesFull = LocalizedConfig.Bind(Config, FeaturesSection, "Teleport To Dump Site When Full", true, "teleport_to_dump_site_when_full", order: 9);
        ImmersionMode = LocalizedConfig.Bind(Config, FeaturesSection, "Immersive Mode", true, "immersive_mode", order: 8);

        DesignatedTimberLocation = LocalizedConfig.Bind(Config, LocationsSection, "Designated Timber Location", new Vector3(-3712.003f, 6144f, 1294.643f), "designated_timber_location", order: 7);
        DesignatedOreLocation = LocalizedConfig.Bind(Config, LocationsSection, "Designated Ore Location", new Vector3(-3712.003f, 6144f, 1294.643f), "designated_ore_location", order: 6);
        DesignatedStoneLocation = LocalizedConfig.Bind(Config, LocationsSection, "Designated Stone Location", new Vector3(-3712.003f, 6144f, 1294.643f), "designated_stone_location", order: 5);

        SetTimberLocationKeybind = LocalizedConfig.Bind(Config, KeybindsSection, "Set Timber Location Keybind", new KeyboardShortcut(KeyCode.Alpha7), "set_timber_location_keybind", order: 4);
        SetOreLocationKeybind = LocalizedConfig.Bind(Config, KeybindsSection, "Set Ore Location Keybind", new KeyboardShortcut(KeyCode.Alpha8), "set_ore_location_keybind", order: 3);
        SetStoneLocationKeybind = LocalizedConfig.Bind(Config, KeybindsSection, "Set Stone Location Keybind", new KeyboardShortcut(KeyCode.Alpha9), "set_stone_location_keybind", order: 2);
        TeleportToggleKeybind = LocalizedConfig.Bind(Config, KeybindsSection, "Toggle Teleport Keybind", new KeyboardShortcut(KeyCode.Alpha6), "toggle_teleport_keybind", order: 1);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 0);
    }

    internal static void WriteLog(string message, bool error = false)
    {
        if (error)
        {
            LogHelper.Error(message);
        }
        else
        {
            LogHelper.Info(message);
        }
    }

    internal static void DropObjectAndNull(BaseCharacterComponent __instance, Item item)
    {
        DropResGameObject.Drop(__instance.tf.position, item,
            __instance.tf.parent,
            __instance.anim_direction,
            3f,
            Random.Range(0, 2), false);

        __instance.SetOverheadItem(null);
    }

    private static (int, int) GetGridLocation()
    {
        const int horizontal = 30;
        const int vertical = 5;
        var tupleList = new List<(int, int)>(horizontal * vertical);

        for (var x = 0; x < vertical; ++x)
        {
            for (var y = 0; y < horizontal; ++y)
            {
                tupleList.Add((x, y));
            }
        }

        var spot = tupleList.RandomElement();
        return spot;
    }

    internal static void ShowLootAddedIcon(Item item)
    {
        var originalSize = item.definition.item_size;
        item.definition.item_size = 1;
        DropCollectGUI.OnDropCollected(item);
        item.definition.item_size = originalSize;
        Sounds.PlaySound("pickup", null, true);
    }

    internal static void TeleportItem(BaseCharacterComponent __instance, Item item)
    {
        var pwo = MainGame.me.player;
        var needEnergy = !ImmersionMode.Value || pwo.IsPlayerInvulnerable() ? 0f : EnergyRequirement;

        if (pwo.energy >= needEnergy)
        {
            pwo.energy -= needEnergy;
            EffectBubblesManager.ShowStackedEnergy(pwo, -needEnergy);

            var loc = GetGridLocation();
            float xAdjustment = loc.Item1 * 75;

            var timber = DesignatedTimberLocation.Value + new Vector3(xAdjustment, 0, 0);
            var ore = DesignatedOreLocation.Value + new Vector3(xAdjustment, 0, 0);
            var stone = DesignatedStoneLocation.Value + new Vector3(xAdjustment, 0, 0);

            var location = item.id switch
            {
                Constants.ItemDefinitionId.Wood => timber,
                Constants.ItemDefinitionId.Ore => ore,
                Constants.ItemDefinitionId.Stone => stone,
                Constants.ItemDefinitionId.Marble => stone,
                _ => MainGame.me.player_pos
            };

            MainGame.me.player.DropItem(item, Direction.IgnoreDirection, location, 0f, false);
            if (DebugEnabled) WriteLog($"Teleporting {item.id} to dump site.");
            __instance.SetOverheadItem(null);
        }
        else
        {
            DropObjectAndNull(__instance, item);

            if (Time.time - LastBubbleTime >= 0.5f)
            {
                LastBubbleTime = Time.time;

                EffectBubblesManager.ShowImmediately(pwo.bubble_pos,
                    GJL.L("not_enough_something", $"(en)"),
                    EffectBubblesManager.BubbleColor.Energy, true, 1f);
                if (DebugEnabled) WriteLog($"Not enough energy to teleport. Dropping.");
            }
        }
    }

    internal static bool TryPutToInventoryAndNull(BaseCharacterComponent __instance, WorldGameObject wgo, List<Item> itemsToInsert)
    {
        var pwo = MainGame.me.player;
        var needEnergy = !ImmersionMode.Value || pwo.IsPlayerInvulnerable() ? 0f : EnergyRequirement;

        if (pwo.energy >= needEnergy)
        {
            wgo.TryPutToInventory(itemsToInsert, out var failed);
            if (failed.Count == 0)
            {
                pwo.energy -= needEnergy;
                EffectBubblesManager.ShowStackedEnergy(pwo, -needEnergy);
                __instance.SetOverheadItem(null);
                return true;
            }
        }
        else if (Time.time - LastBubbleTime > 0.5f)
        {
            LastBubbleTime = Time.time;
            EffectBubblesManager.ShowImmediately(pwo.bubble_pos,
                GJL.L("not_enough_something", $"(en)"),
                EffectBubblesManager.BubbleColor.Energy, true, 1f);
        }

        return false;
    }

    internal struct Constants
    {
        public struct ItemDefinitionId
        {
            public const string Marble = "marble";
            public const string Ore = "ore_metal";
            public const string Stone = "stone";
            public const string Wood = "wood";
        }

        public struct ItemObjectId
        {
            public const string Ore = "mf_ore_1";
            public const string Stone = "mf_stones_1";
            public const string Timber = "mf_timber_1";
        }
    }

    internal static void RemoveStockpile(WorldGameObject wgo)
    {
        var stockpile = SortedStockpiles.Find(a => a.Wgo == wgo);
        if (stockpile != null)
        {
            if (DebugEnabled) WriteLog($"Removed stockpile: location: {stockpile.Location}, type: {stockpile.Type}, distance: {stockpile.DistanceFromPlayer}");
            SortedStockpiles.Remove(stockpile);
        }
        else
        {
            WriteLog($"Error removing stockpile (null??).", error: true);
        }
    }

    private static Vector3 GetLocation(WorldGameObject wgo)
    {
        return new Vector3((float) Math.Ceiling(wgo.pos3.x), (float) Math.Ceiling(wgo.pos3.y),
            (float) Math.Ceiling(wgo.pos3.z));
    }

    private static float GetDistance(WorldGameObject wgo)
    {
        return Vector3.Distance(MainGame.me.player_pos, wgo.pos3);
    }

    private static Stockpile.StockpileType GetStockpileType(WorldGameObject wgo)
    {
        return wgo.obj_id switch
        {
            { } id when id.Contains(Constants.ItemObjectId.Ore) => Stockpile.StockpileType.Ore,
            { } id when id.Contains(Constants.ItemObjectId.Stone) => Stockpile.StockpileType.Stone,
            { } id when id.Contains(Constants.ItemObjectId.Timber) => Stockpile.StockpileType.Timber,
            _ => Stockpile.StockpileType.Unknown,
        };
    }

    internal static bool AddStockpile(WorldGameObject stockpile)
    {
        var exists = SortedStockpiles.Find(a => a.Wgo == stockpile);
        if (exists != null)
        {
            exists.DistanceFromPlayer = GetDistance(stockpile);
            return false;
        }

        var newStockpile = new Stockpile
        (
            GetLocation(stockpile),
            GetStockpileType(stockpile),
            GetDistance(stockpile),
            stockpile
        );

        SortedStockpiles.Add(newStockpile);
        return true;
    }

    internal static bool OverheadItemIsHeavy(Item item)
    {
        return item.id is Constants.ItemDefinitionId.Wood or
            Constants.ItemDefinitionId.Ore or
            Constants.ItemDefinitionId.Stone or
            Constants.ItemDefinitionId.Marble;
    }

    internal static void StartScan()
    {
        if (!MainGame.game_started) return;
        if (ActiveScan != null) MainGame.me.StopCoroutine(ActiveScan);
        ActiveScan = MainGame.me.StartCoroutine(RunFullUpdate());
    }

    internal static IEnumerator RunFullUpdate()
    {
        if (!MainGame.game_started)
        {
            ActiveScan = null;
            yield break;
        }

        var sw = new Stopwatch();
        sw.Start();

        var snapshot = new List<WorldGameObject>(WorldMap._objs);
        var count = snapshot.Count;
        var fresh = new List<Stockpile>(SortedStockpiles.Count);
        var chunkSize = CachedScanChunkSize;

        if (DebugEnabled) WriteLog($"[ALH]: Scanning {count} world objects for stockpiles (chunk size {chunkSize}).");

        for (var i = 0; i < count; i++)
        {
            var wgo = snapshot[i];
            if (wgo != null && StockpileIsValid(wgo) && wgo.data.inventory_size > 0 && !wgo.obj_id.Contains("decor"))
            {
                fresh.Add(new Stockpile(GetLocation(wgo), GetStockpileType(wgo), GetDistance(wgo), wgo));
            }

            if ((i + 1) % chunkSize == 0) yield return null;
        }

        foreach (var existing in SortedStockpiles)
        {
            if (existing.Wgo == null) continue;
            var alreadyIn = false;
            foreach (var f in fresh)
            {
                if (f.Wgo != existing.Wgo) continue;
                alreadyIn = true;
                break;
            }
            if (!alreadyIn) fresh.Add(existing);
        }

        fresh.Sort((x, y) => x.DistanceFromPlayer.CompareTo(y.DistanceFromPlayer));

        SortedStockpiles.Clear();
        SortedStockpiles.AddRange(fresh);

        sw.Stop();
        if (DebugEnabled) WriteLog($"[ALH]: Scan complete - {fresh.Count} stockpiles, {sw.ElapsedMilliseconds}ms wall time.");

        ActiveScan = null;
    }

    internal static void ShowMessage(string msg, Vector3 pos)
    {
        if (GJL.IsEastern())
        {
            MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                SmartSpeechEngine.VoiceID.None, true);
        }
        else
        {
            var newPos = pos;
            if (newPos == Vector3.zero)
            {
                newPos = MainGame.me.player.pos3;
                newPos.y += 125f;
            }

            EffectBubblesManager.ShowImmediately(newPos, msg,
                EffectBubblesManager.BubbleColor.Relation,
                true, 3f);
        }
    }

    internal static bool StockpileIsValid(WorldGameObject wgo)
    {
        return wgo.obj_id.Contains(Constants.ItemObjectId.Timber) ||
               wgo.obj_id.Contains(Constants.ItemObjectId.Ore) ||
               wgo.obj_id.Contains(Constants.ItemObjectId.Stone);
    }

    internal static void WorldGameObjectInteract(WorldGameObject obj)
    {
        if (StockpileIsValid(obj))
        {
            AddStockpile(obj);
        }
    }

    internal static void CheckKeybinds()
    {
        if (SetTimberLocationKeybind.Value.IsUp())
        {
            DesignatedTimberLocation.Value = MainGame.me.player_pos;
            ShowMessage(Lang.Get("DumpTimber"), DesignatedTimberLocation.Value);
        }

        if (SetOreLocationKeybind.Value.IsUp())
        {
            DesignatedOreLocation.Value = MainGame.me.player_pos;
            ShowMessage(Lang.Get("DumpOre"), DesignatedOreLocation.Value);
        }

        if (SetStoneLocationKeybind.Value.IsUp())
        {
            DesignatedStoneLocation.Value = MainGame.me.player_pos;
            ShowMessage(Lang.Get("DumpStone"), DesignatedStoneLocation.Value);
        }

        if (TeleportToggleKeybind.Value.IsUp())
        {
            TeleportToDumpSiteWhenAllStockPilesFull.Value = !TeleportToDumpSiteWhenAllStockPilesFull.Value;
            var state = TeleportToDumpSiteWhenAllStockPilesFull.Value ? "enabled" : "disabled";
            ShowMessage($"Teleport to dump site: {state}", MainGame.me.player.pos3);
        }
    }
}
