namespace WheresMaStorage;

internal static class Classifiers
{
    // Inventory/obj_id substrings that disqualify a source from shared-inventory treatment.
    private static readonly string[] AlwaysSkipInventories =
        ["slime", "bat", "refugees", "refugee", "bush_berry", "tree_apple", "bee"];

    // Widget inventory ids that should always be hidden (refugee-camp + hardwired misc).
    private static readonly string[] AlwaysHidePartials =
        ["refugee_camp_well", "refugee_camp_tent", "pump", "pallet", "refugee_camp_well_2"];

    // Widget inventory ids recognised as stockpile widgets.
    private static readonly string[] StockpileIdPartials =
        ["mf_stones", "mf_ore", "mf_timber"];

    // ---- Source --------------------------------------------------------

    public static WmsSourceKind GetSourceKind(WorldGameObject wgo)
    {
        if (wgo == null) return WmsSourceKind.Unknown;
        var marker = wgo.GetComponent<WmsSourceMarker>();
        if (marker != null) return marker.Kind;

        var kind = ClassifySource(wgo);
        marker = wgo.gameObject.AddComponent<WmsSourceMarker>();
        marker.Kind = kind;

        if (Plugin.DebugEnabled)
        {
            Helpers.Log($"[Classify:Source] stamped obj={wgo.obj_id} def={wgo.obj_def?.id} zone={wgo.GetMyWorldZoneId()} → {kind}");
        }

        return kind;
    }

    private static WmsSourceKind ClassifySource(WorldGameObject wgo)
    {
        if (wgo.is_player) return WmsSourceKind.Player;

        var objId = wgo.obj_id ?? string.Empty;
        var objIdLower = objId.ToLowerInvariant();
        var objDefId = wgo.obj_def?.id ?? string.Empty;
        var zoneId = wgo.GetMyWorldZoneId() ?? string.Empty;

        if (MatchesAny(objIdLower, AlwaysSkipInventories) ||
            MatchesAny(objDefId.ToLowerInvariant(), AlwaysSkipInventories) ||
            MatchesAny(zoneId.ToLowerInvariant(), AlwaysSkipInventories))
        {
            return WmsSourceKind.AlwaysSkip;
        }

        if (objIdLower.Contains("barman")) return WmsSourceKind.Barman;
        if (wgo.vendor != null) return WmsSourceKind.Vendor;
        if (objIdLower.Contains(Fields.Soul)) return WmsSourceKind.SoulBox;
        if (objIdLower.Contains("tavern_cellar_rack")) return WmsSourceKind.TavernCellar;
        if (objIdLower.Contains(Fields.Writer)) return WmsSourceKind.WritersTable;
        if (objIdLower.Contains("pulpit")) return WmsSourceKind.ChurchPulpit;

        if (objIdLower.StartsWith("refugee_camp_")) return WmsSourceKind.RefugeeStructure;

        if (wgo.obj_def != null)
        {
            if (wgo.obj_def.interaction_type == ObjectDefinition.InteractionType.Chest)
            {
                return WmsSourceKind.Chest;
            }

            if (wgo.obj_def.has_craft)
            {
                return WmsSourceKind.CraftStation;
            }
        }

        if (objIdLower.StartsWith("mf_")) return WmsSourceKind.Stockpile;
        if (objIdLower.Contains("compost")) return WmsSourceKind.Compost;

        if (zoneId.Contains("zombie_mill")) return WmsSourceKind.ZombieMill;
        if (zoneId.Contains("stone_workyard") || zoneId.Contains("marble_deposit")) return WmsSourceKind.Quarry;
        if (objIdLower.Contains("well")) return WmsSourceKind.Well;

        var zombieWorker = (wgo.has_linked_worker && wgo.linked_worker != null &&
                            wgo.linked_worker.obj_id.ToLowerInvariant().Contains("zombie")) ||
                           objDefId.ToLowerInvariant().Contains("zombie") ||
                           objIdLower.Contains("zombie");
        if (zombieWorker) return WmsSourceKind.ZombieWorker;

        if (wgo.IsWorker() || wgo.IsInvisibleWorker()) return WmsSourceKind.Worker;

        return WmsSourceKind.Unknown;
    }

    // ---- Panel ---------------------------------------------------------

