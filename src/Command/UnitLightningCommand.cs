using Polibrary.PolyScript;

namespace Ancients;
public class UnitLightningCommand : PolibCommandBase
{
    public WorldCoordinates Coordinates;
    public UnitLightningCommand(System.IntPtr ptr) : base(ptr) {}
    public UnitLightningCommand() {}
    public override void ExecuteNew(GameState state)
    {
        UnitState unit = state.Map.GetTile(Coordinates).unit;

        if (unit != null && !unit.HasAbility(Main.Capacitor))
        {
            state.ActionStack.Add(new KillUnitAction(PlayerId, Coordinates));
        }

        CAR.New(out UnitLightningAction action);
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
        CommandType type = EnumCache<CommandType>.GetType("unitlightningcommand");
        return type;
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