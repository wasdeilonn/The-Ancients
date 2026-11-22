using Polibrary.PolyScript;

namespace Ancients;
public class ChargeAction : PolibActionBase
{
    public bool Positive;
    public WorldCoordinates Coordinates;
    public ChargeAction(IntPtr ptr) : base(ptr) {}
    public ChargeAction() {}

    public ChargeAction(byte playerId, bool positive, WorldCoordinates coordinates) 
    : base(playerId)
    {
        base.PlayerId = playerId;
        this.Positive = positive;
        Coordinates = coordinates;
    }
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("chargeaction");
    }
    
    public override void Execute(GameState state)
    {
        TileData tile = state.Map.GetTile(Coordinates);
        if (tile.unit == null) return;

        if (Positive)
        {
            if (tile.unit.GetChargeCount() < tile.unit.GetMaxCharge())
            {
                tile.unit.effects.Add(Main.Charged);
            }
        }
        else
        {
            for (int i = 0; i < tile.unit.GetChargeConsumptionAmount(); i++)
            {
                tile.unit.RemoveEffect(Main.Charged);
            }
        }
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        writer.Write(Positive);
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
        Positive = reader.ReadBoolean();
        Coordinates.Deserialize(reader, version);
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Positive: {2}, Coordinates: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Positive,
            this.Coordinates
        });
    }
}