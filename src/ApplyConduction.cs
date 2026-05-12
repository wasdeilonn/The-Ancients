using BepInEx.Logging;
using HarmonyLib;
using Polytopia.Data;
using Polibrary;
using PolibMain = Polibrary.Main;
using AMain = Ancients.Main;
using Il2Gen = Il2CppSystem.Collections.Generic;
using UnityEngine.Tilemaps;
using Ancients;

public class ApplyConductionAction : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public WorldCoordinates Origin;
    public ApplyConductionAction(IntPtr ptr) : base(ptr) {}
    public ApplyConductionAction() {}

    public ApplyConductionAction(byte playerId, WorldCoordinates origin, WorldCoordinates coordinates) 
    : base(playerId)
    {
        base.PlayerId = playerId;
        Origin = origin;
        Coordinates = coordinates;
    }
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("applyconductionaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData tile = state.Map.GetTile(Coordinates);

        if (tile != null && tile.unit != null)
        {
            tile.unit.AddEffect(AMain.Conductive);
        }
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        base.Serialize(writer, version); //this line is important btw
        Origin.Serialize(writer, version);
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        base.Deserialize(reader, version); //leave this line in
        Origin.Deserialize(reader, version);
        Coordinates.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Origin: {2}, Coordinates: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Origin,
            this.Coordinates
        });
    }
}

public class ApplyConductionReaction : PolibReactionBase
{
    protected ApplyConductionAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            ApplyConductionAction ApplyConductionAction = value.TryCast<ApplyConductionAction>();
            if (ApplyConductionAction != null)
            this.action = ApplyConductionAction;
            else
            AMain.modLogger.LogInfo("shits fucked");
        } 
    }
    public ApplyConductionReaction(IntPtr ptr) : base(ptr) {}
    public ApplyConductionReaction(ApplyConductionAction action)
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
            instance.Sway();
            GameManager.DelayCall(200, onComplete);
        }

        onComplete.Invoke();
    }
}

