using Newtonsoft.Json;

namespace TheSeedEqualizer;

// Per-instance plant→harvest ledger. Each plant cycle is keyed by the bed's
// world position (rounded to 1 decimal) so the same bed transitioning through
// planting → growing → ready states keeps a single open record.
//
// Persisted as ledger.json next to the plugin DLL. Disk writes are debounced
// via a hidden DontDestroyOnLoad MonoBehaviour so a flurry of harvests doesn't
// thrash the disk. All file I/O is best-effort — the ledger is observability
// only, never block plant/harvest if writing fails.
public static class Ledger
{
    public sealed class SeedYield
    {
        public string Id;
        public int Qty;
    }

    public sealed class PlantRecord
    {
        public string PositionKey;
        public string Kind;
        public string CraftId;
        public string CropType;
        public string SeedInId;
        public int    SeedInQty;
        public DateTime PlantedAtUtc;

        public List<SeedYield> SeedOut;
        public DateTime? HarvestedAtUtc;
        public int Net;
    }

    public sealed class CropTotals
    {
        public int CyclesCompleted;
        public int SeedsIn;
        public int SeedsOut;
        public int Net;
    }

    public sealed class LedgerFile
    {
        public int Schema = 1;
        public string GeneratedAtUtc;
        public Dictionary<string, PlantRecord> Open = new();
        public List<PlantRecord> Closed = new();
        public Dictionary<string, CropTotals> TotalsByCrop = new();
    }

    private static readonly object Sync = new();
    private static LedgerFile _file;
    private static bool _loaded;
    private static bool _dirty;
    private static DebouncedSaver _saver;

