namespace SaveNow;

public enum SaveSortMode
{
    RealTime,
    GameTime
}

public enum SaveSortDirection
{
    Descending,
    Ascending
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private const string ModGerryTag = "mod_gerry";

    private const string AdvancedSection      = "── Advanced ──";
    private const string SavingSection        = "── Saving ──";
    private const string UISection            = "── UI ──";
    private const string ControlsSection      = "── Controls ──";
    private const string NotificationsSection = "── Notifications ──";
    private const string ExitingSection       = "── Exiting ──";
    private const string UpdatesSection       = "── Updates ──";

    internal static ConfigEntry<bool> Debug { get; private set; }
    internal static bool DebugEnabled;
    internal static ConfigEntry<int> SaveInterval { get; private set; }
    internal static ConfigEntry<bool> AutoSaveConfig { get; private set; }
    internal static ConfigEntry<bool> NewFileOnAutoSave { get; private set; }
    internal static ConfigEntry<bool> NewFileOnManualSave { get; private set; }
    internal static ConfigEntry<bool> NewFileOnNewDaySave { get; private set; }
    internal static ConfigEntry<bool> BackupSavesOnSave { get; private set; }
    internal static ConfigEntry<bool> SaveGameNotificationText { get; private set; }
    internal static ConfigEntry<bool> ExitToDesktop { get; private set; }
    internal static ConfigEntry<bool> SaveOnExit { get; private set; }
    internal static ConfigEntry<bool> SaveOnNewDay { get; private set; }
    internal static ConfigEntry<int> MaximumSavesVisible { get; private set; }
    internal static ConfigEntry<SaveSortMode> SortMode { get; private set; }
    internal static ConfigEntry<SaveSortDirection> SortDirection { get; private set; }
    internal static ConfigEntry<bool> PinLastPlayedToTop { get; private set; }
    internal static ConfigEntry<string> LastPlayedSlot { get; private set; }
    internal static ConfigEntry<bool> EnableManualSaveControllerButton { get; private set; }
    internal static ConfigEntry<KeyboardShortcut> ManualSaveKeyBind { get; private set; }
    internal static ConfigEntry<string> ManualSaveControllerButton { get; private set; }
    internal static ConfigEntry<bool> CheckForUpdates { get; private set; }

    internal static TimestampedLogger Log { get; private set; }

    internal static Vector3 Pos { get; set; }
    internal static string DataPath { get; set; }
    internal static string SavePath { get; set; }
    internal static bool CanSave { get; set; }
    internal static string CurrentSave { get; set; }
    internal static readonly Dictionary<string, Vector3> SaveLocationsDictionary = new();

    private static readonly string[] TutorialQuests =
    [
        "start",
        "place_body_on_table",
        "place_interrupted",
        "place_interrupted_2",
        "grave_digging",
        "go_to_graveyard",
        "go_to_tavern",
        "go_to_lighthouse",
        "start2",
        "bishop",
        "circular_saw",
        "inquisitor_1",
        "player_repairs_sword",
        "blacksmith"
    ];

    private static readonly List<GJTimer> Timers = [];

    private static WorldGameObject _gerry;
    private static bool _gerryRunning;
    private static Coroutine AutoSaveCoroutine;

    private void Awake()
    {
        Log = new TimestampedLogger(Logger);
        LogHelper.Log = Log;
        Lang.Init(Assembly.GetExecutingAssembly(), Log);
        InitConfiguration();
        UpdateSaveData();
        UpdateChecker.Register(Info, CheckForUpdates);
        SettingsChangeLogger.Register(Config, Log);
        DebugWarningDialog.Register(MyPluginInfo.PLUGIN_NAME, () => DebugEnabled);
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
    }

