namespace RestInPatches.Patches;

[Harmony]
public static class MovementPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MovementComponent), nameof(MovementComponent.UpdateComponent))]
    public static void MovementComponent_UpdateComponent(MovementComponent __instance)
    {
        // While unfocused the game misreads walking NPCs as stuck and cancels their paths.
        // Keep the stuck counter from building up until the window's focused again.
        if (!Application.isFocused)
        {
            __instance._stuck_counter = 0;
        }
    }
}
