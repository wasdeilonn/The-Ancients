using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;
using Newtonsoft.Json.Linq;
using Polibrary;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq;
using MS.Internal.Xml.XPath;
using PolytopiaBackendBase.Common;
using System.Data;
using Steamworks.Data;
using Il2CppSystem;
using System.Timers;
using Il2CppMono.Security.Interface;
using Polibrary.Parsing;
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ImprovementLevelUpAction), nameof(ImprovementLevelUpAction.IsValid))]
    private static void LvlUpFix(GameState state, ImprovementLevelUpAction __instance, ref bool __result)
	{
        TileData tile = state.Map.GetTile(__instance.Coordinates);
		if (tile == null) return;
        if (tile.improvement == null) return;

        if (!state.GameLogicData.TryGetData(tile.improvement.type, out var data))
        {
            AMain.modLogger.LogError("Nice one dumbfuck");
            return;
        }

        if (data.HasAbility(AMain.Electric) && tile.improvement.level <= data.maxLevel)
        {
            __result = true;
        }
	}

    public static int GetLightningStars(ImprovementData.Type imp)
    {
        int i = 0;
        AMain.LightningStars.TryGetValue(imp, out i);
        return i;
    }

    public static bool GetLightningGrow(ImprovementData.Type imp)
    {
        bool b = false;
        AMain.LightningGrow.TryGetValue(imp, out b);
        return b;
    }
}