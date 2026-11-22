using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;

namespace Ancients;
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
            Main.modLogger.LogError("YOU WIN!!");
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
                state.ActionStack.Add(new AttackAction(base.PlayerId, Coordinates, tile.coordinates, battleResults2.attackDamage, shouldMoveToTarget: false, AttackAction.AnimationType.Splash, 20));
            }
        }

        CAR.New(out ChargeAction action);
        action.PlayerId = unit.owner;
        action.Coordinates = unit.coordinates;
        action.Positive = false;
        state.ActionStack.Add(action);

        unit.MakeExhauseted(state);
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        writer.Write(Level);
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
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