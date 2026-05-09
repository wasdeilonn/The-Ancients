using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary;
using PolibMain = Polibrary.Main;
using AMain = Ancients.Main;
using Il2Gen = Il2CppSystem.Collections.Generic;
using UnityEngine.UIElements.UIR;

public static class RuinPatcher
{
    public static void Load(ManualLogSource logger)
    {
        Harmony.CreateAndPatchAll(typeof(RuinPatcher));
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ExamineRuinsCommand), nameof(ExamineRuinsCommand.Execute))]
    private static bool ExamineRuins_Execute(GameState state, ExamineRuinsCommand __instance)
	{
        if (!state.TryGetPlayer(__instance.PlayerId, out var player))
        {
            return true;
        }
        if (player.tribe != AMain.Ancients) return true;


        AncientsExamineAction action = PolibActionManager.MakeIl2CppAction<AncientsExamineAction>();
        action.Coordinates = __instance.Coordinates;
        action.PlayerId = __instance.PlayerId;
        state.ActionStack.Add(action);
        return false;
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CommandTriggerUIUtils), nameof(CommandTriggerUIUtils.ShowCommandTrigger))]
    public static bool CommandTriggerPrefix(CommandTrigger commandTrigger)
    {
        GameState state = GameManager.GameState;
        if (!GameManager.GameState.TryGetPlayer(commandTrigger.playerId, out var player)) return true;
        TileData tile = state.Map.GetTile(commandTrigger.coordinates);

        if (commandTrigger.type != CommandTriggerType.CityLevelUp) return true;

        if (tile.improvement == null || tile.improvement.type == ImprovementData.Type.City)
        {
            if (!PopupManager.IsPopupShowing<RewardPopup>())
            {
                List<TechData.Type> techs = new()
                {
                    AMain.TeslaTech,
                    AMain.DroneTech,
                    AMain.AccumulatorTech,
                    AMain.PylonTech,
                    AMain.SentryTech
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
                    return false;
                }
                else if (num == 1)
                {
                    rewards = new CityReward[1]
                    {
                        AMain.SecretRewards[techs.LastIndexOf(eligibleTechs[0])]
                    };
                }
                else
                {
                    Il2CppSystem.Random random = new Il2CppSystem.Random(state.Seed);
                    for (int i = 0; i < eligibleTechs.Count - 1; i++)
                    {
                        int index = random.Range(i, eligibleTechs.Count);
                        TechData.Type value = eligibleTechs[index];
                        eligibleTechs[index] = eligibleTechs[i];
                        eligibleTechs[i] = value;
                    }

                    rewards[0] = AMain.SecretRewards[techs.LastIndexOf(eligibleTechs[0])];
                    rewards[1] = AMain.SecretRewards[techs.LastIndexOf(eligibleTechs[1])];
                }
                if (num != 0)
                {
                    var popup = PopupManager.GetRewardPopup();
                    popup.RewardChoosenCallback = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Il2CppSystem.Action<TileData, CityReward>>(
                        (System.Action<TileData, CityReward>)((tileData, reward) => PerformCityRewardActionTwo(tileData, reward, commandTrigger.coordinates))
                    );
                    popup.SetData(player, tile, rewards, RewardPopup.PopupType.CityLevelUp, false);
                    popup.Header = Localization.Get("world.ancients.popup.header");
                    popup.Description = Localization.Get("world.ancients.popup.description");
                    popup.Show();
                    AudioManager.PlaySFX(SFXTypes.Popup);
                    return false;
                }
            }
        }
        return true;
    }

    private static void PerformCityRewardActionTwo(TileData tile, CityReward cityReward, WorldCoordinates coordinates) //tile is NULL!! DONT USE IT!!
    {
        GameManager.GameState.TryGetPlayer(GameManager.GameState.CurrentPlayer, out var player);
        CityRewardCommand cityRewardCommand = new CityRewardCommand(player.Id, cityReward, coordinates);
        if (cityRewardCommand.IsValid(GameManager.GameState))
        {
            GameManager.Client.SendCommand(cityRewardCommand);
            AudioManager.PlaySFX(SFXTypes.RewardEnd);
        }
        else
        {
            CommandTriggerUIUtils.TryShowNextCommandTrigger();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CityRewardCommand), nameof(CityRewardCommand.IsValid))]
    private static bool CityRewardCommand_IsValid(GameState gameState, out string validationError, CityRewardCommand __instance, ref bool __result)
    {
        if (AMain.SecretRewards.Contains(__instance.Reward))
        {
            __result = true;
            validationError = "";
            return false;
        }
        validationError = "";
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CityRewardAction), nameof(CityRewardAction.Execute))]
    private static bool CityRewardActionPatch(GameState state, CityRewardAction __instance)
    {
        if (AMain.SecretRewards.Contains(__instance.Reward))
        {
            List<TechData.Type> techs = new()
            {
                AMain.TeslaTech,
                AMain.DroneTech,
                AMain.AccumulatorTech,
                AMain.PylonTech,
                AMain.SentryTech
            };

            TechData.Type type = techs[AMain.SecretRewards.LastIndexOf(__instance.Reward)];
            state.ActionStack.Add(new ResearchAction(__instance.PlayerId, type, 0));
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CityRewardReaction), nameof(CityRewardReaction.Execute))]
    private static bool CityRewardReactionPatch(Il2CppSystem.Action onComplete, CityRewardReaction __instance)
    {
        if (AMain.SecretRewards.Contains(__instance.action.Reward))
        {
            onComplete.Invoke();
            return false;
        }

        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.GetUnlockableTech))]
    private static void SecretTechUnlockabilityFix(Il2Gen.List<TechData> __result, PlayerState player, GameState gameState)
    {
        if(player.tribe == AMain.Ancients)
        {
            List<TechData.Type> techs = new()
            {
                AMain.TeslaTech,
                AMain.DroneTech,
                AMain.AccumulatorTech,
                AMain.PylonTech,
                AMain.SentryTech
            };

            foreach (TechData.Type type in techs)
            {
                gameState.GameLogicData.TryGetData(type, out var data);
                __result.Add(data);
            }
        }
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
}

