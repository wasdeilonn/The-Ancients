using HarmonyLib;
using Polytopia.Data;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Polibrary.PolyScript;
using UnityEngine;
using wLib;


namespace Ancients.Visual;

public static class Glow
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tile), nameof(Tile.Render), typeof(MapRenderContext))]
    [HarmonyPatch(typeof(Tile), nameof(Tile.Render), typeof(MapRenderContext), typeof(SkinVisualsTransientData))]
    private static void TileEffects(Tile __instance)
    {
        TileData tile = __instance.data;

        if (tile.unit != null)
        {
            foreach (SkinVisualsReference.VisualPart visualPart in __instance.Unit.skinVisuals.visualParts)
            {
                if (visualPart != null && !visualPart.DefaultSpriteName.Contains("ui_"))
                {
                    if (visualPart.renderer != null)
                    {
                        if (visualPart.renderer.spriteRenderer != null)
                        {
                            if (__instance.data.unit.HasEffect(Main.Hammered))
                            GlobalTint.Register(visualPart.renderer.spriteRenderer);
                            else
                            GlobalTint.Unregister(visualPart.renderer.spriteRenderer);
                        }
                    }
                }
            }
        }

        if (tile.HasTesla())
        {
            if (!tile.GetNeighbor(GridDirection.N, GameManager.GameState).HasTesla())
            {
                __instance.border.northBorder.gameObject.SetActive(true);
            }
            GlobalTint.Register(__instance.border.northBorder);

            if (!tile.GetNeighbor(GridDirection.E, GameManager.GameState).HasTesla())
            {
                __instance.border.eastBorder.gameObject.SetActive(true);
            }
            GlobalTint.Register(__instance.border.eastBorder);

            if (!tile.GetNeighbor(GridDirection.W, GameManager.GameState).HasTesla())
            {
                __instance.border.westBorder.gameObject.SetActive(true);
            }
            GlobalTint.Register(__instance.border.westBorder);

            if (!tile.GetNeighbor(GridDirection.S, GameManager.GameState).HasTesla())
            {
                __instance.border.southBorder.gameObject.SetActive(true);
            }
            GlobalTint.Register(__instance.border.southBorder);
        }
        else
        {
            GlobalTint.Unregister(__instance.border.northBorder);
            GlobalTint.Unregister(__instance.border.eastBorder);
            GlobalTint.Unregister(__instance.border.westBorder);
            GlobalTint.Unregister(__instance.border.southBorder);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BorderContainer), nameof(BorderContainer.SetColor))]
    private static void AfterSetColor(BorderContainer __instance)
    {
        GlobalTint.Rebase(__instance.northBorder);
        GlobalTint.Rebase(__instance.eastBorder);
        GlobalTint.Rebase(__instance.southBorder);
        GlobalTint.Rebase(__instance.westBorder);
    }

    public static bool HasTesla(this TileData tile)
    {
        if (tile == null) return false;

        if (tile.rulingCityCoordinates == WorldCoordinates.NULL_COORDINATES) return false;

        TileData cityTile = GameManager.GameState.Map.GetTile(tile.rulingCityCoordinates);

        if (!cityTile.HasImprovement(ImprovementData.Type.City)) return false;
        
        return cityTile.improvement.HasReward(Main.TeslaTower);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PolytopiaSpriteRenderer), nameof(PolytopiaSpriteRenderer.ReturnToPool))]
    private static void OnReturn(PolytopiaSpriteRenderer __instance)
    {
        GlobalTint.Unregister(__instance); //we need this guy to unregister stuff so it doesnt affect pooled stuff
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BorderContainer), nameof(BorderContainer.Render))]
    private static void AfterBorderRender(BorderContainer __instance)
    {
        GlobalTint.Reassert(__instance.northBorder);
        GlobalTint.Reassert(__instance.eastBorder);
        GlobalTint.Reassert(__instance.southBorder);
        GlobalTint.Reassert(__instance.westBorder);
    }
}