    private void InitConfiguration()
    {
        Debug = LocalizedConfig.Bind(Config, AdvancedSection, "Debug Logging", false, "debug_logging", order: 100);
        DebugEnabled = Debug.Value;
        Debug.SettingChanged += (_, _) => DebugEnabled = Debug.Value;

        LastPlayedSlot = Config.Bind(AdvancedSection, "Last Played Slot", string.Empty,
            new ConfigDescription("Internal: tracks the last save you played so it can be pinned to the top of the list.", null,
                new ConfigurationManagerAttributes {Browsable = false, IsAdvanced = true, HideDefaultButton = true, ReadOnly = true}));

        AutoSaveConfig = LocalizedConfig.Bind(Config, SavingSection, "Auto Save", true, "auto_save", order: 100);
        AutoSaveConfig.SettingChanged += (_, _) =>
        {
            KillTimers();
            if (AutoSaveConfig.Value)
            {
                StartTimer();
            }
        };

        SaveInterval = LocalizedConfig.Bind(Config, SavingSection, "Save Interval (Minutes)", 10, "save_interval_minutes", new AcceptableValueRange<int>(1, 60), order: 99, dispNamePrefix: "    └ ",
            extra: a => a.ShowRangeAsPercent = false);

        NewFileOnAutoSave = LocalizedConfig.Bind(Config, SavingSection, "New File On Auto Save", false, "new_file_on_auto_save", order: 98, dispNamePrefix: "    └ ");

        SaveOnNewDay = LocalizedConfig.Bind(Config, SavingSection, "Save On New Day", true, "save_on_new_day", order: 90);

        NewFileOnNewDaySave = LocalizedConfig.Bind(Config, SavingSection, "New File On New Day Save", true, "new_file_on_new_day_save", order: 89, dispNamePrefix: "    └ ");

        NewFileOnManualSave = LocalizedConfig.Bind(Config, SavingSection, "New File On Manual Save", true, "new_file_on_manual_save", order: 80);

        BackupSavesOnSave = LocalizedConfig.Bind(Config, SavingSection, "Backup Saves On Save", true, "backup_saves_on_save", order: 70);

        MaximumSavesVisible = LocalizedConfig.Bind(Config, UISection, "Maximum Saves Visible", 20, "maximum_saves_visible", new AcceptableValueRange<int>(0, 100), order: 100,
            extra: a => a.ShowRangeAsPercent = false);

        SortMode = LocalizedConfig.Bind(Config, UISection, "Sort Mode", SaveSortMode.GameTime, "sort_mode", order: 90);

        SortDirection = LocalizedConfig.Bind(Config, UISection, "Sort Direction", SaveSortDirection.Descending, "sort_direction", order: 89);

        PinLastPlayedToTop = LocalizedConfig.Bind(Config, UISection, "Pin Last Played To Top", false, "pin_last_played_to_top", order: 80);

        ManualSaveKeyBind = LocalizedConfig.Bind(Config, ControlsSection, "Manual Save Key Bind", new KeyboardShortcut(KeyCode.K), "manual_save_key_bind", order: 100);

        EnableManualSaveControllerButton = LocalizedConfig.Bind(Config, ControlsSection, "Enable Manual Save Controller Button", false, "enable_manual_save_controller_button", order: 90);

        ManualSaveControllerButton = LocalizedConfig.Bind(Config, ControlsSection, "Manual Save Controller Button",
            Enum.GetName(typeof(GamePadButton), GamePadButton.LT), "manual_save_controller_button",
            new AcceptableValueList<string>(Enum.GetNames(typeof(GamePadButton))), order: 89, dispNamePrefix: "    └ ");

        SaveGameNotificationText = LocalizedConfig.Bind(Config, NotificationsSection, "Save Game Notification Text", false, "save_game_notification_text", order: 100);

        SaveOnExit = LocalizedConfig.Bind(Config, ExitingSection, "Save On Exit", true, "save_on_exit", order: 100);

        ExitToDesktop = LocalizedConfig.Bind(Config, ExitingSection, "Exit To Desktop", false, "exit_to_desktop", order: 90);

        CheckForUpdates = LocalizedConfig.Bind(Config, UpdatesSection, "Check for Updates", true, "check_for_updates", order: 100);
    }

    private static void UpdateSaveData()
    {
        SavePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "SaveBackup");

        if (!Directory.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }

