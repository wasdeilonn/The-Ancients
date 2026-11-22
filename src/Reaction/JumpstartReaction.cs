using Polibrary.PolyScript;
using Polibrary;
using Ancients.Visual;
using wLib;

namespace Ancients;
public class JumpstartReaction : PolibReactionBase
{
    protected JumpstartAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            JumpstartAction action = value.TryCast<JumpstartAction>();
            if (action != null)
            this.action = action;
        } 
    }
    public JumpstartReaction(System.IntPtr ptr) : base(ptr) {}

    public override bool ShouldFocusCamera()
    {
        return IsRecapOrOpponentAction(action);
    }

    public override WorldCoordinates GetCameraFocusCoordinates()
    {
        return action.Target;
    }

    public override void Execute(Il2CppSystem.Action onComplete)
    {
        Tile origin = GameManager.GameState.Map.GetTile(action.Origin).GetInstance();
        Tile target = GameManager.GameState.Map.GetTile(action.Target).GetInstance();

        if (!origin.IsHidden && origin != null)
        {
            origin.Unit.AnimatePathMoveJump
            (
                new List<WorldCoordinates>()
                {
                    action.Target,
                    action.Origin
                }.ToIl2List(),
                Jump.DURATION,
                (Il2CppSystem.Action) delegate
                {
                    CompletePath(onComplete, origin, target);
                }
            );
            origin.Unit.swayTween = Jump.JumpSway(origin.Unit.spriteContainer);
        }
        else onComplete.Invoke();
    }

    public void CompletePath(Il2CppSystem.Action onComplete, Tile origin, Tile target)
    {
        origin.Render();
        if (!target.IsHidden && origin != null)
        {
            target.Render();
            target.SpawnDischargePuff();
            target.Sway();
        }
        onComplete.Invoke();
    }
}