    private static string SavePath
    {
        get
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(dir ?? string.Empty, "ledger.json");
        }
    }

    private static LedgerFile File
    {
        get
        {
            if (!_loaded)
            {
                Load();
            }
            return _file;
        }
    }

    private static void Load()
    {
        try
        {
            if (System.IO.File.Exists(SavePath))
            {
                var json = System.IO.File.ReadAllText(SavePath);
                _file = JsonConvert.DeserializeObject<LedgerFile>(json) ?? new LedgerFile();
                _file.Open ??= new Dictionary<string, PlantRecord>();
                _file.Closed ??= new List<PlantRecord>();
                _file.TotalsByCrop ??= new Dictionary<string, CropTotals>();
            }
            else
            {
                _file = new LedgerFile();
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Ledger] Failed to load {SavePath}: {ex.Message}. Starting with an empty ledger.");
            _file = new LedgerFile();
        }
        _loaded = true;
    }

    private static void Save()
    {
        try
        {
            _file.GeneratedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var json = JsonConvert.SerializeObject(_file, Formatting.Indented);
            System.IO.File.WriteAllText(SavePath, json);
            _dirty = false;
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[Ledger] Failed to write {SavePath}: {ex.Message}. Will retry on next event.");
        }
    }

    public static string PositionKeyFor(WorldGameObject wgo)
    {
        var p = wgo.transform.position;
        return string.Format(CultureInfo.InvariantCulture, "{0:F1},{1:F1}", p.x, p.y);
    }

    public static string CropTypeFromCraftId(string craftId)
    {
        // garden_wheat_planting_1 → wheat
        // grow_desk_planting_carrot_2 → carrot
        // grow_vineyard_planting_grapes_3 → grapes
        // refugee_garden_grow_beet → beet
        if (string.IsNullOrEmpty(craftId)) return craftId;

        const string growDesk = "grow_desk_planting_";
        const string growVineyard = "grow_vineyard_planting_";
        const string refugee = "refugee_garden_grow_";

        if (craftId.StartsWith(growDesk))
        {
            return TrimTrailingTier(craftId.Substring(growDesk.Length));
        }
        if (craftId.StartsWith(growVineyard))
        {
            return TrimTrailingTier(craftId.Substring(growVineyard.Length));
        }
        if (craftId.StartsWith(refugee))
        {
            return TrimTrailingTier(craftId.Substring(refugee.Length));
        }
        if (craftId.StartsWith("garden_"))
        {
            // garden_<crop>_planting_N or garden_<crop>_growing
            var tail = craftId.Substring("garden_".Length);
            var idx = tail.IndexOf("_planting", StringComparison.Ordinal);
            if (idx < 0) idx = tail.IndexOf("_growing", StringComparison.Ordinal);
            return idx > 0 ? tail.Substring(0, idx) : tail;
        }
        return craftId;
    }

    private static string TrimTrailingTier(string s)
    {
        // Strip a trailing "_1", "_2", "_3" tier suffix if present.
        if (s.Length >= 2 && s[s.Length - 2] == '_' && char.IsDigit(s[s.Length - 1]))
        {
            return s.Substring(0, s.Length - 2);
        }
        return s;
    }

    public static string KindFromCraftId(string craftId)
    {
        if (string.IsNullOrEmpty(craftId)) return "unknown";
        if (craftId.Contains("grow_desk_planting")) return "zombie_garden";
        if (craftId.Contains("grow_vineyard_planting")) return "zombie_vineyard";
        if (craftId.StartsWith("refugee_garden")) return "refugee_garden";
        if (craftId.StartsWith("garden")) return "player_garden";
        return "unknown";
    }

    public static void RecordSpend(WorldGameObject wgo, string craftId, IReadOnlyList<(string id, int qty)> seedNeeds)
    {
        if (wgo == null || seedNeeds == null || seedNeeds.Count == 0) return;
        EnsureSaver();

        var key = PositionKeyFor(wgo);
        // Plant crafts have a single seed input. If for some reason there are
        // multiple seed entries (multi-quality combo), sum them and pick the
        // first id as the primary label — preserves total count without
        // forcing a more complex schema for a rare case.
        var totalQty = 0;
        for (var i = 0; i < seedNeeds.Count; i++)
        {
            totalQty += seedNeeds[i].qty;
        }
        var primaryId = seedNeeds[0].id;

        lock (Sync)
        {
            var record = new PlantRecord
            {
                PositionKey   = key,
                Kind          = KindFromCraftId(craftId),
                CraftId       = craftId,
                CropType      = CropTypeFromCraftId(craftId),
                SeedInId      = primaryId,
                SeedInQty     = totalQty,
                PlantedAtUtc  = DateTime.UtcNow,
                SeedOut       = null,
                HarvestedAtUtc = null,
                Net           = 0
            };
            File.Open[key] = record;
            _dirty = true;
            if (Plugin.DebugTracking != null && Plugin.DebugTracking.Value)
            {
                LogHelper.Info($"[Ledger] spend  pos={key} kind={record.Kind} craft={craftId} seed={primaryId} qty={totalQty}");
            }
        }
        _saver?.RequestFlush();
    }

    public static void RecordHarvest(WorldGameObject wgo, IEnumerable<(string id, int qty)> outputs, string fallbackCraftId = null)
    {
        if (wgo == null || outputs == null) return;
        EnsureSaver();

        var key = PositionKeyFor(wgo);
        var seedYields = new List<SeedYield>();
        foreach (var (id, qty) in outputs)
        {
            if (string.IsNullOrEmpty(id) || qty <= 0) continue;
            if (!id.Contains("seed")) continue;
            seedYields.Add(new SeedYield { Id = id, Qty = qty });
        }
        if (seedYields.Count == 0) return;

        lock (Sync)
        {
            File.Open.TryGetValue(key, out var open);

            // Cancel detection: a planting craft cancellation refunds the seed
            // via WorldGameObject.DropItems(craft.needs). If the open record's
            // seed id+qty exactly matches the dropped items (single seed yield,
            // same id, same qty), treat it as a cancel and discard the record
            // without touching totals.
            if (open != null && seedYields.Count == 1
                && seedYields[0].Id == open.SeedInId
                && seedYields[0].Qty == open.SeedInQty)
            {
                File.Open.Remove(key);
                _dirty = true;
                if (Plugin.DebugTracking != null && Plugin.DebugTracking.Value)
                {
                    LogHelper.Info($"[Ledger] cancel pos={key} craft={open.CraftId} seed={open.SeedInId} qty={open.SeedInQty} (refund detected)");
                }
                _saver?.RequestFlush();
                return;
            }

            var totalOut = 0;
            for (var i = 0; i < seedYields.Count; i++)
            {
                totalOut += seedYields[i].Qty;
            }

            PlantRecord closed;
            if (open != null)
            {
                File.Open.Remove(key);
                open.SeedOut = seedYields;
                open.HarvestedAtUtc = DateTime.UtcNow;
                open.Net = totalOut - open.SeedInQty;
                closed = open;
            }
            else
            {
                // Orphan harvest — no matching plant record. Could be a save
                // that predates the mod, or a refugee bed that auto-replanted.
                closed = new PlantRecord
                {
                    PositionKey    = key,
                    Kind           = fallbackCraftId != null ? KindFromCraftId(fallbackCraftId) : "unknown",
                    CraftId        = fallbackCraftId,
                    CropType       = fallbackCraftId != null ? CropTypeFromCraftId(fallbackCraftId) : "unknown",
                    SeedInId       = null,
                    SeedInQty      = 0,
                    PlantedAtUtc   = DateTime.MinValue,
                    SeedOut        = seedYields,
                    HarvestedAtUtc = DateTime.UtcNow,
                    Net            = totalOut
                };
                if (Plugin.DebugTracking != null && Plugin.DebugTracking.Value)
                {
                    LogHelper.Info($"[Ledger] orphan harvest pos={key} craft={fallbackCraftId} totalOut={totalOut}");
                }
            }

            File.Closed.Add(closed);

            if (!File.TotalsByCrop.TryGetValue(closed.CropType ?? "unknown", out var totals))
            {
                totals = new CropTotals();
                File.TotalsByCrop[closed.CropType ?? "unknown"] = totals;
            }
            totals.CyclesCompleted += 1;
            totals.SeedsIn  += closed.SeedInQty;
            totals.SeedsOut += totalOut;
            totals.Net      = totals.SeedsOut - totals.SeedsIn;

            _dirty = true;

            if (Plugin.DebugTracking != null && Plugin.DebugTracking.Value)
            {
                LogHelper.Info($"[Ledger] harvest pos={key} kind={closed.Kind} crop={closed.CropType} in={closed.SeedInQty} out={totalOut} net={closed.Net}");
            }
        }
        _saver?.RequestFlush();
    }

    private static void EnsureSaver()
    {
        if (_saver != null) return;
        var go = new GameObject("~SeedEqualizerLedgerSaver");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        _saver = go.AddComponent<DebouncedSaver>();
    }

    internal static bool ConsumeDirty()
    {
        lock (Sync)
        {
            if (!_dirty) return false;
            Save();
            return true;
        }
    }

    private sealed class DebouncedSaver : MonoBehaviour
    {
        private const float MinIntervalSeconds = 1.0f;
        private float _flushAt = -1f;

        public void RequestFlush()
        {
            if (_flushAt < 0f)
            {
                _flushAt = Time.unscaledTime + MinIntervalSeconds;
            }
        }

        private void Update()
        {
            if (_flushAt < 0f) return;
            if (Time.unscaledTime < _flushAt) return;
            _flushAt = -1f;
            ConsumeDirty();
        }

        private void OnApplicationQuit()
        {
            ConsumeDirty();
        }
    }
}
