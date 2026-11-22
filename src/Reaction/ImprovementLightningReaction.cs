using Polibrary.PolyScript;
using Ancients.Visual;
using Il2Gen = Il2CppSystem.Collections.Generic;

namespace Ancients;
public class ImprovementLightningReaction : PolibReactionBase
{
    protected ImprovementLightningAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            ImprovementLightningAction LightningStrikeAction = value.TryCast<ImprovementLightningAction>();
            if (LightningStrikeAction != null)
            this.action = LightningStrikeAction;
            else
            Main.modLogger.LogInfo("shits fucked");
        } 
    }
    public ImprovementLightningReaction(IntPtr ptr) : base(ptr) {}
    public ImprovementLightningReaction(ImprovementLightningAction action)
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
        TileData originTileData = GameManager.GameState.Map.GetTile(action.Coordinates);
        Tile originTileInstance = originTileData.GetInstance();
        if (originTileInstance == null)
        {
            onComplete.Invoke();
            return;
        }

        if (originTileInstance != null && !originTileInstance.IsHidden)
        {
            originTileInstance.Render();
            originTileInstance.Sway();
            
            originTileInstance.SpawnLightning();

            //audio
            AudioManager.PlaySFXAtTile(Main.LightningSFX, originTileData.coordinates);
            AudioManager.PlaySFXAtTile(SFXTypes.FireImpact, originTileData.coordinates);

            //explosion
            originTileInstance.SpawnDischargePuff();
        }

        foreach (WorldCoordinates coordinates in action.ToAnimate)
        {
            float delay = 0.1f;
            Tile toAnimate = MapRenderer.Current.GetTileInstance(coordinates);

            if (toAnimate != null && !toAnimate.IsHidden)
            {
                GameManager.DelayCall(delay, (Il2CppSystem.Action)delegate
                {
                    toAnimate.SpawnChargePuff();
                    toAnimate.SpawnShine();
                    AudioManager.PlaySFXAtTile(SFXTypes.Plop, coordinates);
                });

                delay += 0.1f;
            }
        }
        GameManager.DelayCall(200, onComplete);
    }
}