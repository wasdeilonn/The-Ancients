using HarmonyLib;
using Polytopia.Data;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Polibrary.PolyScript;
using UnityEngine;


namespace Ancients.Visual;

public static class House
{
    const int IndexBump = 3400;
    const int HOUSE_TESLATOWER = 9 + IndexBump; //3409, just a random number to confuse you
    static Dictionary<CityRenderer, bool> HasTesla = new();
    static bool prepost_dataChanged = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CityRenderer), nameof(CityRenderer.RefreshCity))]
    private static void GetDataChanged(CityRenderer __instance)
    {
        prepost_dataChanged = __instance.dataChanged;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CityRenderer), nameof(CityRenderer.RefreshCity))]
    private static void RenderHouses(CityRenderer __instance)
    {
        if (!prepost_dataChanged) return;

        if (HasTesla.TryGetValue(__instance, out var hasTesla))
        {
            if (hasTesla)
            {
                int num = 4;
                if (__instance.Level >= 2)
                {
                    num = 9;
                }
                if (__instance.Level >= 5)
                {
                    num = 16;
                }
                int index = 3; //idfc i cant care about calculating an actual random starting index
                CityPlot nextRandomPlot = __instance.GetNextRandomPlot(ref index, num);
                PolytopiaSpriteRenderer house = __instance.GetHouse(__instance.Tribe, HOUSE_TESLATOWER, __instance.SkinType);
                nextRandomPlot.AddHouse(house);

                __instance.UpdateSpriteRenderers();
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(City), nameof(City.UpdateObject))]
    public static void UpdateCity(City __instance)
    {
        if (__instance.Tile.IsHidden) return;

        if (__instance.Owner != null)
        {
            if (__instance.cityRenderer != null)
            {
                bool has = __instance.HasReward(Main.TeslaTower);
                HasTesla.TryGetValue(__instance.cityRenderer, out var prev);
                if (prev != has)
                {
                    HasTesla[__instance.cityRenderer] = has;
                    __instance.cityRenderer.dataChanged = true;
                }
            }
        }
    }
}