using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;
using PolytopiaBackendBase.Common;
using Polibrary;
using wLib;
using Ancients.Visual;

namespace Ancients;

public static class Main
{
    public static ManualLogSource modLogger;
    public static void Load(ManualLogSource logger)
    {
        logger.LogMessage("bitch");
        modLogger = logger;

        Harmony.CreateAndPatchAll(typeof(ChargeUI));
        Harmony.CreateAndPatchAll(typeof(Glow));
        Harmony.CreateAndPatchAll(typeof(House));
        Harmony.CreateAndPatchAll(typeof(ChargeManager));
        Harmony.CreateAndPatchAll(typeof(ConductionManager));
        Harmony.CreateAndPatchAll(typeof(LightningManager));
        Harmony.CreateAndPatchAll(typeof(PushManager));
        Harmony.CreateAndPatchAll(typeof(RedirectionManager));
        Harmony.CreateAndPatchAll(typeof(TechManager));
        Harmony.CreateAndPatchAll(typeof(UnitAbilityManager));
        Harmony.CreateAndPatchAll(typeof(CityRewardManager));
    }
    
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
    public static void Register(Newtonsoft.Json.Linq.JObject rootObject)
    {
        Ancients.Get("ancients");

        Techless.Get("anc_techless");

        Charge.Get("anc_chargeability");
        Discharge.Get("anc_discharge");
        Capacitor.Get("anc_capacitor");
        Jumpstart.Get("anc_jumpstart");
        Protect.Get("anc_protect");
        Push.Get("anc_push");
        UnitLightning.Get("anc_unitlightning");

        Charged.Get("anc_chargeeffect");
        Conductive.Get("anc_conductive");
        Hammered.Get("anc_hammercharged");

        PowerStored.Get("anc_powerstored");

        ImprovementLightning.Get("anc_improvementlightning");
        PowerStorage.Get("anc_powerstorage");

        TeslaTower.Get("anc_teslatower");
        Hammer.Get("anc_hammer");
        InternalTech.Get("anc_internaltech");
        Manuscript.Get("anc_freetech");

        LightningSFX.Get("lightning");
        RailgunSFX.Get("railgun");
        LowSFX.Get("low");
        HighSFX.Get("high");
        
        PolibUtils.ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "maxCharge", MaxCharge);
        PolibUtils.ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "chargeConsumptionAmount", ChargeConsumptionAmount);
        PolibUtils.ParsePerEach<ImprovementData.Type, int>(rootObject, "improvementData", "lightningStars", LightningStars);
        PolibUtils.ParsePerEach<ImprovementData.Type, int>(rootObject, "improvementData", "lightningPop", LightningPop);
        PolibUtils.ParseListPerEach<UnitData.Type, string>(rootObject, "unitData", "chargeConsumptionEvent", ChargeConsumptionEvent);
        PolibUtils.ParseListPerEach<UnitData.Type, string>(rootObject, "unitData", "chargeBuff", ChargeBuff);
    }

	public static Dictionary<UnitData.Type, int> MaxCharge = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, int> ChargeConsumptionAmount = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, List<string>> ChargeConsumptionEvent = new Dictionary<UnitData.Type, List<string>>();
    public static Dictionary<UnitData.Type, List<string>> ChargeBuff = new Dictionary<UnitData.Type, List<string>>();
    public static Dictionary<ImprovementData.Type, int> LightningStars = new();
    public static Dictionary<ImprovementData.Type, int> LightningPop = new();

    public static TribeType Ancients;

    public static TribeAbility.Type Techless;

    public static UnitAbility.Type Charge;
    public static UnitAbility.Type Discharge;
    public static UnitAbility.Type Capacitor;
    public static UnitAbility.Type Jumpstart;
    public static UnitAbility.Type Protect;
    public static UnitAbility.Type Push;
    public static UnitAbility.Type UnitLightning;

    public static UnitEffect Charged;
    public static UnitEffect Conductive;
    public static UnitEffect Hammered;

    public static TileData.EffectType PowerStored;

    public static ImprovementAbility.Type ImprovementLightning;
    public static ImprovementAbility.Type PowerStorage;

    public static CityReward TeslaTower;
    public static CityReward Hammer;
    public static CityReward Manuscript;
    public static CityReward InternalTech;

    public static SFXTypes LightningSFX;
    public static SFXTypes RailgunSFX;
    public static SFXTypes LowSFX;
    public static SFXTypes HighSFX;


    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.KillUnit))]
    private static bool ActionUtils_KillUnit_NullCheck(GameState gameState, TileData tile) //why does this not have a nullcheck ingame??
    {
        if (tile.unit == null) return false;

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartMatchAction), nameof(StartMatchAction.Execute))]
    private static void PolytopiaCityName(GameState gameState)
    {
        foreach (PlayerState player in gameState.PlayerStates)
        {
            if (player.tribe == Ancients)
            {
                gameState.GameLogicData.TryGetData(Ancients, out var data);
                if (player.color == data.color)
                {
                    TileData tile = gameState.Map.GetTile(player.GetCurrentCapitalCoordinates(gameState));

                    if (tile != null && tile.improvement != null)
                    {
                        tile.improvement.name = "Polytopia";
                    }
                }
            }
        }
    }
}
