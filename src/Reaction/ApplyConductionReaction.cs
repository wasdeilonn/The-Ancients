using Polibrary.PolyScript;
using Ancients.Visual;

namespace Ancients;
public class ApplyConductionReaction : PolibReactionBase
{
    protected ApplyConductionAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            ApplyConductionAction ApplyConductionAction = value.TryCast<ApplyConductionAction>();
            if (ApplyConductionAction != null)
            this.action = ApplyConductionAction;
        } 
    }
    public ApplyConductionReaction(IntPtr ptr) : base(ptr) {}
    public ApplyConductionReaction(ApplyConductionAction action)
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
            instance.SpawnChargePuff();
            instance.Sway();
            GameManager.DelayCall(100, onComplete);
            return;
        }

        onComplete.Invoke();
    }
}