        DataPath = Path.Combine(PlatformSpecific.GetSaveFolder(), "save-locations-savenow.dat");
        LoadSaveLocations();
    }

    internal static void SaveCallback(SaveSlotData slot)
    {
        if (DebugEnabled) WriteLog($"[SaveNow] SaveCallback fired: slot='{slot.filename_no_extension}'");
        LastPlayedSlot.Value = slot.filename_no_extension;
        SaveLocation(false, slot.filename_no_extension);
        GUIElements.me.ShowSavingStatus(false);
    }

    internal static void PerformNewDaySave()
    {
        if (DebugEnabled) WriteLog("[SaveNow] New day save starting");
        MainGame.me.StartCoroutine(PerformNewDaySaveIE());
    }

    private static IEnumerator PerformNewDaySaveIE()
    {
        if (!MainGame.game_started)
        {
            WriteLog("[SaveNow] New day save skipped: no game running");
            yield break;
        }

        if (!CanSave)
        {
            WriteLog("[SaveNow] New day save skipped: player controlled by script");
            yield break;
        }

        if (DungeonState.IsInDungeon)
        {
            WriteLog("[SaveNow] New day save skipped: in dungeon");
            yield break;
        }

        if (NewFileOnNewDaySave.Value)
        {
            var date = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var newSlot = $"newdaysave.{date}".Trim();
            if (DebugEnabled) WriteLog($"[SaveNow] New day saving to new slot '{newSlot}'");
            MainGame.me.save_slot.filename_no_extension = newSlot;
        }
        else
        {
            if (DebugEnabled) WriteLog($"[SaveNow] New day saving to existing slot '{MainGame.me.save_slot.filename_no_extension}'");
        }

        GUIElements.me.ShowSavingStatus(true);
        PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save, SaveCallback);
    }

    internal static IEnumerator PerformManualSave()
    {
        if (EnvironmentEngine.me.IsTimeStopped())
        {
            WriteLog("[SaveNow] Manual save skipped: time is stopped");
            yield break;
        }
        if (!Application.isFocused)
        {
            WriteLog("[SaveNow] Manual save skipped: application not focused");
            yield break;
        }
        if (!CanSave)
        {
            WriteLog("[SaveNow] Manual save skipped: player controlled by script");
            yield break;
        }

        if (DungeonState.IsInDungeon)
        {
            WriteLog("[SaveNow] Manual save skipped: in dungeon");
            Lang.Reload();
            SpawnGerry(Lang.Get("CantSaveHere"));
            yield break;
        }

        if (NewFileOnManualSave.Value)
        {
            var date = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var newSlot = $"manualsave.{date}".Trim();
            if (DebugEnabled) WriteLog($"[SaveNow] Manual saving to new slot '{newSlot}'");
            MainGame.me.save_slot.filename_no_extension = newSlot;
        }
        else
        {
            if (DebugEnabled) WriteLog($"[SaveNow] Manual saving to existing slot '{MainGame.me.save_slot.filename_no_extension}'");
        }

        GUIElements.me.ShowSavingStatus(true);
        PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save, SaveCallback);
    }

    internal static bool TutorialDone()
    {
        if (!MainGame.game_started) return false;

        foreach (var quest in TutorialQuests)
        {
            if (!MainGame.me.save.quests.IsQuestSucced(quest))
                return false;
        }

        return !MainGame.me.save.IsInTutorial();
    }

    private static void ShowMessage(string msg)
    {
        if (GJL.IsEastern())
        {
            MainGame.me.player.Say(msg, null, false, SpeechBubbleGUI.SpeechBubbleType.Think,
                SmartSpeechEngine.VoiceID.None, true);
        }
        else
        {
            var pos = MainGame.me.player.pos3;
            pos.y += 125f;
            EffectBubblesManager.ShowImmediately(pos, msg,
                EffectBubblesManager.BubbleColor.Relation, true, 3f);
        }
    }

    private static void SpawnGerry(string message)
    {
        if (_gerryRunning) return;

        var location = MainGame.me.player_pos;
        location.x -= 75f;

        if (_gerry == null)
        {
            _gerry = WorldMap.SpawnWGO(MainGame.me.world_root.transform, "talking_skull", location);
            GS.AddCameraTarget(_gerry.transform);
            GS.SetPlayerEnable(false, true);
            _gerry.tag = ModGerryTag;
            _gerry.custom_tag = ModGerryTag;
            _gerry.ReplaceWithObject("talking_skull", true);
            _gerry.tag = ModGerryTag;
            _gerry.custom_tag = ModGerryTag;
            _gerryRunning = true;
        }

        GJTimer.AddTimer(0.5f, delegate
        {
            if (_gerry == null)
            {
                GS.AddCameraTarget(MainGame.me.player.transform);
                GS.SetPlayerEnable(true, true);
                return;
            }

            _gerry.Say(message, delegate
            {
                GJTimer.AddTimer(0.25f, delegate
                {
                    if (_gerry == null)
                    {
                        GS.AddCameraTarget(MainGame.me.player.transform);
                        GS.SetPlayerEnable(true, true);
                        return;
                    }

                    _gerry.ReplaceWithObject("talking_skull", true);
                    _gerry.tag = ModGerryTag;
                    _gerry.custom_tag = ModGerryTag;
                    GS.AddCameraTarget(MainGame.me.player.transform);
                    GS.SetPlayerEnable(true, true);
                    _gerry.DestroyMe();
                    _gerry = null;
                    _gerryRunning = false;
                });
            }, null, SpeechBubbleGUI.SpeechBubbleType.Talk, SmartSpeechEngine.VoiceID.Skull);
        });
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


    private static void WriteSavesToFile()
    {
        using var file = new StreamWriter(DataPath, false);
        foreach (var entry in SaveLocationsDictionary)
        {
            var result = entry.Value.ToString().Substring(1, entry.Value.ToString().Length - 2);
            result = result.Replace(" ", "");
            file.WriteLine("{0}={1}", entry.Key, result);
        }

        if (BackupSavesOnSave.Value)
        {
            MainGame.me.StartCoroutine(BackUpSaveDirectory());
        }
    }

    private static IEnumerator BackUpSaveDirectory()
    {
        try
        {
            foreach (var file in Directory.GetFiles(PlatformSpecific.GetSaveFolder()))
            {
                if (!File.Exists(Path.Combine(SavePath, Path.GetFileName(file))))
                {
                    File.Copy(file, Path.Combine(SavePath, Path.GetFileName(file)));
                }
            }
        }
        catch (Exception e)
        {
            WriteLog(e.Message, true);
        }

        yield return true;
    }

    private static void LoadSaveLocations()
    {
        if (!File.Exists(DataPath)) return;

        var lines = File.ReadAllLines(DataPath, Encoding.Default);
        foreach (var line in lines)
        {
            if (!line.Contains('=')) continue;
            var splitLine = line.Split('=');
            var saveName = splitLine[0];
            var tempVector = splitLine[1].Split(',');
            var vectorToAdd = new Vector3(float.Parse(tempVector[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(tempVector[1].Trim(), CultureInfo.InvariantCulture), float.Parse(tempVector[2].Trim(), CultureInfo.InvariantCulture));

            if (!File.Exists(Path.Combine(PlatformSpecific.GetSaveFolder(), saveName + ".dat"))) continue;
            if (!SaveLocationsDictionary.ContainsKey(saveName))
            {
                SaveLocationsDictionary.Add(saveName, vectorToAdd);
            }
        }
    }

    internal static bool SaveLocation(bool menuExit, string saveSlot)
    {
        Lang.Reload();

        Pos = MainGame.me.player.pos3;
        CurrentSave = MainGame.me.save_slot.filename_no_extension;

        SaveLocationsDictionary[CurrentSave] = Pos;

        WriteSavesToFile();

        if (menuExit) return true;

        if (SaveGameNotificationText.Value)
        {
            if (NewFileOnAutoSave.Value || NewFileOnNewDaySave.Value || NewFileOnManualSave.Value)
            {
                ShowMessage(Lang.Get("SaveMessage") + ": " + saveSlot);
            }
            else
            {
                ShowMessage(Lang.Get("SaveMessage"));
            }
        }

        return true;
    }

    internal static void Resize<T>(List<T> list, int size)
    {
        var count = list.Count;
        if (size < count) list.RemoveRange(size, count - size);
    }

    internal static void RestoreLocation()
    {
        LoadSaveLocations();

        var slot = MainGame.me.save_slot.filename_no_extension;
        LastPlayedSlot.Value = slot;
        var homeVector = new Vector3(2841, -6396, -1332);
        var foundLocation = SaveLocationsDictionary.TryGetValue(slot, out var posVector3);
        var pos = foundLocation ? posVector3 : homeVector;
        if (DebugEnabled) WriteLog($"[SaveNow] RestoreLocation: slot='{slot}', found={foundLocation}, pos={pos}");
        MainGame.me.player.PlaceAtPos(pos);

        StartTimer();
    }

    internal static void KillTimers()
    {
        Timers.RemoveAll(a => !a);
        foreach (var timer in Timers)
        {
            if (DebugEnabled) WriteLog($"Timer '{timer.name}' killed");
            timer.Stop();
            timer.DestroyComponent();
        }
        Timers.Clear();
        if (AutoSaveCoroutine != null)
        {
            MainGame.me.StopCoroutine(AutoSaveCoroutine);
            AutoSaveCoroutine = null;
        }
    }

    internal static void StartTimer()
    {
        KillTimers();
        if (AutoSaveConfig.Value)
        {
            var intervalSeconds = SaveInterval.Value * 60;
            if (DebugEnabled) WriteLog($"[SaveNow] Starting auto-save timer: interval={SaveInterval.Value}min ({intervalSeconds}s)");
            var timer = GJTimer.AddTimer(intervalSeconds, AutoSave);
            timer.name = "AutoSaveTimer";
            Timers.Add(timer);
        }
        else
        {
            if (DebugEnabled) WriteLog("[SaveNow] Auto-save is disabled, no timer started");
        }
    }

    private static void AutoSave()
    {
        if (DebugEnabled) WriteLog("[SaveNow] Auto-save timer fired");
        if (AutoSaveCoroutine != null)
        {
            MainGame.me.StopCoroutine(AutoSaveCoroutine);
            AutoSaveCoroutine = null;
        }
        AutoSaveCoroutine = MainGame.me.StartCoroutine(AutoSaveIE());
        StartTimer();
    }

    private static IEnumerator AutoSaveIE()
    {
        if (!MainGame.game_started)
        {
            WriteLog("[SaveNow] Auto-save skipped: no game running");
            yield break;
        }
        if (EnvironmentEngine.me.IsTimeStopped())
        {
            WriteLog("[SaveNow] Auto-save skipped: time is stopped");
            yield break;
        }
        if (!Application.isFocused)
        {
            WriteLog("[SaveNow] Auto-save skipped: application not focused");
            yield break;
        }
        if (!CanSave)
        {
            WriteLog("[SaveNow] Auto-save skipped: player controlled by script");
            yield break;
        }
        if (!NewFileOnAutoSave.Value)
        {
            var slot = MainGame.me.save_slot.filename_no_extension;
            if (DebugEnabled) WriteLog($"[SaveNow] Auto-saving to existing slot '{slot}'");
            PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                delegate
                {
                    if (DebugEnabled) WriteLog($"[SaveNow] Auto-save complete: '{slot}'");
                    SaveLocation(false, slot);
                });
        }
        else
        {
            GUIElements.me.ShowSavingStatus(true);
            var date = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var newSlot = $"autosave.{date}".Trim();
            if (DebugEnabled) WriteLog($"[SaveNow] Auto-saving to new slot '{newSlot}'");

            MainGame.me.save_slot.filename_no_extension = newSlot;
            PlatformSpecific.SaveGame(MainGame.me.save_slot, MainGame.me.save,
                delegate
                {
                    if (DebugEnabled) WriteLog($"[SaveNow] Auto-save complete: '{newSlot}'");
                    SaveLocation(false, newSlot);
                    GUIElements.me.ShowSavingStatus(false);
                });
        }
    }
}
