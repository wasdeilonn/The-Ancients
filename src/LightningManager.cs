using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary;
using AMain = Ancients.Main;


namespace Ancients;

public static class LightningManager
{
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(LightningManager));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartTurnAction), nameof(StartTurnAction.ExecuteDefault))]
    private static void StartTurn(GameState gameState, StartTurnAction __instance)
	{
		foreach (TileData tile in gameState.Map.tiles)
        {
            if (tile.improvement != null && tile.owner == __instance.PlayerId)
            {
                if (gameState.GameLogicData.GetImprovementData(tile.improvement.type).HasAbility(AMain.Lightning))
                {
                    LightningStrikeAction action = PolibActionManager.MakeIl2CppAction<LightningStrikeAction>();
                    action.PlayerId = __instance.PlayerId;
                    action.Coordinates = tile.coordinates;
                    gameState.ActionStack.Add(action);
                }
            }
        }
	}

    public static int GetLightningStars(ImprovementData.Type imp)
    {
        int i = 0;
        AMain.LightningStars.TryGetValue(imp, out i);
        return i;
    }

    public static int GetLightningPop(ImprovementData.Type imp)
    {
        int i = 0;
        AMain.LightningPop.TryGetValue(imp, out i);
        return i;
    }
}