    public static WmsPanelKind GetPanelKind(InventoryPanelGUI panel)
    {
        if (panel == null) return WmsPanelKind.Unknown;
        var marker = panel.GetComponent<WmsPanelMarker>();
        if (marker != null) return marker.Kind;

        var kind = ClassifyPanel(panel);
        marker = panel.gameObject.AddComponent<WmsPanelMarker>();
        marker.Kind = kind;
        return kind;
    }

    // Re-classify a panel regardless of any cached marker.
    // Used by the DoOpening prefix where a fresh classification uses the live interaction context.
    public static WmsPanelKind RefreshPanelKind(InventoryPanelGUI panel)
    {
        if (panel == null) return WmsPanelKind.Unknown;
        var kind = ClassifyPanel(panel);
        var marker = panel.GetComponent<WmsPanelMarker>() ?? panel.gameObject.AddComponent<WmsPanelMarker>();
        var changed = marker.Kind != kind;
        marker.Kind = kind;

        if (Plugin.DebugEnabled)
        {
            Helpers.Log($"[Classify:Panel] {(changed ? "refreshed" : "no-change")} panel={panel.name} → {kind}");
        }

        return kind;
    }

    private static WmsPanelKind ClassifyPanel(InventoryPanelGUI panel)
    {
        // Identity-first: each specific-role panel GameObject is stable.
        var gui = GUIElements.me;
        if (gui != null)
        {
            var vendor = gui.vendor;
            if (vendor != null)
            {
                if (panel == vendor.player_panel) return WmsPanelKind.Player;
                if (panel == vendor.vendor_panel) return WmsPanelKind.Vendor;
            }

            var chest = gui.chest;
            if (chest != null)
            {
                if (panel == chest.player_panel) return WmsPanelKind.Player;
                if (panel == chest.chest_panel) return WmsPanelKind.Chest;
            }

            var inv = gui.inventory;
            if (inv != null)
            {
                if (panel == inv._inventory_panel) return WmsPanelKind.Player;
                if (panel == inv.bag_panel) return WmsPanelKind.Player;
            }

            var resourcePicker = gui.resource_picker;
            if (resourcePicker != null && panel == resourcePicker._inventory_panel)
            {
                return WmsPanelKind.Resource;
            }
        }

        // Name-match fallback for other panels (GraveGUI, MixedCraftGUI, SoulHealerGUI,
        // OrganEnhancerGUI, ResurrectionGUI, etc.) that own their own InventoryPanelGUI fields.
        var panelNameLower = panel.name.ToLowerInvariant();
        var looksVendor = panelNameLower.Contains(Fields.Vendor);
        var looksChest = panelNameLower.Contains(Fields.Chest);
        var looksPlayer = panelNameLower.Contains(Fields.Player) ||
                          (panelNameLower.Contains(Fields.Multi) && !looksChest && !looksVendor);

        if (looksPlayer) return WmsPanelKind.Player;

        var interaction = Fields.CurrentWgoInteraction;
        if (interaction != null && GetSourceKind(interaction).IsVendorLike()) return WmsPanelKind.Vendor;
        if (looksVendor) return WmsPanelKind.Vendor;
        if (looksChest) return WmsPanelKind.Chest;
        if (panelNameLower.Contains("resource")) return WmsPanelKind.Resource;

        return WmsPanelKind.Unknown;
    }

    // ---- Widget --------------------------------------------------------

    public static void StampWidgets(InventoryPanelGUI panel, WmsPanelKind panelKind, WmsSourceKind sourceKind)
    {
        if (panel == null) return;

        foreach (var widget in panel._widgets)
        {
            StampOne(widget, panelKind, sourceKind);
        }

        foreach (var widget in panel._custom_widgets)
        {
            StampOne(widget, panelKind, sourceKind);
        }
    }

    private static void StampOne(BaseInventoryWidget widget, WmsPanelKind panelKind, WmsSourceKind sourceKind)
    {
        if (widget == null) return;

        var marker = widget.GetComponent<WmsWidgetMarker>() ??
                     widget.gameObject.AddComponent<WmsWidgetMarker>();

        marker.Kind = ClassifyWidget(widget.inventory, panelKind);
        marker.ShouldHide = ComputeShouldHide(marker.Kind, panelKind, sourceKind);
    }