public class AncientsExamineAction : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public AncientsExamineAction(IntPtr ptr) : base(ptr) {}
    public AncientsExamineAction() {}
    public AncientsExamineAction(byte playerId, WorldCoordinates coordinates) 
    : base(playerId)
    {
        base.PlayerId = playerId;
        Coordinates = coordinates;
    }
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("ancientsexamineaction");
    }
    
    public override void Execute(GameState state)
    {
        if (!GameManager.GameState.TryGetPlayer(PlayerId, out var player)) return;
        TileData tile = state.Map.GetTile(Coordinates);

        if (tile == null) return;

        tile.improvement = null;
        if (tile.unit != null)
        {
            tile.unit.MakeExhauseted(state);
        }

        CommandTrigger commandTrigger = new CommandTrigger
        {
            playerId = PlayerId,
            type = CommandTriggerType.CityLevelUp,
            coordinates = Coordinates
        };
        state.pendingCommandTriggers.Add(commandTrigger);
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        base.Serialize(writer, version); //this line is important btw
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        base.Deserialize(reader, version); //leave this line in
        Coordinates.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Coordinates: {2})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Coordinates
        });
    }
}

public class AncientsExamineReaction : PolibReactionBase
{
    protected AncientsExamineAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            AncientsExamineAction action = value.TryCast<AncientsExamineAction>();
            if (action != null)
            this.action = action;
            else
            AMain.modLogger.LogInfo("shits fucked");
        } 
    }
    public AncientsExamineReaction(IntPtr ptr) : base(ptr) {}
    public AncientsExamineReaction(AncientsExamineAction action)
    {
        this.action = action;
    }

    public override bool ShouldFocusCamera()
    {
        return IsRecapOrOpponentAction(action);
    }

    public override WorldCoordinates GetCameraFocusCoordinates()
    {
        return action.Coordinates;
    }

    public override void Execute(Il2CppSystem.Action onComplete)
    {
        Tile instance = MapRenderer.Current.GetTileInstance(action.Coordinates);
        if (instance == null) return;

        if (instance.IsHidden)
        {
            instance.StopRainbowFire(false);
            onComplete.Invoke();
            return;
        }
        instance.Render();
        instance.SpawnShine();
        instance.SpawnSparkles();
        AudioManager.PlaySFXAtTile(SFXTypes.Examine, instance.Coordinates);
        GameManager.DelayCall(200, onComplete);
    }
}
