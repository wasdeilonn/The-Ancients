using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule.NativeRenderPassCompiler;

using Il2Gen = Il2CppSystem.Collections.Generic;


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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.CanBuild))]
    public static void GLD_CanBuild(ref bool __result, GameState gameState, TileData tile, PlayerState playerState, ImprovementData improvement)
    {
        if (!EnumCache<ImprovementData.Type>.TryGetType("excavate_improvement_ancients", out var type))
        {
            modLogger.LogInfo("excavate renamed but not updated in polyscript?");
        }
        if (improvement.type == type)
        {
            if (tile.improvement != null)
            {
                __result = false;
            }
        }
    }

	[HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void GetAttackOptionsAtPosition(Il2Gen.List<WorldCoordinates> __result, GameState gameState, byte playerId, WorldCoordinates position, int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
	{
		Il2Gen.List<TileData> list = gameState.Map.GetArea(position, range, true, false);

		if (gameState.Map.GetTile(position).unit == null) return;
		if (!EnumCache<UnitAbility.Type>.TryGetType("charge_ability_ancients", out var chargeType) || !EnumCache<UnitAbility.Type>.TryGetType("capacitor_ability_ancients", out var capacitorType)) return;

		if (gameState.Map.GetTile(position).unit.HasAbility(chargeType))
		{
			foreach (TileData tile in list)
			{
				if (tile.unit == null) continue;

				if (tile.unit.HasAbility(capacitorType))
				{
					__result.Add(tile.coordinates);
				}
			}
		}
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