using Polibrary;
using AMain = Ancients.Main;
using Ancients;

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
            if (ChargeManager.GetChargeCount(tile.unit) < ChargeManager.GetMaxCharge(tile.unit.type))
            {
                tile.unit.effects.Add(AMain.Charged);
            }
        }
        else
        {
            for (int i = 0; i < ChargeManager.GetChargeConsumptionAmount(tile.unit.type); i++)
            {
                tile.unit.RemoveEffect(AMain.Charged);
            }
        }
    }

    public override void Serialize(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        // base.Serialize(writer, version);
        writer.Write(PlayerId);  // The safe way.
        writer.Write(Positive);
        Coordinates.Serialize(writer, version);
    }

    public override void Deserialize(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        // base.Deserialize(reader, version);
        PlayerId = reader.ReadByte(); // The safe way.
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

public class ChargeReaction : PolibReactionBase
{
    protected ChargeAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            ChargeAction ChargeAction = value.TryCast<ChargeAction>();
            if (ChargeAction != null)
            this.action = ChargeAction;
            else
            Ancients.Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public ChargeReaction(IntPtr ptr) : base(ptr) {}
    public ChargeReaction(ChargeAction action)
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

            if (action.Positive)
            {
                VFXManager.EnsureCustomPuffRegistered("ChargePuff", "Puff");
                instance.DoPuff("ChargePuff", instance.transform, instance.VisualCenterObject.localPosition);
                AudioManager.PlaySFXAtTile(SFXTypes.Connect, tile.coordinates);
            }
            else
            {
                instance.Sway();
                AudioManager.PlaySFXAtTile(SFXTypes.Explode, tile.coordinates);
            }
            onComplete.Invoke();
        }
        else
        {
            onComplete.Invoke();
        }
    }
}

