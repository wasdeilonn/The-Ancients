using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary;
using PolibMain = Polibrary.Main;
using AMain = Ancients.Main;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Il2CppSystem;

public class ExcavateCommand : PolibCommandBase
{
    public WorldCoordinates Coordinates;
    public ExcavateCommand(System.IntPtr ptr) : base(ptr) {}
    public ExcavateCommand() {}
    public ExcavateCommand(byte playerId, WorldCoordinates coordinates) 
    : base(playerId)
    {
        Coordinates = coordinates;
    }
    public override bool IsValid(GameState state, out string validationError)
    {
        if (!base.PassesBasicValidation(state, out validationError))
        {
            return false;
        }
        if (state.Map.GetTile(Coordinates).improvement != null)
        {
            validationError = VALIDATION_ERROR_CANT_BUILD;
            return false;
        }
        validationError = null;
        return true;
    }

    public override void ExecuteNew(GameState state)
    {
        ExcavateAction action = PolibActionManager.MakeIl2CppAction<ExcavateAction>();
        action.PlayerId = PlayerId;
        action.Coordinates = Coordinates;
        state.ActionStack.Add(action);
    }

    public override void SerializeNew(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        Coordinates.Serialize(writer, version);
    }

    public override void DeserializeNew(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        Coordinates.Deserialize(reader, version);
    }
    
    public override CommandType GetCommandType()
    {
        CommandType type = EnumCache<CommandType>.GetType("excavatecommand");
        return type;
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Level: {2}, Coordinates: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Coordinates
        });
    }
}

public class ExcavateAction : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public WorldCoordinates UnitHome;
    public ExcavateAction(System.IntPtr ptr) : base(ptr) {}
    public ExcavateAction() {}

    public ExcavateAction(byte playerId, WorldCoordinates coordinates) 
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
        return EnumCache<ActionType>.GetType("excavateaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData tile = state.Map.GetTile(Coordinates);
        if (tile != null && state.GameLogicData.TryGetData(ImprovementData.Type.Ruin, out var data))
        {
            tile.improvement = new ImprovementState
            {
                type = ImprovementData.Type.Ruin,
                borderSize = (ushort)data.borderSize,
                level = 0,
                xp = 0,
                production = 1,
                founded = (ushort)state.CurrentTurn,
                baseScore = (ushort)data.GetScoreReward(),
                founder = base.PlayerId
            };
        }
        UnitHome = tile.unit.home;
        ActionUtils.KillUnit(state, tile);
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        Coordinates.Serialize(writer, version);
        UnitHome.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
        Coordinates.Deserialize(reader, version);
        UnitHome.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Coordinates: {2}, UnitHome {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Coordinates,
            this.UnitHome
        });
    }
}

public class ExcavateReaction : PolibReactionBase
{
    protected ExcavateAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            ExcavateAction action = value.TryCast<ExcavateAction>();
            if (action != null)
            this.action = action;
            else
            AMain.modLogger.LogInfo("shits fucked");
        } 
    }
    public ExcavateReaction(System.IntPtr ptr) : base(ptr) {}
    public ExcavateReaction(ExcavateAction action)
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
            instance.SpawnShine();
            instance.Sway();
            AudioManager.PlaySFXAtTile(SFXTypes.Capture, tile.coordinates);
            GameManager.DelayCall(200, onComplete);
        }
        if (GameManager.GameState.Map.GetTile(action.UnitHome) != null)
        {
            Tile homeInstance = GameManager.GameState.Map.GetTile(action.UnitHome).GetInstance();
            if (homeInstance != null)
            {
                homeInstance.Render();
            }
        }
        else
        {
            onComplete.Invoke();
        }
    }
}

