using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary.PolyScript;

namespace Ancients;

public static class ConductionManager
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
    private static bool AttackCommand_Execute(GameState gameState, AttackCommand __instance)
	{
		UnitState attacker = gameState.Map.GetTile(__instance.Origin).unit;
		UnitState defender = gameState.Map.GetTile(__instance.Target).unit;

        if (attacker == null || defender == null) return true;

        if (attacker.HasAbility(Main.Jumpstart))
        {
            CAR.New(out JumpstartAction a);
            a.PlayerId = __instance.PlayerId;
            a.Target = defender.coordinates;
            a.Origin = attacker.coordinates;
            gameState.ActionStack.Add(a);
            return false;
        }
        return true;
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttackAction), nameof(AttackAction.Execute))]
    private static void AttackAction_Execute(GameState state, AttackAction __instance)
	{
        TileData tile = state.Map.GetTile(__instance.Target);
        if (tile.unit.HasEffect(Main.Conductive))
        {
            CAR.New(out UnitLightningAction a);
            a.PlayerId = __instance.PlayerId;
            a.Coordinates = tile.coordinates;
            a.SetDamage = 50;
            state.ActionStack.Add(a);
        }
	}
}