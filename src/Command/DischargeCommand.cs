using Polibrary.PolyScript;

namespace Ancients;
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
        CAR.New(out DischargeAction action);
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