using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;
using wLib;

namespace Ancients;
public class UnitLightningAction : PolibActionBase
{
    public WorldCoordinates Coordinates;
    public int SetDamage = -1;
    public UnitLightningAction(IntPtr ptr) : base(ptr) {}
    public UnitLightningAction() {}
    
    public override bool IsValid(GameState state)
    {
        return true;
    }

    public override ActionType GetActionType()
    {
        return EnumCache<ActionType>.GetType("unitlightningaction");
    }
    
    public override void Execute(GameState state)
    {
        UnitState unit = state.Map.GetTile(Coordinates).unit;
        Il2Gen.List<TileData> area = state.Map.GetArea(Coordinates, 1, true, true);

        foreach (TileData tile in area)
        {
            if (tile.unit != null && !tile.unit.IsFriendly(PlayerId, state))
            {
                BattleResults battleResults2 = BattleHelpers.GetBattleResults(state, unit, tile.unit);

                int dmg = (SetDamage == -1) ? battleResults2.attackDamage : SetDamage;

                state.ActionStack.Add(new AttackAction(base.PlayerId, Coordinates, tile.coordinates, dmg, shouldMoveToTarget: false, AttackAction.AnimationType.Splash, 20));
            }
        }

        if (unit.HasAbility(Main.Capacitor))
        {
            CAR.New(out ChargeAction action);
            action.PlayerId = unit.owner;
            action.Coordinates = unit.coordinates;
            action.Positive = false;
            state.ActionStack.Add(action);
        }

        unit.RemoveEffect(Main.Conductive);
        
        unit.MakeExhauseted(state);
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(PlayerId); //this line is important btw
        Coordinates.Serialize(writer, version);
        writer.Write(SetDamage);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        PlayerId = reader.ReadByte(); //leave this line in
        Coordinates.Deserialize(reader, version);
        SetDamage = reader.ReadInt32();
    }

    public override string ToString()
    {
        return string.Format("{0} (PlayerId: {1}, Coordinates: {2}, SetDamage: {3})", new object[]
        {
            base.GetType(),
            base.PlayerId,
            this.Coordinates,
            this.SetDamage
        });
    }
}