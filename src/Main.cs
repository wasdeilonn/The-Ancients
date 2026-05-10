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


namespace Ancients;

public static class Main
{
    public static ManualLogSource modLogger;
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(Main));
        modLogger = logger;
        logger.LogMessage("Ancients.dll loaded.");
        modLogger.LogMessage("Version INDEV2");

        PolyMod.Loader.AddPatchDataType("sfx", typeof(SFXTypes));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
    public static void GetEnumShit(Newtonsoft.Json.Linq.JObject rootObject)
    {
        PolibCommandManager.RegisterCommand<DischargeCommand>("dischargecommand");
        PolibCommandManager.RegisterCommand<ExcavateCommand>("excavatecommand");

        PolibActionManager.RegisterAction<DischargeAction>("dischargeaction");
        PolibActionManager.RegisterAction<ExcavateAction>("excavateaction");
        PolibActionManager.RegisterAction<AncientsExamineAction>("ancientsexamineaction");
        PolibActionManager.RegisterAction<ChargeAction>("chargeaction");
        PolibActionManager.RegisterAction<LightningStrikeAction>("lightningstrikeaction");

        PolibReactionManager.AssignReaction<DischargeReaction>("dischargeaction");
        PolibReactionManager.AssignReaction<ExcavateReaction>("excavateaction");
        PolibReactionManager.AssignReaction<AncientsExamineReaction>("ancientsexamineaction");
        PolibReactionManager.AssignReaction<ChargeReaction>("chargeaction");
        PolibReactionManager.AssignReaction<LightningStrikeReaction>("lightningstrikeaction");

        if (
            !EnumCache<UnitAbility.Type>.TryGetType("charge_ability", out var chargeType) 
            || !EnumCache<UnitAbility.Type>.TryGetType("excavation_ability", out var excavateType)
            || !EnumCache<UnitAbility.Type>.TryGetType("discharge_ability", out var dischargeAbilityType)
            || !EnumCache<UnitAbility.Type>.TryGetType("capacitor_ability", out var capacitorType) 
            || !EnumCache<UnitAbility.Type>.TryGetType("eightway_ability", out var eightwayType) 
            || !EnumCache<UnitAbility.Type>.TryGetType("shock_ability", out var shockType) 
            || !EnumCache<UnitEffect>.TryGetType("conductive_effect", out var shockedEffectType)
            || !EnumCache<UnitEffect>.TryGetType("charge_effect", out var chargeEffectType)
            || !EnumCache<CityReward>.TryGetType("highvoltage_secretreward", out var teslaReward) 
            || !EnumCache<CityReward>.TryGetType("aviation_secretreward", out var droneReward) 
            || !EnumCache<CityReward>.TryGetType("chargestorage_secretreward", out var accReward) 
            || !EnumCache<CityReward>.TryGetType("pylons_secretreward", out var pylonReward) 
            || !EnumCache<CityReward>.TryGetType("redirection_secretreward", out var sentryReward) 
            || !EnumCache<TechData.Type>.TryGetType("tesla_secrettech", out var teslaTech) 
            || !EnumCache<TechData.Type>.TryGetType("accumulator_secrettech", out var accTech) 
            || !EnumCache<TechData.Type>.TryGetType("drone_secrettech", out var droneTech)
            || !EnumCache<TechData.Type>.TryGetType("pylon_secrettech", out var pylonTech)
            || !EnumCache<TechData.Type>.TryGetType("sentry_secrettech", out var sentryTech)
            || !EnumCache<ImprovementAbility.Type>.TryGetType("lightning_improvementability", out var lightningType)
            || !EnumCache<ImprovementAbility.Type>.TryGetType("electric_improvementability", out var electricType)
            || !EnumCache<TribeType>.TryGetType("ancients", out var ancientsType)
            )
		{
			modLogger.LogInfo("couldnt find some enumcache shit");
			return;
		}

        Ancients = ancientsType;
        Charge = chargeType;
        Capacitor = capacitorType;
        Eightway = eightwayType;
        Shock = shockType;
        Conductive = shockedEffectType;
        Discharge = dischargeAbilityType;
        Charged = chargeEffectType;
        Lightning = lightningType;
        Electric = electricType;
        Excavate = excavateType;

        TeslaTech = teslaTech;
        DroneTech = droneTech;
        AccumulatorTech = accTech;
        PylonTech = pylonTech;
        SentryTech = sentryTech;

        SecretRewards.AddRange(new CityReward[]
        {
            teslaReward,
            droneReward,
            accReward,
            pylonReward,
            sentryReward
        });

        Techs.AddRange(new TechData.Type[]
        {
            teslaTech,
            droneTech,
            accTech,
            pylonTech,
            sentryTech
        });
        
        PolibUtils.ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "maxCharge", MaxCharge);
        PolibUtils.ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "chargeConsumptionAmount", ChargeConsumptionAmount);
        PolibUtils.ParsePerEach<ImprovementData.Type, int>(rootObject, "improvementData", "lightningStars", LightningStars);
        PolibUtils.ParsePerEach<ImprovementData.Type, bool>(rootObject, "improvementData", "lightningGrow", LightningGrow);
        PolibUtils.ParseListPerEach<UnitData.Type, string>(rootObject, "unitData", "chargeConsumptionEvent", ChargeConsumptionEvent);
        PolibUtils.ParseListPerEach<UnitData.Type, string>(rootObject, "unitData", "chargeBuff", ChargeBuff);
    }

	public static Dictionary<UnitData.Type, int> MaxCharge = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, int> ChargeConsumptionAmount = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, List<string>> ChargeConsumptionEvent = new Dictionary<UnitData.Type, List<string>>();
    public static Dictionary<UnitData.Type, List<string>> ChargeBuff = new Dictionary<UnitData.Type, List<string>>();
    public static Dictionary<ImprovementData.Type, int> LightningStars = new();
    public static Dictionary<ImprovementData.Type, bool> LightningGrow = new();
    public static List<CityReward> SecretRewards = new();
    public static List<TechData.Type> Techs = new();
    public static TechData.Type TeslaTech;
    public static TechData.Type DroneTech;
    public static TechData.Type AccumulatorTech;
    public static TechData.Type PylonTech;
    public static TechData.Type SentryTech;
    public static TribeType Ancients;
    public static UnitAbility.Type Discharge;
    public static UnitAbility.Type Charge;
    public static UnitAbility.Type Capacitor;
    public static UnitAbility.Type Eightway;
    public static UnitAbility.Type Shock;
    public static UnitEffect Charged;
    public static UnitEffect Conductive;
    public static ImprovementAbility.Type Lightning;
    public static ImprovementAbility.Type Electric;
    public static UnitAbility.Type Excavate;


    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommandUtils), nameof(CommandUtils.GetUnitActions))]
    private static void AddCommands(ref Il2Gen.List<CommandBase> __result, GameState gameState, PlayerState player, TileData tile, bool includeUnavailable)
    {
        if (tile.unit == null || tile.unit.owner != player.Id) return;

        if (tile.unit.HasAbility(Discharge) && tile.unit.HasAbility(Capacitor) && !tile.unit.moved && !tile.unit.attacked && GetChargeCount(tile.unit) > 0)
        {
            DischargeCommand command = PolibCommandManager.MakeIl2CppCommand<DischargeCommand>();
            command.Coordinates = tile.coordinates;
            command.PlayerId = player.Id;
            command.Level = GetChargeCount(tile.unit) - 1; //subtract 1 cause its 0 based
            CommandUtils.AddCommand(gameState, __result, command, includeUnavailable);
        }

        if (tile.unit.HasAbility(Excavate) && !tile.unit.moved && !tile.unit.attacked)
        {
            ExcavateCommand command = PolibCommandManager.MakeIl2CppCommand<ExcavateCommand>();
            command.Coordinates = tile.coordinates;
            command.PlayerId = player.Id;
            CommandUtils.AddCommand(gameState, __result, command, includeUnavailable);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void RangeFix(GameState gameState, byte playerId, WorldCoordinates position, ref int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
	{
        UnitState unit = gameState.Map.GetTile(position).unit;
        if (unit == null ) return;

        if (!GetsChargeBuff(unit.type, "range")) return;

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
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttack), typeof(UnitState), typeof(GameState))]
    private static void AddAttack(ref int __result, GameState gameState, UnitState unitState)
	{
        if (!GetsChargeBuff(unitState.type, "attack")) return;

        foreach (UnitEffect effect in unitState.effects)
        {
            if (effect == Charged)
            {
                
                __result += 100;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetMovement))]
    private static void AddMovement(ref int __result, GameState gameState, UnitState unitState)
	{
        if (!GetsChargeBuff(unitState.type, "movement")) return;

        foreach (UnitEffect effect in unitState.effects)
        {
            if (effect == Charged)
            {
                __result++;
            }
        }
    }

	[HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void UnitDataExtensions_GetAttackOptionsAtPosition(ref Il2Gen.List<WorldCoordinates> __result, GameState gameState, byte playerId, WorldCoordinates position, int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
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

        if (unit.HasAbility(Eightway))
        {
            if (!unit.moved)
            {
                __result = new Il2Gen.List<WorldCoordinates>();
                return;
            }

            Il2Gen.List<WorldCoordinates> ewaylist = new();
            foreach (TileData tile in gameState.Map.GetArea(position, 1, true, false))
            {
                ewaylist.Add(tile.coordinates);
            }
            __result = ewaylist;
        }
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MoveAction), nameof(MoveAction.ExecuteDefault))]
    private static void MoveAction_Execute(GameState gameState, MoveAction __instance)
	{
		if (!gameState.TryGetUnit(__instance.UnitId, out var unit)) return;
        if (unit.HasAbility(Capacitor) && DoesConsume(unit.type, "move") && __instance.Reason == MoveAction.MoveReason.Command)
        {
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = unit.owner;
            action.Coordinates = unit.coordinates;
            action.Positive = false;
            gameState.ActionStack.Add(action);
        }
	}

	[HarmonyPostfix]
    [HarmonyPatch(typeof(AttackAction), nameof(AttackAction.Execute))]
    private static void AttackAction_Execute(GameState state, AttackAction __instance)
	{
		UnitState attacker = state.Map.GetTile(__instance.Origin).unit;
		UnitState defender = state.Map.GetTile(__instance.Target).unit;

        if (attacker == null || defender == null) return;

		if (attacker.HasAbility(Charge) && defender.HasAbility(Capacitor))
		{
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = defender.owner;
            action.Coordinates = defender.coordinates;
            action.Positive = true;
            state.ActionStack.Add(action);
		}

        if (attacker.HasAbility(Capacitor) && DoesConsume(attacker.type, "attack"))
        {
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = attacker.owner;
            action.Coordinates = attacker.coordinates;
            action.Positive = false;
            state.ActionStack.Add(action);
        }

        if (attacker.HasAbility(Shock))
        {
            defender.AddEffect(Conductive);

            if (attacker.HasAbility(UnitAbility.Type.Splash))
            {
                state.TryGetPlayer(__instance.PlayerId, out var player);
                foreach (TileData tile in state.Map.GetArea(__instance.Target, 1, true, false))
                {
                    if (tile.unit != null && !player.HasPeaceWith(tile.unit.owner) && tile.unit.owner != __instance.PlayerId)
                    {
                        tile.unit.AddEffect(Conductive);
                    }
                }
            }
        }
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ActionUtils), nameof(ActionUtils.KillUnit))]
    private static void ActionUtils_KillUnit(GameState gameState, TileData tile)
	{
        if (tile.unit == null || !gameState.TryGetPlayer(tile.unit.owner, out var player))
        {
            return;
        }
        if (tile.unit.HasEffect(Conductive))
        {
            foreach (TileData tile1 in gameState.Map.GetArea(tile.coordinates, 1, true, false))
            {
                if (tile1.unit != null && tile1.unit.owner == tile.unit.owner)
                {
                    tile1.unit.AddEffect(Conductive);
                    gameState.ActionStack.Add(new AttackAction(tile.unit.owner, tile1.coordinates, tile1.coordinates, 50, false, AttackAction.AnimationType.Splash, 20));
                }
            }
        }
	}
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
    private static bool AttackCommand_ExecuteDefault(GameState gameState, AttackCommand __instance)
	{
		if (!gameState.TryGetUnit(__instance.UnitId, out var unit)) return true;

        if (unit.HasAbility(Eightway))
        {
            GridDirection direction = WorldCoordinates.GetDirection(__instance.Origin, __instance.Target);

            WorldCoordinates coordinates = __instance.Origin;
            Il2Gen.List<ActionBase> stack = new();
            for (int i = 0; i < unit.GetRange(gameState); i ++)
            {
                coordinates += direction.ToCoordinates();
                TileData tile = gameState.Map.GetTile(coordinates);

                if (tile == null) continue;

                if (tile.unit == null)
                {
                    stack.Add(new AttackAction(__instance.PlayerId, __instance.Origin, coordinates, 0, false, AttackAction.AnimationType.Splash, 20));
                    continue;
                }
                BattleResults battleResults = BattleHelpers.GetBattleResults(gameState, unit, tile.unit);
                stack.Add(new AttackAction(__instance.PlayerId, __instance.Origin, coordinates, battleResults.attackDamage, false, AttackAction.AnimationType.Splash, 20));
            }

            stack.Reverse();

            foreach(ActionBase action in stack)
            {
                gameState.ActionStack.Add(action);
            }

            unit.attacked = true;
            return false;
        }
        return true;
	}

    

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartTurnAction), nameof(StartTurnAction.ExecuteDefault))]
    private static void StartTurn(GameState gameState, StartTurnAction __instance)
	{
		foreach (TileData tile in gameState.Map.tiles)
        {
            if (tile.improvement != null && tile.owner == __instance.PlayerId)
            {
                if (gameState.GameLogicData.GetImprovementData(tile.improvement.type).HasAbility(Lightning))
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
            modLogger.LogError("Nice one dumbfuck");
            return;
        }

        if (data.HasAbility(Electric) && tile.improvement.level <= data.maxLevel)
        {
            __result = true;
        }
	}

    public static int GetChargeCount(UnitState unit)
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

    public static int GetMaxCharge(UnitData.Type unit)
    {
        int i = 3;
        MaxCharge.TryGetValue(unit, out i);
        return i;
    }

    public static int GetChargeConsumptionAmount(UnitData.Type unit)
    {
        int i = 3;
        ChargeConsumptionAmount.TryGetValue(unit, out i);
        return i;
    }

    public static int GetLightningStars(ImprovementData.Type imp)
    {
        int i = 0;
        LightningStars.TryGetValue(imp, out i);
        return i;
    }

    public static bool GetLightningGrow(ImprovementData.Type imp)
    {
        bool b = false;
        LightningGrow.TryGetValue(imp, out b);
        return b;
    }


    public static bool DoesConsume(UnitData.Type unit, string e)
    {
        if (!ChargeConsumptionEvent.TryGetValue(unit, out var strings))
        return false;

        return strings.Contains(e);
    }

    public static bool GetsChargeBuff(UnitData.Type unit, string e)
    {
        if (ChargeBuff.TryGetValue(unit, out var strings))
        {
            return strings.Contains(e);
        }
        else
        return false;
    }

    public static void ConsumeCharge(UnitState unit)
    {
        int consumption = GetChargeConsumptionAmount(unit.type);

        for (int i = 0; i < consumption; i++)
        {
            unit.effects.Remove(Charged);
            modLogger.LogInfo($"removed charge, consumption: {consumption}");
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