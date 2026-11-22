using Polibrary.PolyScript;

namespace Ancients;
public class ApplyConductionAction : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public WorldCoordinates Origin;
    public ApplyConductionAction(IntPtr ptr) : base(ptr) {}
    public ApplyConductionAction() {}
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
            tile.unit.AddEffect(Main.Conductive);
        }
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        Origin.Serialize(writer, version);
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
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