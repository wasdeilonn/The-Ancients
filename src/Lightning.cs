using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary;
using PolibMain = Polibrary.Main;
using AMain = Ancients.Main;
using Il2Gen = Il2CppSystem.Collections.Generic;
using UnityEngine.Tilemaps;
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

        if (origin.unit != null && origin.unit.HasAbility(AMain.Capacitor))
        {
            ChargeAction action = PolibActionManager.MakeIl2CppAction<ChargeAction>();
            action.PlayerId = origin.unit.owner;
            action.Coordinates = origin.unit.coordinates;
            action.Positive = true;
            state.ActionStack.Add(action);
            return;
        }

        Il2Gen.List<TileData> tiles = state.Map.GetArea(Coordinates, 1, true, false);

        int groundingImprovementCount = 0;

        foreach (TileData tile in tiles)
        {
            if (tile == null) continue;

            if (tile.improvement == null)
            continue;

            ImprovementData data = state.GameLogicData.GetImprovementData(tile.improvement.type);
            if (!data.HasAbility(AMain.Electric))
            continue;

            if (LightningManager.GetLightningStars(data.type) > 0)
            {
                state.ActionStack.Add(new IncreaseCurrencyAction(tile.owner, tile.coordinates, LightningManager.GetLightningStars(data.type), 0));
            }
            if (LightningManager.GetLightningGrow(data.type) && tile.improvement.level <= data.maxLevel)
            {
                state.ActionStack.Add(new ImprovementLevelUpAction(state.CurrentPlayer, tile.coordinates));
            }
            groundingImprovementCount++;
        }

        if (groundingImprovementCount == 0)
        {
            Il2Gen.List<TileData> tileNeighbors = state.Map.GetTileNeighbors(Coordinates);

            foreach (TileData tile in tileNeighbors)
            {
                if (tile == null) continue;
                
                if (tile.unit != null)
                {
                    state.ActionStack.Add(new AttackAction(PlayerId, tile.coordinates, tile.coordinates, 100, false, AttackAction.AnimationType.Splash));
                }
            }
            
            if (origin.unit != null)
            state.ActionStack.Add(new AttackAction(PlayerId, Coordinates, Coordinates, 150, false, AttackAction.AnimationType.Splash));
        }
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
        TileData tile = GameManager.GameState.Map.GetTile(action.Coordinates);
        Tile instance = tile.GetInstance();
        if (instance == null)
        {
            onComplete.Invoke();
            return;
        }

        if (instance != null && !instance.IsHidden)
        {
            instance.Render();
            instance.Sway();

            VFXManager.SizeMappings["lightning"] = 6f;
            VFXManager.FadeInOutAnimOverrideMappings["lightning"] = new UnityEngine.Vector2(0.1f, 1f);
            VFXManager.EnsureCustomPuffRegistered("Lightning", "Puff");
            instance.DoPuff("Lightning", instance.transform, instance.VisualCenterObject.localPosition);

            if (EnumCache<SFXTypes>.TryGetType("lightning", out var type))
            {
                AudioManager.PlaySFXAtTile(type, tile.coordinates);
            }
            else
            {
                AMain.modLogger.LogInfo("can't find lightning sfx");
            }
            AudioManager.PlaySFXAtTile(SFXTypes.FireImpact, tile.coordinates);

            VFXManager.SizeMappings["dischargepuff"] = 2f;
            VFXManager.EnsureCustomPuffRegistered("DischargePuff", "Puff");
            instance.DoPuff("DischargePuff", instance.transform, instance.VisualCenterObject.localPosition);
        }

        if (tile.unit != null && tile.unit.HasAbility(AMain.Capacitor))
        {
            GameManager.DelayCall(200, onComplete);
            return;
        }

        Il2Gen.List<TileData> tiles = GameManager.GameState.Map.GetArea(action.Coordinates, 1, true, false);

        int groundingImprovementCount = 0;

        foreach (TileData tile1 in tiles)
        {
            if (tile1 == null) continue;

            Tile instance1 = tile1.GetInstance();

            if (instance1 == null || instance1.IsHidden) continue;

            if (tile1.improvement == null)
            continue;

            if (!GameManager.GameState.GameLogicData.TryGetData(tile1.improvement.type, out var data))
            continue;

            if (!data.HasAbility(AMain.Electric))
            continue;

            GameManager.DelayCall(100, (Il2CppSystem.Action)(() =>
            {
                VFXManager.EnsureCustomPuffRegistered("ChargePuff", "Puff");
                instance1.DoPuff("ChargePuff", instance1.transform, instance1.VisualCenterObject.localPosition);
                AudioManager.PlaySFXAtTile(SFXTypes.Plop, tile.coordinates);
            }));
            
            
            groundingImprovementCount++;
        }

        if (groundingImprovementCount == 0)
        {
            Il2Gen.List<TileData> tileNeighbors = GameManager.GameState.Map.GetTileNeighbors(action.Coordinates);

            foreach (TileData tile2 in tileNeighbors)
            {
                if (tile2 == null) continue;
                Tile instance2 = tile2.GetInstance();

                if (instance2 != null && !instance2.IsHidden)
                instance2.Sway(0.1f);
            }

            if (instance != null && !instance.IsHidden)
            {
                instance.Sway();
                instance.SpawnAreaDamage();
            }
        }

        GameManager.DelayCall(200, onComplete);
    }
}

