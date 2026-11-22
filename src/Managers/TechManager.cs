using HarmonyLib;
using Polytopia.Data;
using UnityEngine.EventSystems;

namespace Ancients;

public static class TechManager
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ResearchCommand), nameof(ResearchCommand.IsValid))]
    private static void ResearchCommand_IsValid(ref bool __result, ResearchCommand __instance, GameState state, string validationError)
    {
        if(!__result)
            return;

        if(!state.TryGetPlayer(__instance.PlayerId, out PlayerState playerState))
            return;

        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null)
            return;

        if ((bool)hasFreeTech) __result = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechItem), nameof(TechItem.RefreshState))]
    private static void TechItem_Refresh(TechItem __instance, bool forceUnavaliable = false)
    {
        if(__instance.state == TechItem.State.Unavailable || __instance.state == TechItem.State.Complete)
            return;

        GameState state = GameManager.GameState;
        if(state == null)
            return;

        PlayerState playerState = GameManager.LocalPlayer;
        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null || !(bool)hasFreeTech)
            return;
        __instance.resourceWidget.gameObject.SetActive(false);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.GetTechPrice))]
	private static void GameLogicData_GetTechPrice(ref int __result, TechData techData, PlayerState playerState, GameState state)
    {
        bool? hasFreeTech = HasFreeTech(state, playerState);
        if(hasFreeTech == null || !(bool)hasFreeTech)
            return;

        __result = 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.LearnTech))]
    private static void ActionUtils_LearnTech(GameState gameState, PlayerState playerState, TechData.Type type, int cost, bool shouldUseActions)
    {
        bool? hasFreeTech = HasFreeTech(gameState, playerState);
        if(hasFreeTech == null)
            return;

        if(!(bool)hasFreeTech)
        {
            return;
        }
        TileData capital = gameState.Map.GetTile(playerState.startTile);
        if(capital.improvement == null || !capital.HasImprovement(ImprovementData.Type.City))
        {
            Main.modLogger.LogError("Capital city wasnt found. The fuck?"); //you asking me?
            return;
        }

        if(capital.improvement.rewards == null)
        {
            capital.improvement.rewards = new();
            return;
        }

        capital.improvement.rewards.Remove(Main.InternalTech);
    }

    private static bool? HasFreeTech(GameState state, PlayerState playerState)
    {
        if(playerState == null)
            return null;

        if(!state.GameLogicData.TryGetData(playerState.tribe, out TribeData tribeData))
            return null;

        if(!tribeData.HasAbility(Main.Techless))
            return null;

        TileData capital = state.Map.GetTile(playerState.startTile);
        if(capital.improvement == null || !capital.HasImprovement(ImprovementData.Type.City))
        {
            Main.modLogger.LogInfo("Capital city wasnt found. The fuck?");
            return null;
        }

        return capital.improvement.HasReward(Main.InternalTech);
    }
}