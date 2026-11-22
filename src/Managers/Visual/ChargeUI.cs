using HarmonyLib;
using Polytopia.Data;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Polibrary.PolyScript;
using UnityEngine;
using System.Text;


namespace Ancients.Visual;

public static class ChargeUI
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionBar), nameof(InteractionBar.RefreshUnitOptions))]
    private static void InteractionBar_RefreshUnitOptions(InteractionBar __instance)
    {
        if (__instance.unit == null) return;
        
        if 
        (
            (
                __instance.unit.UnitState.HasAbility(Main.Capacitor) || 
                (__instance.unit.UnitState.passengerUnit != null && __instance.unit.UnitState.passengerUnit.HasAbility(Main.Capacitor))
            ) &&
            __instance.unit.UnitState.HasEffect(Main.Charged)
        )
        {
            if (__instance.headerString.Contains(Localization.Get("actionbox.unit." + Main.Charged)))
            {
                Il2Gen.List<UnitEffect> effects = __instance.unit.UnitState.effects; //couldnt be fucked to write this so vibecode ahead

                if (effects == null || effects.Count < 2) return;

                StringBuilder originalSb = new StringBuilder(" (");

                for (int i = 0; i < effects.Count; i++)
                {
                    if (i > 0)
                    {
                        originalSb.Append(", ");
                    }
                    originalSb.Append(Localization.Get("actionbox.unit." + effects[i]));
                }

                originalSb.Append(')');

                string original = originalSb.ToString();

                
                List<UnitEffect> keys = new();
                List<int> counts = new();

                foreach (UnitEffect effect in effects)
                {
                    int count = 1;
                    int idx = keys.IndexOf(effect);
                    if (idx >= 0)
                    {
                        count = counts[idx] + 1;
                        keys.RemoveAt(idx);
                        counts.RemoveAt(idx);
                    }
                    keys.Add(effect);
                    counts.Add(count);
                }

                StringBuilder replacementSb = new StringBuilder(" (");
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i > 0) replacementSb.Append(", ");
                    replacementSb.Append(Localization.Get("actionbox.unit." + keys[i]));
                    if (counts[i] > 1)
                        replacementSb.Append(' ').Append(counts[i]).Append('x');
                }
                replacementSb.Append(')');
                string replacement = replacementSb.ToString();

                int pos = __instance.headerString.IndexOf(original);
                if (pos >= 0)
                {
                    __instance.headerString = __instance.headerString.Remove(pos, original.Length).Insert(pos, replacement);
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tile), nameof(Tile.Render), typeof(MapRenderContext))]
    [HarmonyPatch(typeof(Tile), nameof(Tile.Render), typeof(MapRenderContext), typeof(SkinVisualsTransientData))]
    private static void ChargeVisuals(Tile __instance) //we patch tile cause we only want this to show in game yk
    {
        TileData tile = __instance.data;

        if (tile.unit != null)
        {
            foreach (SkinVisualsReference.VisualPart visualPart in __instance.Unit.skinVisuals.visualParts)
            {
                if (visualPart != null && visualPart.DefaultSpriteName.Contains("ui_") && visualPart.DefaultSpriteName.Contains("charge"))
                {
                    if (visualPart.renderer != null)
                    {
                        if (visualPart.renderer.spriteRenderer != null)
                        {
                            int chargemax = tile.unit.GetMaxCharge();
                            int chargeamount = tile.unit.GetChargeCount();

                            string spritename;

                            if (chargeamount >= chargemax)
                            {
                                spritename = "ui_chargemax";
                            }
                            else
                            {
                                spritename = $"ui_{chargemax}charge{chargeamount}";
                            }
                            visualPart.renderer.spriteRenderer.sprite = PolyMod.Registry.GetSprite(spritename);
                        }
                    }
                }
            }
        }
    }
}