    private static WmsWidgetKind ClassifyWidget(Inventory inv, WmsPanelKind panelKind)
    {
        if (inv == null) return WmsWidgetKind.Unknown;

        if (string.Equals(inv.preset, "soul_container_widget", StringComparison.Ordinal))
        {
            return WmsWidgetKind.SoulContainer;
        }

        var id = inv.data?.id ?? string.Empty;
        var idLower = id.ToLowerInvariant();

        if (inv._is_player) return WmsWidgetKind.PersonalInventory;

        if (string.Equals(id, "Tools", StringComparison.Ordinal) ||
            string.Equals(id, "Toolbelt", StringComparison.Ordinal))
        {
            return WmsWidgetKind.Toolbelt;
        }

        if (inv.data != null && inv.data.is_bag) return WmsWidgetKind.Bag;

        if (MatchesAny(idLower, StockpileIdPartials)) return WmsWidgetKind.Stockpile;
        if (idLower.Contains(Fields.Writer)) return WmsWidgetKind.WritersTable;
        if (idLower.Contains(Fields.Soul)) return WmsWidgetKind.SoulContainer;
        if (idLower.Contains(Fields.Tavern)) return WmsWidgetKind.Tavern;
        if (idLower.Contains(Fields.Storage)) return WmsWidgetKind.WarehouseShop;

        if (idLower.Contains("refugee_camp_")) return WmsWidgetKind.RefugeeStructure;
        if (MatchesAny(idLower, AlwaysHidePartials)) return WmsWidgetKind.AlwaysHide;

        if (panelKind == WmsPanelKind.Vendor) return WmsWidgetKind.VendorOffer;

        return WmsWidgetKind.Unknown;
    }

    private static bool ComputeShouldHide(WmsWidgetKind widget, WmsPanelKind panel, WmsSourceKind source)
    {
        // Refugee structures and other always-hide items are always hidden.
        if (widget is WmsWidgetKind.RefugeeStructure or WmsWidgetKind.AlwaysHide) return true;

        // WritersTable widgets are never auto-hidden — they're the writers' table UI.
        if (widget == WmsWidgetKind.WritersTable) return false;

        // Skip config-gated hiding when the panel/source is a kind WMS doesn't interfere with.
        if (panel == WmsPanelKind.Vendor) return false;
        if (source is WmsSourceKind.Barman or WmsSourceKind.TavernCellar or
            WmsSourceKind.SoulBox or WmsSourceKind.ChurchPulpit)
        {
            return false;
        }

        return widget switch
        {
            WmsWidgetKind.SoulContainer => Plugin.HideSoulWidgets.Value,
            WmsWidgetKind.Stockpile     => Plugin.HideStockpileWidgets.Value,
            WmsWidgetKind.Tavern        => Plugin.HideTavernWidgets.Value,
            WmsWidgetKind.WarehouseShop => Plugin.HideWarehouseShopWidgets.Value,
            _                           => false,
        };
    }

    // ---- Shared helpers ------------------------------------------------

    private static bool MatchesAny(string haystack, string[] needles)
    {
        if (string.IsNullOrEmpty(haystack) || needles == null) return false;
        for (var i = 0; i < needles.Length; i++)
        {
            if (haystack.Contains(needles[i])) return true;
        }
        return false;
    }
}

internal static class SourceKindExtensions
{
    public static bool IsVendorLike(this WmsSourceKind k) =>
        k is WmsSourceKind.Vendor or WmsSourceKind.Barman;

    public static bool IsPersonalOnlyTrigger(this WmsSourceKind k) =>
        k is WmsSourceKind.Vendor or WmsSourceKind.Barman or
             WmsSourceKind.TavernCellar or WmsSourceKind.SoulBox or
             WmsSourceKind.Chest or WmsSourceKind.WritersTable;

    public static bool ProceedForSharedInventory(this WmsSourceKind k) =>
        k is WmsSourceKind.ChurchPulpit or WmsSourceKind.CraftStation or
             WmsSourceKind.ZombieWorker or WmsSourceKind.Compost or
             WmsSourceKind.Worker or WmsSourceKind.Player or
             WmsSourceKind.Stockpile;

    public static bool IsAlwaysSkip(this WmsSourceKind k) =>
        k == WmsSourceKind.AlwaysSkip;
}
