using Polibrary.PolyScript;
using Ancients.Visual;

namespace Ancients;
public class DischargeReaction : PolibReactionBase
{
    protected DischargeAction action;
    public override ActionBase actionProperty 
    { 
        get => this.action; 
        set
        {
            DischargeAction dischargeAction = value.TryCast<DischargeAction>();
            if (dischargeAction != null)
            this.action = dischargeAction;
        } 
    }
    public DischargeReaction(IntPtr ptr) : base(ptr) {}
    public DischargeReaction(DischargeAction action)
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
            
            if (action.Level == 0)
            {
                instance.SpawnAreaDamage();
                instance.SpawnDischargePuff();
            }
            else if (action.Level == 1)
            {
                VFXManager.ShakeCamera(0.1f, 0.5f);
                instance.SpawnAreaDamage();
                instance.SpawnDischargePuff();

                foreach (TileData tileNeighbor in GameManager.GameState.Map.GetTileNeighbors(instance.Coordinates))
                {
                    MapRenderer.Current.GetTileInstance(tileNeighbor.coordinates).Sway();
                }
            }
            else if (action.Level == 2)
            {
                VFXManager.ShakeCamera(0.1f, 1f);
                instance.SpawnDischargePuffLarge();
                instance.SpawnDischargeBlast();

                List<Tile> doneTiles = new();

                foreach (TileData tileNeighbor in GameManager.GameState.Map.GetTileNeighbors(instance.Coordinates))
                {
                    MapRenderer.Current.GetTileInstance(tileNeighbor.coordinates).Sway();
                    doneTiles.Add(MapRenderer.Current.GetTileInstance(tileNeighbor.coordinates));
                }

                foreach (TileData tileData in GameManager.GameState.Map.GetArea(instance.Coordinates, 2, true, true))
                {
                    if (!doneTiles.Contains(MapRenderer.Current.GetTileInstance(tileData.coordinates)))
                    MapRenderer.Current.GetTileInstance(tileData.coordinates).Sway(0.1f);
                }
            }
            
            instance.Sway();
            
            AudioManager.PlaySFXAtTile(SFXTypes.Explode, tile.coordinates);
            GameManager.DelayCall(200, onComplete);
        }
        else
        {
            onComplete.Invoke();
        }
    }
}