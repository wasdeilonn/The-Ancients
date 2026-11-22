using HarmonyLib;
using Polytopia.Data;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Polibrary.PolyScript;
using UnityEngine;


namespace Ancients.Visual;

public static class VFXUtils
{
    public static void SpawnDischargePuff(this Tile tile, float size = 2f)
    {
        VFXManager.SizeMappings["dischargepuff"] = size;
        VFXManager.EnsureCustomPuffRegistered("DischargePuff", "Puff");
        tile.DoPuff("DischargePuff", tile.transform, tile.VisualCenterObject.localPosition);
    }

    public static void SpawnDischargePuffLarge(this Tile tile, float size = 4f)
    {
        VFXManager.SizeMappings["dischargepufflarge"] = 4f;
        VFXManager.EnsureCustomPuffRegistered("DischargePuffLarge", "Puff");
        tile.DoPuff("DischargePuffLarge", tile.transform, tile.VisualCenterObject.localPosition);
    }

    public static void SpawnDischargeBlast(this Tile tile, float size = 2f)
    {
        VFXManager.SizeMappings["dischargeblast"] = size;
        VFXManager.EnsureCustomPuffRegistered("DischargeBlast", "Blast");
        tile.DoPuff("DischargeBlast", tile.transform, tile.VisualCenterObject.localPosition);
    }

    public static void SpawnChargePuff(this Tile tile)
    {
        VFXManager.EnsureCustomPuffRegistered("ChargePuff", "Puff");
        tile.DoPuff("ChargePuff", tile.transform, tile.VisualCenterObject.localPosition);
    }

    public static void SpawnLightning(this Tile tile, float size = 6f)
    {
        VFXManager.SizeMappings["lightning"] = size;
        VFXManager.FadeInOutAnimOverrideMappings["lightning"] = new UnityEngine.Vector2(0.1f, 1f);
        VFXManager.EnsureCustomPuffRegistered("Lightning", "Puff");
        tile.DoPuff("Lightning", tile.transform, tile.VisualCenterObject.localPosition);
    }
}