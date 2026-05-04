namespace WheresMaVeggies;

public static class Helpers
{
    internal const string ControllerUnlockTechTooltip = "Gardening";
    internal const string MouseUnlockTechTooltip = "Perk: Farmer";

    internal static bool IsReady()
    {
        return !Plugin.RequireFarmerPerk.Value || UnlockedStageOne();
    }

    internal static List<WorldGameObject> FindNearbyObjectsByVector(WorldGameObject obj)
    {
        var ready = IsReady();
        if (!ready)
        {
            if (Plugin.DebugEnabled)
            {
                Plugin.WriteLog($"[FindNearby] seed='{obj.obj_id}' ready=false range=0 matches=0");
            }
            return new List<WorldGameObject>();
        }

        var range = Plugin.CascadeRadius.Value * LazyConsts.GRID_SIZE;
        var seedX = obj.pos3.x;
        var seedY = obj.pos3.y;
        var foundObjects = WorldMap._objs
            .Where(a => a != obj && a.obj_id == obj.obj_id &&
                        Math.Abs(seedX - a.pos3.x) <= range &&
                        Math.Abs(seedY - a.pos3.y) <= range)
            .ToList();

        if (!Plugin.DebugEnabled) return foundObjects;

        Plugin.WriteLog($"[FindNearby] seed='{obj.obj_id}' ready=true range={range} matches={foundObjects.Count}");
        foreach (var obj2 in foundObjects)
        {
            Plugin.WriteLog($"[FindNearby]  └ {obj2.obj_id} at ({obj2.pos3.x:F0}, {obj2.pos3.y:F0})");
        }

        return foundObjects;
    }

    internal static bool UnlockedStageOne()
    {
        return MainGame.me.save.unlocked_techs.Exists(a =>
            a.ToLowerInvariant().Equals(ControllerUnlockTechTooltip.ToLowerInvariant()));
    }
}