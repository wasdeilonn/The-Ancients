using HarmonyLib;
using Polibrary.PolyScript;
using Polytopia.Data;

namespace Ancients;

public static class LightningManager
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartTurnAction), nameof(StartTurnAction.ExecuteDefault))]
    private static void StartTurn(GameState gameState, StartTurnAction __instance)
	{
		foreach (TileData tile in gameState.Map.tiles)
        {
            if (tile.improvement != null && tile.owner == __instance.PlayerId)
            {
                if (gameState.GameLogicData.GetImprovementData(tile.improvement.type).HasAbility(Main.ImprovementLightning))
                {
                    CAR.New(out ImprovementLightningAction action);
                    action.PlayerId = __instance.PlayerId;
                    action.Coordinates = tile.coordinates;
                    gameState.ActionStack.Add(action);
                }
            }
        }
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.CalculateImprovementLevel))]
    private static void ActionUtils_CalculateImprovementLevel(ref int __result, GameState gameState, TileData tile)
    {
		if (!gameState.TryGetPlayer(tile.owner, out PlayerState playerState)
            || tile.improvement == null
            || !gameState.GameLogicData.TryGetData(tile.improvement.type, out ImprovementData improvementData)
            || !improvementData.HasAbility(Main.PowerStorage))
		{
            return;
		}

		int num = 0;
        if(tile.effects == null)
            tile.effects = new();

        if(!tile.HasEffect(Main.PowerStored))
            return;

        foreach(var tileEffect in tile.effects)
        {
            if(tileEffect == Main.PowerStored)
                num++;
        }
		int val = improvementData.MaxLevel(playerState, gameState);
		if (improvementData.maxLevel > 0)
			num = Math.Min(num, val);

		__result = num;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ImprovementLevelUpAction), nameof(ImprovementLevelUpAction.ExecuteDefault))]
    private static void ImprovementLevelUpAction_ExecuteDefault(ImprovementLevelUpAction __instance, GameState state)
    {
        TileData tile = state.Map.GetTile(__instance.Coordinates);

		if (!state.TryGetPlayer(tile.owner, out PlayerState playerState)
            || tile.improvement == null
            || !state.GameLogicData.TryGetData(tile.improvement.type, out ImprovementData improvementData)
            || !improvementData.HasAbility(Main.PowerStorage))
		{
            return;
		}

        if(tile.effects == null)
            tile.effects = new();

        if(!tile.HasEffect(Main.PowerStored))
            return;

		if (tile.improvement.level != improvementData.MaxLevel(playerState, state))
            return;

        var filteredEffects = new Il2CppSystem.Collections.Generic.List<TileData.EffectType>();
        foreach (TileData.EffectType effect in tile.effects)
        {
            if (effect != Main.PowerStored)
                filteredEffects.Add(effect);
        }

        tile.effects = filteredEffects;
        tile.improvement.level = 1;
        state.ActionStack.Add(new IncreasePopulationAction(__instance.PlayerId, tile.coordinates, tile.rulingCityCoordinates, 60));
        state.ActionStack.Add(new IncreasePopulationAction(__instance.PlayerId, tile.coordinates, tile.rulingCityCoordinates, 60));
    }

}