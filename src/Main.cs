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
using System.Data;
using Steamworks.Data;
using Il2CppSystem;
using System.Timers;
using Il2CppMono.Security.Interface;


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

    public static void ParseListPerEach<targetType, T>(JObject rootObject, string categoryName, string fieldName, Dictionary<targetType, List<T>> dict)
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
                        List<T> v = ParseToSysList<T>(token[fieldName]);
                        dict[type] = v;
                        token.Remove(fieldName);
                    }
                }
            }
        }
    }

    public static List<T> ParseToSysList<T>(JToken token)
    {
        JArray jArray = token.TryCast<JArray>();
        if (jArray == null)
        {
            modLogger.LogWarning($"couldnt parse {token.GetName()}, not a jArray");
            return new List<T>();
        }
        return ParseJArray<T>(jArray);
    }

    public static List<T> ParseJArray<T>(JArray token)
    {
        List<T> list = new List<T>();

        for (int i = 0; i < token.Count; i++)
        {
            list.Add(token[i].ToObject<T>());
        }

        return list;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.AddGameLogicPlaceholders))]
    public static void GetEnumShit(Newtonsoft.Json.Linq.JObject rootObject)
    {
        if (
            !EnumCache<UnitAbility.Type>.TryGetType("charge_ability", out var chargeType) 
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
            || !EnumCache<ImprovementData.Type>.TryGetType("excavate_improvement", out var excavateType)
            || !EnumCache<ImprovementData.Type>.TryGetType("discharge_improvement", out var dischargeType)
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
        Charged = chargeEffectType;
        Lightning = lightningType;
        Electric = electricType;
        Excavate = excavateType;
        Discharge = dischargeType;

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

        ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "maxCharge", MaxCharge);
        ParsePerEach<UnitData.Type, int>(rootObject, "unitData", "chargeConsumptionAmount", ChargeConsumptionAmount);
        ParsePerEach<ImprovementData.Type, int>(rootObject, "improvementData", "lightningStars", LightningStars);
        ParsePerEach<ImprovementData.Type, bool>(rootObject, "improvementData", "lightningGrow", LightningGrow);
        ParseListPerEach<UnitData.Type, string>(rootObject, "unitData", "chargeConsumptionEvent", ChargeConsumptionEvent);
        ParseListPerEach<UnitData.Type, string>(rootObject, "unitData", "chargeBuff", ChargeBuff);
    }

	public static Dictionary<UnitData.Type, int> MaxCharge = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, int> ChargeConsumptionAmount = new Dictionary<UnitData.Type, int>();
    public static Dictionary<UnitData.Type, List<string>> ChargeConsumptionEvent = new Dictionary<UnitData.Type, List<string>>();
    public static Dictionary<UnitData.Type, List<string>> ChargeBuff = new Dictionary<UnitData.Type, List<string>>();
    public static Dictionary<ImprovementData.Type, int> LightningStars = new();
    public static Dictionary<ImprovementData.Type, bool> LightningGrow = new();
    public static List<CityReward> SecretRewards = new();
    public static TechData.Type TeslaTech;
    public static TechData.Type DroneTech;
    public static TechData.Type AccumulatorTech;
    public static TechData.Type PylonTech;
    public static TechData.Type SentryTech;
    public static TribeType Ancients;
    public static UnitAbility.Type Charge;
    public static UnitAbility.Type Capacitor;
    public static UnitAbility.Type Eightway;
    public static UnitAbility.Type Shock;
    public static UnitEffect Charged;
    public static UnitEffect Conductive;
    public static ImprovementAbility.Type Lightning;
    public static ImprovementAbility.Type Electric;
    public static ImprovementData.Type Excavate;
    public static ImprovementData.Type Discharge;


    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.CanBuild))]
    public static void GLD_CanBuild(ref bool __result, GameState gameState, TileData tile, PlayerState playerState, ImprovementData improvement)
    {
        if (improvement.type == Excavate)
        {
            if (tile.improvement != null) //this is needed cuz discrete is needed so it doesnt look fucking ass
            {
                __result = false;
            }
        }
        if (improvement.type == Discharge)
        {
            if (tile.unit == null) return;

            if (GetChargeCount(tile.unit) == 0)
            __result = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildAction), nameof(BuildAction.ExecuteDefault))]
    public static void BuildAction_Execute(BuildAction __instance, GameState gameState)
    {
        if (!gameState.TryGetPlayer(__instance.PlayerId, out var player))
        {
            modLogger.LogError("YOU WIN!!");
            return;
        }
        if (__instance.Type == Discharge)
        {
            UnitState unit = gameState.Map.GetTile(__instance.Coordinates).unit;
            int radius = (GetChargeCount(unit) == GetMaxCharge(unit.type)) ? 2 : 1;
            Il2Gen.List<TileData> area = gameState.Map.GetArea(__instance.Coordinates, radius, true, false);

            foreach (TileData tile in area)
            {
                if (tile.unit != null && !player.HasPeaceWith(tile.unit.owner) && tile.unit.owner != __instance.PlayerId)
                {
                    BattleResults battleResults2 = BattleHelpers.GetBattleResults(gameState, unit, tile.unit);
                    gameState.ActionStack.Add(new AttackAction(__instance.PlayerId, __instance.Coordinates, tile.coordinates, battleResults2.attackDamage / 2, shouldMoveToTarget: false, AttackAction.AnimationType.Splash, 20));
                }
            }
            ConsumeCharge(unit);
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
            ConsumeCharge(unit);
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
            defender.effects.Add(Charged);
		}

        if (attacker.HasAbility(Capacitor) && DoesConsume(attacker.type, "attack"))
        {
            ConsumeCharge(attacker);
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
    [HarmonyPatch(typeof(AttackAction), nameof(AttackAction.Execute))]
    private static void Kill(GameState state, AttackAction __instance)
	{
        TileData tile = state.Map.GetTile(__instance.Target);
        UnitState unit = tile.unit;
        if (unit == null) return;

        if (unit.HasEffect(Conductive) && unit.health <= __instance.Damage)
        {
            foreach (TileData tile1 in state.Map.GetArea(__instance.Target, 1, true, false))
            {
                if (tile1.unit != null && tile1.coordinates != __instance.Origin)
                {
                    tile1.unit.AddEffect(Conductive);
                    state.ActionStack.Add(new AttackAction(__instance.PlayerId, tile1.coordinates, tile1.coordinates, 50, false, AttackAction.AnimationType.Splash, 20));
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExamineRuinsAction), nameof(ExamineRuinsAction.ExecuteDefault))]
    private static bool ExamineRuins_Execute(GameState gameState, ExamineRuinsAction __instance)
	{
        TileData tile = gameState.Map.GetTile(__instance.Coordinates);
        if (!gameState.TryGetPlayer(__instance.PlayerId, out var player))
        {
            return true;
        }
        tile.improvement = null;
        if (tile.unit != null)
        {
            tile.unit.MakeExhauseted(gameState);
        }

        if (__instance.PlayerId == GameManager.LocalPlayer.Id)
        {
            
            List<TechData.Type> techs = new()
            {
                TeslaTech,
                DroneTech,
                AccumulatorTech,
                PylonTech,
                SentryTech
            };

            List<TechData.Type> eligibleTechs = new();
            
            int num = 0;

            foreach (TechData.Type tech in techs)
            {
                if (!player.HasTech(tech))
                {
                    num++;
                    eligibleTechs.Add(tech);
                }
            }

            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<CityReward> rewards = new CityReward[2];
            
            if (num == 0)
            {
                return true;
            }
            else if (num == 1)
            {
                rewards = new CityReward[1]
                {
                    SecretRewards[techs.LastIndexOf(eligibleTechs[0])]
                };
            }
            else
            {
                Il2CppSystem.Random random = new Il2CppSystem.Random(gameState.Seed);
                for (int i = 0; i < eligibleTechs.Count - 1; i++)
                {
                    int index = random.Range(i, eligibleTechs.Count);
                    TechData.Type value = eligibleTechs[index];
                    eligibleTechs[index] = eligibleTechs[i];
                    eligibleTechs[i] = value;
                }

                rewards[0] = SecretRewards[techs.LastIndexOf(eligibleTechs[0])];
                rewards[1] = SecretRewards[techs.LastIndexOf(eligibleTechs[1])];
            }
            if (num != 0)
            {
                var popup = PopupManager.GetRewardPopup();
                popup.SetData(GameManager.LocalPlayer, gameState.Map.GetTile(__instance.Coordinates), rewards, RewardPopup.PopupType.CityLevelUp, false);
                popup.Header = Localization.Get("world.ancients.popup.header");
                popup.Description = Localization.Get("world.ancients.popup.description");
                popup.Show();
            }
        }

        return false;
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RewardPopup), nameof(RewardPopup.OnRewardButtonClicked))]
    private static void fix(int id, UnityEngine.EventSystems.BaseEventData eventData, RewardPopup __instance)
	{
        GameState gameState = GameManager.GameState;
        CityReward reward = __instance.cityRewards[id];

        if (SecretRewards.Contains(reward))
        {
            List<TechData.Type> techs = new()
            {
                TeslaTech,
                DroneTech,
                AccumulatorTech,
                PylonTech,
                SentryTech
            };

            TechData.Type type = techs[SecretRewards.LastIndexOf(reward)];

            gameState.TryGetPlayer(gameState.CurrentPlayer, out var player);
            player.availableTech.Add(type);
            gameState.ActionStack.Add(new ResearchAction(gameState.CurrentPlayer, type, 0));
        }
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExamineRuinsReaction), nameof(ExamineRuinsReaction.Execute))]
    private static bool ExamineRuins_ReactionFix(Il2CppSystem.Action onComplete, ExamineRuinsReaction __instance)
	{
        if (!GameManager.GameState.TryGetPlayer(__instance.action.PlayerId, out var player)) return true;
        if (player.tribe != Ancients) return true;

        Tile tileInstance = MapRenderer.Current.GetTileInstance(__instance.action.Coordinates);
        if (tileInstance.IsHidden)
        {
            tileInstance.StopRainbowFire(false);
            onComplete.Invoke();
            return false;
        }
        tileInstance.Render();
        tileInstance.SpawnShine();
        tileInstance.SpawnSparkles();
        AudioManager.PlaySFXAtTile(SFXTypes.Examine, tileInstance.Coordinates);
        
        onComplete.Invoke();
        return false;
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RewardPopup), nameof(RewardPopup.SetData))]
    private static bool SetDataFix(RewardPopup __instance, PlayerState playerState, TileData tile, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<CityReward> rewards, RewardPopup.PopupType type, bool isReplay = false)
	{
        if (tile.improvement == null)
        {
            __instance.SetRewards(playerState, rewards, isReplay);
            RewardPopup.OnDataSet?.Invoke(__instance);
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
                    LightningStrike(tile.coordinates, gameState);
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

    public static void LightningStrike(WorldCoordinates position, GameState gameState)
    {
        Il2Gen.List<TileData> tiles = gameState.Map.GetArea(position, 1, true, false);

        int num = 0;
        
        MapRenderer.Current.GetTileInstance(position).SpawnExplosion();


        foreach (TileData tile in tiles)
        {
            if (tile.improvement == null)
            continue;

            ImprovementData data = gameState.GameLogicData.GetImprovementData(tile.improvement.type);
            if (!data.HasAbility(Electric))
            continue;

            MapRenderer.Current.GetTileInstance(tile.coordinates).SpawnPuff();

            if (GetLightningStars(data.type) > 0)
            {
                gameState.ActionStack.Add(new IncreaseCurrencyAction(tile.owner, tile.coordinates, GetLightningStars(data.type), 0));
            }
            if (GetLightningGrow(data.type))
            {
                gameState.ActionStack.Add(new ImprovementLevelUpAction(gameState.CurrentPlayer, tile.coordinates));
            }

            

            num++;
        }
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

    static int GetChargeConsumptionAmount(UnitData.Type unit)
    {
        int i = 3;
        ChargeConsumptionAmount.TryGetValue(unit, out i);
        return i;
    }

    static int GetLightningStars(ImprovementData.Type imp)
    {
        int i = 0;
        LightningStars.TryGetValue(imp, out i);
        return i;
    }

    static bool GetLightningGrow(ImprovementData.Type imp)
    {
        bool b = false;
        LightningGrow.TryGetValue(imp, out b);
        return b;
    }


    static bool DoesConsume(UnitData.Type unit, string e)
    {
        if (!ChargeConsumptionEvent.TryGetValue(unit, out var strings))
        return false;

        return strings.Contains(e);
    }

    static bool GetsChargeBuff(UnitData.Type unit, string e)
    {
        if (ChargeBuff.TryGetValue(unit, out var strings))
        {
            return strings.Contains(e);
        }
        else
        return false;
    }

    static void ConsumeCharge(UnitState unit)
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