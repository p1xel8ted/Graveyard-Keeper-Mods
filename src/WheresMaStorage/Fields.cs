namespace WheresMaStorage;

public static class Fields
{
    internal const string Chest = "chest";
    internal const string Gerry = "gerry";
    internal const string Multi = "multi";
    internal const string NpcBarman = "npc_tavern_barman";
    internal const string Player = "player";

    internal const string Storage = "storage";
    internal const string Tavern = "tavern";
    internal const string Vendor = "vendor";
    internal const string Writer = "writer";
    internal const string Soul = "soul_container";
    internal const string Bag = "bag";

    internal const string ShippingBoxTag = "shipping_box";
    internal const string MoreInventorySlotsGuid = "MoreInventorySlots";
    internal static bool DebugMessageShown { get; set; }
    internal static bool MisWarningShown { get; set; }

    internal static readonly string[] ChiselItems =
    [
        "chisel"
    ];

    internal static readonly ItemDefinition.ItemType[] GraveItems =
    [
        ItemDefinition.ItemType.GraveStone, ItemDefinition.ItemType.GraveFence, ItemDefinition.ItemType.GraveCover,
        ItemDefinition.ItemType.GraveStoneReq, ItemDefinition.ItemType.GraveFenceReq, ItemDefinition.ItemType.GraveCoverReq
    ];

    internal static readonly string[] PenPaperInkItems =
    [
        "book", "chapter", "ink", "pen"
    ];

    internal static readonly string[] SinShardItems =
    [
        "sin_shard", "sin_shard_body_part"
    ];

    // Zone-level skip list (used when iterating WorldZones in LoadInventories)
    internal static readonly string[] AlwaysSkipZones = ["bat", "slime", "refugees", "bee", "refugee", "npc_tavern_barman", "soul_container", "box_pallet"];

    internal static bool GameBalanceAlreadyRun { get; set; }
    internal static bool GratitudeCraft { get; set; }
    internal static bool ShrinkDialogOpen { get; set; }
    internal static MultiInventory Mi { get; set; } = new();
    internal static bool UsingBag { get; set; }
    internal static bool ZombieWorker { get; set; }

    internal static readonly string[] ExcludeTheseWildernessInventories =
    [
        "vendor", "npc", "donkey", "zombie", "worker", "refugee", "pile", "carrot", "cooking", "guard", "working", "obj_church"
    ];

    internal static readonly Dictionary<WorldGameObject, MultiInventory> WildernessMultiInventories = new();
    internal static readonly List<Inventory> WildernessInventories = [];

    internal static bool InventoriesLoaded { get; set; }
    public static bool DropsCleaned { get; set; }

    // Debounce flags drained once per frame in Plugin.Update. SettingChanged fires
    // synchronously on every slider tick during a drag, so the heavy Update*() helpers
    // would run hundreds of times mid-frame without these.
    internal static bool InventorySizesDirty { get; set; }
    internal static bool StackSizesDirty { get; set; }
    internal static bool ToolDestroyDirty { get; set; }

    // The world object the player is currently interacting with. Reset when all GUIs close.
    // Used as the context source for WmsSourceKind classification in ShouldForcePersonalOnly etc.
    internal static WorldGameObject CurrentWgoInteraction { get; set; }
}
