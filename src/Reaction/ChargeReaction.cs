using Polibrary.PolyScript;
using Ancients.Visual;

namespace Ancients;
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
                instance.SpawnChargePuff();

                if (tile.unit != null && tile.unit.GetChargeCount() == tile.unit.GetMaxCharge())
                {
                    AudioManager.PlaySFXAtTile(Main.HighSFX, tile.coordinates);
                }
                else
                {
                    AudioManager.PlaySFXAtTile(Main.LowSFX, tile.coordinates);
                }
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