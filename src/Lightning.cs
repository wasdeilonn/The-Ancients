using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary;
using PolibMain = Polibrary.Main;
using AMain = Ancients.Main;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Ancients;

public class LightningStrikeAction : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public LightningStrikeAction(IntPtr ptr) : base(ptr) {}
    public LightningStrikeAction() {}

    public LightningStrikeAction(byte playerId, WorldCoordinates coordinates) 
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
        return EnumCache<ActionType>.GetType("lightningstrikeaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData origin = state.Map.GetTile(Coordinates);

        if (origin.unit != null && origin.unit.HasAbility(AMain.Capacitor) && ChargeManager.GetChargeCount(origin.unit) < ChargeManager.GetMaxCharge(origin.unit.type))
        {
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = origin.unit.owner;
            action.Coordinates = origin.unit.coordinates;
            action.Positive = true;
            state.ActionStack.Add(action);
            return;
        }

        Il2Gen.List<TileData> rodNeighbors = state.Map.GetArea(Coordinates, 1, true, false);

        int groundingImprovementCount = 0;

        foreach (TileData rodNeighbor in rodNeighbors)
        {
            if (rodNeighbor == null) continue;

            if (rodNeighbor.improvement == null)
            continue;

            ImprovementData rodNeighborData = state.GameLogicData.GetImprovementData(rodNeighbor.improvement.type);
            if (!rodNeighborData.HasAbility(AMain.Electric))
            continue;

            if (LightningManager.GetLightningStars(rodNeighborData.type) > 0)
            {
                state.ActionStack.Add(new IncreaseCurrencyAction(PlayerId, rodNeighbor.coordinates, LightningManager.GetLightningStars(rodNeighborData.type), 20));
            }
            for ( int i = 0; i < LightningManager.GetLightningPop(rodNeighborData.type); i++)
            {
                if (rodNeighbor.improvement.HasEffect(AMain.Critical)) break;

                state.ActionStack.Add(new IncreasePopulationAction(PlayerId, rodNeighbor.coordinates, rodNeighbor.rulingCityCoordinates));
            }
            if (rodNeighborData.HasAbility(AMain.CriticalAbility))
            {
                if (rodNeighbor.improvement.HasEffect(AMain.Critical))
                {
                    Il2Gen.List<TileData> batteryNeighbors = state.Map.GetTileNeighbors(rodNeighbor.coordinates);

                    foreach (TileData batteryNeighbor in batteryNeighbors)
                    {
                        if (batteryNeighbor == null) continue;
                        
                        if (batteryNeighbor.unit != null)
                        {
                            state.ActionStack.Add(new AttackAction(PlayerId, batteryNeighbor.coordinates, batteryNeighbor.coordinates, 100, false, AttackAction.AnimationType.Splash));
                        }
                    }
                    
                    if (origin.unit != null)
                    state.ActionStack.Add(new AttackAction(PlayerId, Coordinates, Coordinates, 150, false, AttackAction.AnimationType.Splash));
                    state.ActionStack.Add(new DecreasePopulationAction(PlayerId, rodNeighbor.rulingCityCoordinates, 20));
                    state.ActionStack.Add(new DecreasePopulationAction(PlayerId, rodNeighbor.rulingCityCoordinates, 20));
                    state.ActionStack.Add(new DestroyImprovementAction(PlayerId, rodNeighbor.coordinates));
                }
                else
                {
                    rodNeighbor.improvement.AddEffect(AMain.Critical);
                }
            }
            groundingImprovementCount++;
        }

        if (groundingImprovementCount == 0)
        {
            Il2Gen.List<TileData> tileNeighbors = state.Map.GetTileNeighbors(Coordinates);

            foreach (TileData tile1 in tileNeighbors)
            {
                if (tile1 == null) continue;
                
                if (tile1.unit != null)
                {
                    state.ActionStack.Add(new AttackAction(PlayerId, tile1.coordinates, tile1.coordinates, 100, false, AttackAction.AnimationType.Splash));
                }
            }
            
            if (origin.unit != null)
            state.ActionStack.Add(new AttackAction(PlayerId, Coordinates, Coordinates, 150, false, AttackAction.AnimationType.Splash));
        }
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
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

public class LightningStrikeReaction : PolibReactionBase
{
    protected LightningStrikeAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            LightningStrikeAction LightningStrikeAction = value.TryCast<LightningStrikeAction>();
            if (LightningStrikeAction != null)
            this.action = LightningStrikeAction;
            else
            Ancients.Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public LightningStrikeReaction(IntPtr ptr) : base(ptr) {}
    public LightningStrikeReaction(LightningStrikeAction action)
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
        TileData originTileData = GameManager.GameState.Map.GetTile(action.Coordinates);
        Tile originTileInstance = originTileData.GetInstance();
        if (originTileInstance == null)
        {
            onComplete.Invoke();
            return;
        }

        if (originTileInstance != null && !originTileInstance.IsHidden)
        {
            originTileInstance.Render();
            originTileInstance.Sway();

            VFXManager.SizeMappings["lightning"] = 6f;
            VFXManager.FadeInOutAnimOverrideMappings["lightning"] = new UnityEngine.Vector2(0.1f, 1f);
            VFXManager.EnsureCustomPuffRegistered("Lightning", "Puff");
            originTileInstance.DoPuff("Lightning", originTileInstance.transform, originTileInstance.VisualCenterObject.localPosition);

            if (EnumCache<SFXTypes>.TryGetType("lightning", out var type))
            {
                AudioManager.PlaySFXAtTile(type, originTileData.coordinates);
            }
            else
            {
                AMain.modLogger.LogInfo("can't find lightning sfx");
            }
            AudioManager.PlaySFXAtTile(SFXTypes.FireImpact, originTileData.coordinates);

            VFXManager.SizeMappings["dischargepuff"] = 2f;
            VFXManager.EnsureCustomPuffRegistered("DischargePuff", "Puff");
            originTileInstance.DoPuff("DischargePuff", originTileInstance.transform, originTileInstance.VisualCenterObject.localPosition);
        }

        if (originTileData.unit != null && originTileData.unit.HasAbility(AMain.Capacitor)  && ChargeManager.GetChargeCount(originTileData.unit) < ChargeManager.GetMaxCharge(originTileData.unit.type))
        {
            GameManager.DelayCall(200, onComplete);
            return;
        }

        Il2Gen.List<TileData> rodAreaTiles = GameManager.GameState.Map.GetArea(action.Coordinates, 1, true, false);

        int groundingImprovementCount = 0;

        foreach (TileData rodNeighbourTileData in rodAreaTiles)
        {
            if (rodNeighbourTileData == null) continue;

            Tile rodNeighbourTileInstance = rodNeighbourTileData.GetInstance();

            if (rodNeighbourTileInstance == null || rodNeighbourTileInstance.IsHidden) continue;

            if (rodNeighbourTileData.improvement == null)
            continue;

            if (!GameManager.GameState.GameLogicData.TryGetData(rodNeighbourTileData.improvement.type, out var data))
            continue;

            if (!data.HasAbility(AMain.Electric))
            continue;

            rodNeighbourTileInstance.Render();

            GameManager.DelayCall(100, (Il2CppSystem.Action)(() =>
            {
                VFXManager.EnsureCustomPuffRegistered("ChargePuff", "Puff");
                rodNeighbourTileInstance.DoPuff("ChargePuff", rodNeighbourTileInstance.transform, rodNeighbourTileInstance.VisualCenterObject.localPosition);
                AudioManager.PlaySFXAtTile(SFXTypes.Plop, originTileData.coordinates);
            }));

            if (data.HasAbility(AMain.CriticalAbility))
            {
                if (originTileData.improvement.HasEffect(AMain.Critical))
                {
                    rodNeighbourTileInstance.Sway();
                    rodNeighbourTileInstance.SpawnAreaDamage();
                }
                else
                {
                    rodNeighbourTileInstance.Sway();
                }
            }

            groundingImprovementCount++;
        }

        if (groundingImprovementCount == 0)
        {
            Il2Gen.List<TileData> rodNeighbors = GameManager.GameState.Map.GetTileNeighbors(action.Coordinates);

            foreach (TileData rodNeighborTileData in rodNeighbors)
            {
                if (rodNeighborTileData == null) continue;
                Tile instance2 = rodNeighborTileData.GetInstance();

                if (instance2 != null && !instance2.IsHidden)
                instance2.Sway(0.1f);
            }

            if (originTileInstance != null && !originTileInstance.IsHidden)
            {
                originTileInstance.Sway();
                originTileInstance.SpawnAreaDamage();
            }
        }

        GameManager.DelayCall(200, onComplete);
    }
}

