using Polibrary.PolyScript;

namespace Ancients;
public class PushAction : PolibActionBase
{
    public WorldCoordinates Target;
    public WorldCoordinates Origin;
    public PushAction(IntPtr ptr) : base(ptr) {}
    public PushAction() {}

    public PushAction(byte playerId, WorldCoordinates origin, WorldCoordinates target) 
    : base(playerId)
    {
        base.PlayerId = playerId;
        Origin = origin;
        Target = target;
    }
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("pushaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData origin = state.Map.GetTile(Origin);
        TileData target = state.Map.GetTile(Target);

        if (origin.unit == null || target.unit == null) return;

        if (!PushManager.TryPushUnit2(state, PlayerId, target, origin)) //do it manually instead so the game pushes target.unit based on **origin.unit's** direction
        {
            state.ActionStack.Add(new KillUnitAction(PlayerId, target.unit.coordinates));
        }
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