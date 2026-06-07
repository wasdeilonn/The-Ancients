using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary;
using Il2Gen = Il2CppSystem.Collections.Generic;
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
        modLogger.LogMessage("Version INDEV2");

        PolyMod.Loader.AddPatchDataType("sfx", typeof(SFXTypes));
        PolyMod.Loader.AddPatchDataType("improvementEffect", typeof(ImprovementEffect));
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
        PolibActionManager.RegisterAction<ApplyConductionAction>("applyconductionaction");

        PolibReactionManager.AssignReaction<DischargeReaction>("dischargeaction");
        PolibReactionManager.AssignReaction<ExcavateReaction>("excavateaction");
        PolibReactionManager.AssignReaction<AncientsExamineReaction>("ancientsexamineaction");
        PolibReactionManager.AssignReaction<ChargeReaction>("chargeaction");
        PolibReactionManager.AssignReaction<LightningStrikeReaction>("lightningstrikeaction");
        PolibReactionManager.AssignReaction<ApplyConductionReaction>("applyconductionaction");

        if (
            !EnumCache<UnitAbility.Type>.TryGetType("charge_ability", out Charge) 
            || !EnumCache<UnitAbility.Type>.TryGetType("excavation_ability", out Excavate)
            || !EnumCache<UnitAbility.Type>.TryGetType("discharge_ability", out Discharge)
            || !EnumCache<UnitAbility.Type>.TryGetType("capacitor_ability", out Capacitor) 
            || !EnumCache<UnitAbility.Type>.TryGetType("eightway_ability", out Eightway) 
            || !EnumCache<UnitAbility.Type>.TryGetType("shock_ability", out Shock) 
            || !EnumCache<UnitEffect>.TryGetType("conductive_effect", out Conductive)
            || !EnumCache<UnitEffect>.TryGetType("charge_effect", out Charged)
            || !EnumCache<CityReward>.TryGetType("highvoltage_secretreward", out var teslaReward) 
            || !EnumCache<CityReward>.TryGetType("aviation_secretreward", out var droneReward) 
            || !EnumCache<CityReward>.TryGetType("chargestorage_secretreward", out var accReward) 
            || !EnumCache<CityReward>.TryGetType("logistics_secretreward", out var sapperReward) 
            || !EnumCache<CityReward>.TryGetType("redirection_secretreward", out var sentryReward) 
            || !EnumCache<TechData.Type>.TryGetType("tesla_secrettech", out var teslaTech) 
            || !EnumCache<TechData.Type>.TryGetType("accumulator_secrettech", out var accTech) 
            || !EnumCache<TechData.Type>.TryGetType("drone_secrettech", out var droneTech)
            || !EnumCache<TechData.Type>.TryGetType("sapper_secrettech", out var sapperTech)
            || !EnumCache<TechData.Type>.TryGetType("sentry_secrettech", out var sentryTech)
            || !EnumCache<ImprovementAbility.Type>.TryGetType("lightning_improvementability", out Lightning)
            || !EnumCache<ImprovementAbility.Type>.TryGetType("electric_improvementability", out Electric)
            || !EnumCache<ImprovementAbility.Type>.TryGetType("critical_improvementability", out CriticalAbility)
            || !EnumCache<TribeType>.TryGetType("ancients", out Ancients)
            || !EnumCache<ImprovementEffect>.TryGetType("critical_improvementeffect", out Critical)
            )
		{
			modLogger.LogInfo("couldnt find some enumcache shit");
			return;
		}

        TeslaTech = teslaTech;
        DroneTech = droneTech;
        AccumulatorTech = accTech;
        SapperTech = sapperTech;
        SentryTech = sentryTech;

        SecretRewards.AddRange(new CityReward[]
        {
            teslaReward,
            droneReward,
            accReward,
            sapperReward,
            sentryReward
        });

        Techs.AddRange(new TechData.Type[]
        {
            teslaTech,
            droneTech,
            accTech,
            sapperTech,
            sentryTech
        });
        
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
    public static List<CityReward> SecretRewards = new();
    public static List<TechData.Type> Techs = new();
    public static TechData.Type TeslaTech;
    public static TechData.Type DroneTech;
    public static TechData.Type AccumulatorTech;
    public static TechData.Type SapperTech;
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
    public static ImprovementAbility.Type CriticalAbility;
    public static UnitAbility.Type Excavate;
    public static ImprovementEffect Critical;


    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommandUtils), nameof(CommandUtils.GetUnitActions))]
    private static void AddCommands(ref Il2Gen.List<CommandBase> __result, GameState gameState, PlayerState player, TileData tile, bool includeUnavailable)
    {
        if (tile.unit == null || tile.unit.owner != player.Id) return;

        if (tile.unit.HasAbility(Discharge) && tile.unit.HasAbility(Capacitor) && !tile.unit.moved && !tile.unit.attacked && ChargeManager.GetChargeCount(tile.unit) > 0)
        {
            DischargeCommand command = PolibCommandManager.MakeIl2CppCommand<DischargeCommand>();
            command.Coordinates = tile.coordinates;
            command.PlayerId = player.Id;
            command.Level = ChargeManager.GetChargeCount(tile.unit) - 1; //subtract 1 cause its 0 based
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

    

	[HarmonyPostfix]
    [HarmonyPatch(typeof(UnitDataExtensions), nameof(UnitDataExtensions.GetAttackOptionsAtPosition))]
    private static void UnitDataExtensions_GetAttackOptionsAtPosition(ref Il2Gen.List<WorldCoordinates> __result, GameState gameState, byte playerId, WorldCoordinates position, int range, bool includeHiddenTiles = false, UnitState customUnitState = null, bool ignoreDiplomacyRelation = false)
	{
        if (!gameState.TryGetPlayer(playerId, out var player)) return;

        UnitState unit = gameState.Map.GetTile(position).unit;
        if (unit == null) return;

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

        if (unit.HasAbility(UnitAbility.Type.Consumed) && unit.UnitData.attack == 0 && !unit.HasAbility(UnitAbility.Type.Convert))
        {
            Il2Gen.List<TileData> area = gameState.Map.GetArea(position, range, allowDiagonal: true, includeCenter: false);
            foreach (TileData tileData in area)
            {
                UnitState unit1 = tileData.GetUnit(gameState, playerId, includeHiddenTiles);
                if (unit1 != null && (tileData.GetExplored(playerId) || includeHiddenTiles) && unit1.owner != playerId && !player.HasPeaceWith(unit1.owner))
                {
                    __result.Add(tileData.coordinates);
                }
            }
        }
	}

	[HarmonyPostfix]
    [HarmonyPatch(typeof(AttackAction), nameof(AttackAction.Execute))]
    private static void AttackAction_Execute(GameState state, AttackAction __instance)
	{
		UnitState attacker = state.Map.GetTile(__instance.Origin).unit;
		UnitState defender = state.Map.GetTile(__instance.Target).unit;

        if (attacker.HasAbility(Shock))
        {
            ApplyConductionAction action = PolibActionManager.MakeIl2CppAction<ApplyConductionAction>();
            action.PlayerId = attacker.owner;
            action.Coordinates = defender.coordinates;
            action.Origin = attacker.coordinates;
            state.ActionStack.Add(action);

            if (attacker.HasAbility(UnitAbility.Type.Splash))
            {
                state.TryGetPlayer(__instance.PlayerId, out var player);
                foreach (TileData tile in state.Map.GetArea(__instance.Target, 1, true, false))
                {
                    if (tile.unit != null && !player.HasPeaceWith(tile.unit.owner) && tile.unit.owner != __instance.PlayerId)
                    {
                        ApplyConductionAction action1 = PolibActionManager.MakeIl2CppAction<ApplyConductionAction>();
                        action1.PlayerId = attacker.owner;
                        action1.Coordinates = tile.coordinates;
                        action1.Origin = attacker.coordinates;
                        state.ActionStack.Add(action1);
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
                    gameState.ActionStack.Add(new AttackAction(tile.unit.owner, tile1.coordinates, tile1.coordinates, 50, false, AttackAction.AnimationType.Splash, 20));
                    ApplyConductionAction action = PolibActionManager.MakeIl2CppAction<ApplyConductionAction>();
                    action.PlayerId = tile.unit.owner;
                    action.Coordinates = tile1.coordinates;
                    action.Origin = tile.coordinates;
                    gameState.ActionStack.Add(action);
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