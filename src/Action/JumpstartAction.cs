using DG.Tweening;
using Polibrary.PolyScript;
using wLib;

namespace Ancients;
public class JumpstartAction : PolibActionBase
{
    public WorldCoordinates Target;
    public WorldCoordinates Origin;
    public JumpstartAction(IntPtr ptr) : base(ptr) {}
    public JumpstartAction() {}
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("jumpstartaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData origin = state.Map.GetTile(Origin);
        TileData target = state.Map.GetTile(Target);

        if (origin.unit == null || target.unit == null) return;

        CAR.New(out ApplyConductionAction action);
        action.PlayerId = PlayerId;
        action.Coordinates = target.coordinates;
        AddSubAction(action);

        foreach (TileData tile1 in state.Map.GetTileNeighbors(Target))
        {
            if (tile1.unit != null && !tile1.unit.IsFriendly(PlayerId, state) && tile1.coordinates != Target)
            {
                CAR.New(out ApplyConductionAction action1);
                action1.PlayerId = PlayerId;
                action1.Coordinates = tile1.coordinates;
                AddSubAction(action1);

                foreach (TileData tile2 in state.Map.GetTileNeighbors(tile1.coordinates))
                {
                    if (tile2.unit != null && !tile2.unit.IsFriendly(PlayerId, state) && tile2.coordinates != tile1.coordinates)
                    {
                        CAR.New(out ApplyConductionAction action2);
                        action2.PlayerId = PlayerId;
                        action2.Coordinates = tile2.coordinates;
                        AddSubAction(action2);
                    }
                }
            }
        }

        origin.SetUnit(null);

        CommitSubActionsToStack(state.ActionStack);
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        Origin.Serialize(writer, version);
        Target.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
        Origin.Deserialize(reader, version);
        Target.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Origin: {2}, Coordinates: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Origin,
            this.Target
        });
    }
}