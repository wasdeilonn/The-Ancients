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
using Il2CppMicrosoft.AspNetCore.Http.Features;


namespace Ancients;

public static class Redirection
{
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(Redirection));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.PerformAttackDefault))]
    private static bool ActionUtils_PerformAttackDefault(GameState gameState, byte playerId, WorldCoordinates origin, WorldCoordinates target, int damage)
    {
        TileData originTile = gameState.Map.GetTile(origin);
        TileData targetTile = gameState.Map.GetTile(target);
        UnitState attacker = originTile.unit;
        UnitState defender = targetTile.unit;

        if (defender == null) return true;
        GameManager.GameState.TryGetPlayer(defender.owner, out var defenderPlayer);

        if (attacker == null) return true;

        if (defender.HasAbility(AMain.Protect) || attacker.owner == defender.owner || attacker.HasActivePeaceTreaty(GameManager.GameState, defenderPlayer)) return true;

        UnitState protector = null;

        foreach (TileData tileNeighbor in gameState.Map.GetTileNeighbors(defender.coordinates))
        {
            if (tileNeighbor == null) continue;
            if (tileNeighbor.unit == null) continue;

            
            if (tileNeighbor.unit.HasAbility(AMain.Protect) && (tileNeighbor.unit.owner == defender.owner || tileNeighbor.unit.HasActivePeaceTreaty(GameManager.GameState, defenderPlayer)))
            {
                if (protector == null)
                {
                    protector = tileNeighbor.unit;
                    continue;
                }
                else
                {
                    if (protector.health < tileNeighbor.unit.health)
                    {
                        protector = tileNeighbor.unit;
                    }
                }
            }
        }

        if (protector == null) return true;

        else
        {
            if (ChargeManager.GetChargeCount(protector) > 0)
            {
                ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
                action.PlayerId = protector.owner;
                action.Coordinates = protector.coordinates;
                action.Positive = false;
                gameState.ActionStack.Add(action);

                ActionUtils.PerformAttackDefault(gameState, playerId, origin, protector.coordinates, 0);
                return false;
            }
            ActionUtils.PerformAttackDefault(gameState, playerId, origin, protector.coordinates, damage);
            return false;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttackReaction), nameof(AttackReaction.Execute))]
    public static bool AttackReaction_Execute(Il2CppSystem.Action onComplete, AttackReaction __instance)
    {
        Tile originTile = MapRenderer.Current.GetTileInstance(__instance.action.Origin);
        Tile targetTile = MapRenderer.Current.GetTileInstance(__instance.action.Target);
        UnitState defender = GameManager.GameState.Map.GetTile(__instance.action.Target).unit;
        UnitState attacker = GameManager.GameState.Map.GetTile(__instance.action.Origin).unit;

        if (defender == null) return true;
        GameManager.GameState.TryGetPlayer(defender.owner, out var defenderPlayer);

        if (attacker == null) return true;

        if (defender.HasAbility(AMain.Protect) || attacker.owner == defender.owner || attacker.HasActivePeaceTreaty(GameManager.GameState, defenderPlayer)) return true;

        UnitState protector = null;

        foreach (TileData tileNeighbor in GameManager.GameState.Map.GetTileNeighbors(defender.coordinates))
        {
            if (tileNeighbor == null) continue;
            if (tileNeighbor.unit == null) continue;

            if (tileNeighbor.unit.HasAbility(AMain.Protect) && (tileNeighbor.unit.owner == defender.owner || tileNeighbor.unit.HasActivePeaceTreaty(GameManager.GameState, defenderPlayer)))
            {
                if (protector == null)
                {
                    protector = tileNeighbor.unit;
                    continue;
                }
                else
                {
                    if (protector.health < tileNeighbor.unit.health)
                    {
                        protector = tileNeighbor.unit;
                    }
                }
            }
        }

        if (protector == null) return true;

        Tile protectorTile = MapRenderer.Current.GetTileInstance(protector.coordinates);

        VFXManager.SizeMappings["redirectpuff"] = 1.5f;
        VFXManager.EnsureCustomPuffRegistered("RedirectPuff", "Puff");
        targetTile.DoPuff("RedirectPuff", targetTile.transform, targetTile.VisualCenterObject.localPosition);

        if (!protectorTile.IsHidden)
        {
            protectorTile.Render();
            protectorTile.SpawnDarkPuff();
            protectorTile.Sway();
            protectorTile.Damage(__instance.action.Damage);
        }

        if (originTile != null && originTile.Unit != null && (!originTile.IsHidden || !targetTile.IsHidden))
        {
            originTile.Unit.Attack(__instance.action.Target, false /*we know it wont so it shouldnt*/, (Il2CppSystem.Action)delegate
            {
                if (targetTile != null && targetTile.Unit != null && !targetTile.IsHidden)
                {
                    targetTile.Damage(__instance.action.Damage);
                    targetTile.RenderUnit();
                    if (!__instance.action.ShouldMoveToTarget)
                    {
                        originTile.RenderUnit();
                    }
                }
                if (__instance.action.ShouldMoveToTarget)
                {
                    onComplete.Invoke();
                }
                else
                {
                    GameManager.DelayCall(__instance.action.Delay, onComplete);
                }
            });
        }
        else
        {
            onComplete.Invoke();
        }
        return false;
    }
}