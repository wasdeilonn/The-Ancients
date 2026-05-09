using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary;
using PolibMain = Polibrary.Main;
using Il2Gen = Il2CppSystem.Collections.Generic;
using UnityEngine;

public class DischargeCommand : PolibCommandBase
{
    public int Level; 
    public WorldCoordinates Coordinates;
    public DischargeCommand(System.IntPtr ptr) : base(ptr) {}
    public DischargeCommand() {}
    public DischargeCommand(byte playerId, int level, WorldCoordinates coordinates) 
    : base(playerId)
    {
        Level = level;
        Coordinates = coordinates;
    }

    public override void ExecuteNew(GameState state)
    {
        DischargeAction action = PolibActionManager.MakeIl2CppAction<DischargeAction>();
        action.PlayerId = PlayerId;
        action.Level = Level;
        action.Coordinates = Coordinates;
        state.ActionStack.Add(action);
    }

    public override void SerializeNew(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(Level);
        Coordinates.Serialize(writer, version);
    }

    public override void DeserializeNew(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        Level = reader.ReadInt32();
        Coordinates.Deserialize(reader, version);
    }
    
    public override CommandType GetCommandType()
    {
        CommandType type = EnumCache<CommandType>.GetType("dischargecommand");
        return type;
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Level: {2}, Coordinates: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Level,
            this.Coordinates
        });
    }
}

public class DischargeAction : PolibActionBase
{
    public int Level;
    public WorldCoordinates Coordinates;
    public DischargeAction(IntPtr ptr) : base(ptr) {}
    public DischargeAction() {}

    public DischargeAction(byte playerId, int level, WorldCoordinates coordinates) 
    : base(playerId)
    {
        base.PlayerId = playerId;
        this.Level = level;
        Coordinates = coordinates;
    }
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("dischargeaction");
    }
    
    public override void Execute(GameState state)
    {
        if (!state.TryGetPlayer(base.PlayerId, out var player))
        {
            Ancients.Main.modLogger.LogError("YOU WIN!!");
            return;
        }

        UnitState unit = state.Map.GetTile(Coordinates).unit;
        int radius = (Level == 2) ? 2 : 1;
        Il2Gen.List<TileData> area = state.Map.GetArea(Coordinates, radius, true, false);

        foreach (TileData tile in area)
        {
            if (tile.unit != null && !player.HasPeaceWith(tile.unit.owner) && tile.unit.owner != base.PlayerId)
            {
                BattleResults battleResults2 = BattleHelpers.GetBattleResults(state, unit, tile.unit);
                state.ActionStack.Add(new AttackAction(base.PlayerId, Coordinates, tile.coordinates, battleResults2.attackDamage / 2, shouldMoveToTarget: false, AttackAction.AnimationType.Splash, 20));
            }
        }
        Ancients.Main.ConsumeCharge(unit);
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        base.Serialize(writer, version); //this line is important btw
        writer.Write(Level);
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        base.Deserialize(reader, version); //leave this line in
        Level = reader.ReadInt32();
        Coordinates.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Level: {2}, Coordinates: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Level,
            this.Coordinates
        });
    }
}

public class DischargeReaction : PolibReactionBase
{
    protected DischargeAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            DischargeAction dischargeAction = value.TryCast<DischargeAction>();
            if (dischargeAction != null)
            this.action = dischargeAction;
            else
            Ancients.Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public DischargeReaction(IntPtr ptr) : base(ptr) {}
    public DischargeReaction(DischargeAction action)
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
        if (instance != null && !instance.IsHidden)
        {
            instance.Render();

            VFXManager.EnsureCustomPuffRegistered("DischargePuff");
            instance.DoPuff("DischargePuff", instance.transform, instance.VisualCenterObject.localPosition);

            if (action.Level == 0)
            {
                
            }
            else if (action.Level == 1)
            {
                
            }
            else if (action.Level == 2)
            {
                PolibUtils.ShakeCamera(0.1f, 1f);
            }
            
            instance.Sway();
            
            AudioManager.PlaySFXAtTile(SFXTypes.Explode, tile.coordinates);
            GameManager.DelayCall(200, onComplete);
        }
        else
        {
            onComplete.Invoke();
        }
    }
}

