using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;
using Newtonsoft.Json.Linq;
//using Polibrary; <-- it would be so awesome, it would be so cool

using Il2Gen = Il2CppSystem.Collections.Generic;
using Il2CppSystem.Linq;
using MS.Internal.Xml.XPath;
using PolytopiaBackendBase.Common;


namespace Ancients;

public static class Main
{
    public static ManualLogSource modLogger;
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;
        logger.LogMessage("Ancients.dll loaded.");
        modLogger.LogMessage("Version INDEV1");
    }

    public static void ParsePerEach<targetType, T>(JObject rootObject, string categoryName, string fieldName, Dictionary<targetType, T> dict)
        where targetType : struct, System.IConvertible
    {
        foreach (JToken jtoken in rootObject.SelectTokens($"$.{categoryName}.*").ToList())
        {
            JObject token = jtoken.TryCast<JObject>();
            if (token != null)
            {
                if (EnumCache<targetType>.TryGetType(token.Path.Split('.').Last(), out var type))
                {
                    if (token[fieldName] != null)
                    {
                        T v = token[fieldName]!.ToObject<T>();
                        dict[type] = v;
                        token.Remove(fieldName);
                    }
                }
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
    public static void GetEnumShit(Newtonsoft.Json.Linq.JObject rootObject)
    {
        if (
            !EnumCache<UnitAbility.Type>.TryGetType("charge_ability_ancients", out var chargeType) 
            || !EnumCache<UnitAbility.Type>.TryGetType("capacitor_ability_ancients", out var capacitorType) 
            || !EnumCache<UnitEffect>.TryGetType("charge_effect_ancients", out var chargeEffectType)
            || !EnumCache<ImprovementData.Type>.TryGetType("excavate_improvement_ancients", out var excavateType)
            || !EnumCache<TribeType>.TryGetType("ancients", out var ancientsType)
            )
		{
			modLogger.LogInfo("couldnt find some enumcache shit");
			return;
		}

        Ancients = ancientsType;
        Charge = chargeType;
        Capacitor = capacitorType;
        Charged = chargeEffectType;
        Excavate = excavateType;

        ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "maxCharge", MaxCharge);
        ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "chargeConsumption", ChargeConsumption);
    }

	public static Dictionary<UnitData.Type, int> MaxCharge = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, int> ChargeConsumption = new Dictionary<UnitData.Type, int>();
    public static TribeType Ancients;
    public static UnitAbility.Type Charge;
    public static UnitAbility.Type Capacitor;
    public static UnitEffect Charged;
    public static ImprovementData.Type Excavate;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.CanBuild))]
    public static void GLD_CanBuild(ref bool __result, GameState gameState, TileData tile, PlayerState playerState, ImprovementData improvement)
    {
        if (improvement.type == Excavate)
        {
            if (tile.improvement != null)
            {
                __result = false;
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void RangeFix(Il2Gen.List<WorldCoordinates> __result, GameState gameState, byte playerId, WorldCoordinates position, ref int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
	{
        UnitState unit = gameState.Map.GetTile(position).unit;
        if (unit == null) return;

        int newrange = range;
        foreach (UnitEffect effect in unit.effects)
        {
            if (effect == Charged)
            {
                newrange++;
            }
        }

        range = newrange;
    }

	[HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void UnitDataExtensions_GetAttackOptionsAtPosition(Il2Gen.List<WorldCoordinates> __result, GameState gameState, byte playerId, WorldCoordinates position, int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
	{
        UnitState unit = gameState.Map.GetTile(position).unit;
        if (unit == null) return;

		Il2Gen.List<TileData> list = gameState.Map.GetArea(position, range, true, false);
		if (unit.HasAbility(Charge))
		{
			foreach (TileData tile in list)
			{
				if (tile.unit == null) continue;

				if (tile.unit.HasAbility(Capacitor))
				{
                    if (GetChargeCount(tile.unit) < GetMaxCharge(tile.unit.type))
                    {
                        __result.Add(tile.coordinates);
                    }
				}
			}
		}
	}

	[HarmonyPostfix]
    [HarmonyPatch(typeof(AttackAction), nameof(AttackAction.Execute))]
    private static void AttackAction_Execute(GameState state, AttackAction __instance)
	{
		if (state.Map.GetTile(__instance.Origin).unit == null) return;
		if (state.Map.GetTile(__instance.Target).unit == null) return;

		UnitState attacker = state.Map.GetTile(__instance.Origin).unit;
		UnitState defender = state.Map.GetTile(__instance.Target).unit;

		if (attacker.HasAbility(Charge) && defender.HasAbility(Capacitor))
		{
            defender.effects.Add(Charged);
		}

        if (attacker.HasAbility(Capacitor))
        {
            int consumption = GetChargeConsumption(attacker.type);

            for (int i = 0; i <= consumption; i++)
            {
                attacker.effects.Remove(Charged);
            }
        }
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExamineRuinsAction), nameof(ExamineRuinsAction.ExecuteDefault))]
    private static bool ExamineRuins_Execute(GameState gameState, ExamineRuinsAction __instance)
	{
        TileData tile = gameState.Map.GetTile(__instance.Coordinates);
        gameState.TryGetPlayer(__instance.PlayerId, out var player);
        if (player.tribe != Ancients)
        {
            return true;
        }
        tile.improvement = null;
        if (tile.unit != null)
        {
            tile.unit.MakeExhauseted(gameState);
        }

        

		return false;
	}

    static int GetChargeCount(UnitState unit)
    {
        int charges = 0;

        foreach (UnitEffect effect in unit.effects)
        {
            if (effect == Charged)
            {
                charges++;
            }
        }

        return charges;
    }

    static int GetMaxCharge(UnitData.Type unit)
    {
        int i = 3;
        MaxCharge.TryGetValue(unit, out i);
        return i;
    }

    static int GetChargeConsumption(UnitData.Type unit)
    {
        int i = 3;
        ChargeConsumption.TryGetValue(unit, out i);
        return i;
    }
}
































    //I copied from polibrary so this is staying 
    /*



                                                                                ███████        ████████                                                                                       
                                                                           ████                   ██████                                                                                      
                                                                       ████                   █████                                                                                           
                                                                     ███                   ███                                                                                                
                                                                   ███                  ████                                                                                                  
                                                                 ███                  ███                                                                                                     
                                                               ███                 ████                                                                                                       
                                                              ███                 ██                                                                                                          
                                                             ███                 ██                                                                                                           
                                                            ██                  ██                                                                                                            
                                                           ██                   ██               █████                                                                                        
                                                          ██                      ███   ████████████████████                                                                                  
                                                         ███                        █████████████████████████████                                                                             
                                                         ██                     ███████████      █████████████████████                              ██                                        
                                                         ██                █████████████                ████████████████████                       ███                                        
                                                         ██            █████████████                        ████████████████████████  ██████     ███ ██                                       
                                                          ██        ██████████████                              ████████████████████████████   ███   ██                                       
                                                        ████    █████████████████                    ██            ██████████████████████   ████     ██                                       
                                                       ███████████████████████                         ██            ███████████████████████         ██                                       
                                                       ██████████████████████        ███████             ██           █████████████████              ██                                       
                                                       █████████████████████            ██████            ██            ████████████████             ██                                       
                                                       ████████████████████                ████            ██            ███████████████            ██                                        
                                                       ██████████████████                    ███            ████          ██████████████           ██                                         
                                                     ███████████████████              ███████████                          █████████████          ██                                          
                                                  █████████████████████            ██ █  ██████████                        ██████████████       ██                                            
                                                 █████████████████████                  █████    ███                        █████████████      ██                                             
                                               ██████████████████████                          █   ██       █                ███████████     ███                                              
                                               █████████████████████                                     ███        ███████  ███████████   ███                                                
                                              █████████████████████         ██                              ███████████      ██████████████                                                   
                                             █████████████████████          ███                                  ████ ██     ████████████                                                     
                                             ████████████████████          █████                                         █  ████████████                                                      
                                            █████████████████████          ███████                                          █████████                                                         
                                            ████████████████████          ███████████          ███    ██                   █████████                                                          
                                            ███████████████████        ███████████████████               ██                ████████                                                           
                                    ███████████████████████████       █████████████████████                  ██           ████████                                                            
                      █████████████████     ██████████████████        ████████████████████████                            ████████                                                            
              ████████████                 ██████████████████        █████████    ████████████                            ████████                                                            
        █████████                         ███████████████████       █████████    ██       ████   ██                       ████████                                                            
                                        ██  ████████████████        █████████     ██         ███  ██              ███     █████████                                                           
                                      ███   ███   ██████████       ██████████      █           ███                 ████   █████████                                                           
                                     ██             ████████      █████████         █           █████      █        ████  █████████                                                           
                                    ██               ███████      ████████           ███          ██████          █████████████████   ██                                                      
                                  ███                ████████    ███████                ███           ███████████       ████████████████                                                      
                                 ██                  █████████   ██████     █              ██                              ███████████                                                        
                                ██                  ███████████████████ █████          █      ████                          ██████████                                                        
                               ██                ██████████████████████████             ███       █████                   █  ███████████                                                      
                             ███             █████████████████████████████                 ███           █████        █████   ███████████                                                     
                             ██               ███████████████████████████                      █                               ██████████                                                     
                           ███                 ██████████████████████████                                                       █████████                                                     
                          ██                     ████████████████████████                                                       ███   ██                                                      
                          ██                       ████████████████████████                                         ██         ███     ██                                                     
                         ██                          ██████████████████████                                      ███           ██      ██                                                     
                        ██                             █████████████████████                                  ███              ██       ██                                                    
                        ██                               ███████████████████                             █████                ██        ██                                                    
                       ██                                  ██████████████████                                                ██          █                                                    
                       ██                                   █████████████████                                               ██           ██                                                   
                      ██                                  ██ ██████████████████                                            ██             ██                                                  
                     ██                                  ███  ████████████████████            ██                          █               ███                                                 
                     ██                                 ██     █████████████████████         ██                          ██               █████                                               
                     ██                                ██       ███████████████████████    █████     ██                 ██                ██  ███                                             
                     ██                               ██         ████████████████████████████████   ████     ███       ██                 █     ███                                           
                     █                              ██           ████████████████████████████████████████████████    ███                 ██       ██                                          
                    ██                            ███             █████████████████████████████████████████████████████                  █         ███                                        
                    ██                           ███               ███████████████████████████████████████████████████                  ██          ███                                       
                     █                          ██                  ████████████████████████████████████████████████                   ██             ██                                      
                     ██                        ██                   ███████████████████████████████████████████████                  ██                ███                                    
                     ██                      ██                      ████████████████████████████████████████████████             ███                   ███                                   
                      ██                   ███                        █████████████████████████████████████████      ███████  █████                      ███                                  
                       ██                 ██                           ███████████████████████████████████████                                            ███                                 
        */