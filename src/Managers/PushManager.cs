using HarmonyLib;
using Polytopia.Data;
using Polibrary.PolyScript;
using Il2Gen = Il2CppSystem.Collections.Generic;

namespace Ancients;

public static class PushManager
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(AttackCommand), nameof(AttackCommand.ExecuteDefault))]
    private static void AttackCommand_ExecuteDefault(AttackCommand __instance, GameState gameState)
    {
        TileData origin = gameState.Map.GetTile(__instance.Origin);
        TileData target = gameState.Map.GetTile(__instance.Target);

        if (origin.unit == null || target.unit == null) return;

        if (!origin.unit.HasAbility(Main.Push)) return;

        CAR.New(out PushAction action);
        action.PlayerId = __instance.PlayerId;
        action.Target = __instance.Target;
        action.Origin = __instance.Origin;
        gameState.ActionStack.Add(action);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MoveAction), nameof(MoveAction.ExecuteDefault))]
    private static void MoveAction_ExecuteDefault(MoveAction __instance, GameState gameState)
    {
        if (!gameState.TryGetUnit(__instance.UnitId, out var unit) || !gameState.TryGetPlayer(__instance.PlayerId, out var playerState) || !gameState.GameLogicData.TryGetData(unit.type, out var data))
        {
            return;
        }

        if (!unit.HasAbility(UnitAbility.Type.Stomp) || !unit.HasAbility(Main.Push)) return;

        TileData origin = gameState.Map.GetTile(__instance.Path[0]);

        foreach (TileData target in gameState.Map.GetTileNeighbors(origin.coordinates))
        {
            if (unit == null)
            {
                continue;
            }
            if (target.unit == null)
            {
                continue;
            }
            if (target.unit.owner == unit.owner || playerState.HasPeaceWith(target.unit.owner))
            {
                continue;
            }

            CAR.New(out PushAction action);
            action.PlayerId = __instance.PlayerId;
            action.Target = target.coordinates;
            action.Origin = origin.coordinates;
            gameState.ActionStack.Add(action);
        }
    }

    public static bool TryPushUnit2(GameState gameState, byte playerId, TileData tile, TileData originalTile)
    {
        GridDirection gridDirection = WorldCoordinates.ToDirection(tile.coordinates - originalTile.coordinates);
        if (tile.unit.HasFollower() && tile.unit.HasLeader())
        {
            GridDirection firstDirection = tile.unit.LeaderDirection(gameState);
            GridDirection secondDirection = tile.unit.FollowerDirection(gameState);
            gridDirection = GridDirections.Average(firstDirection, secondDirection);
        }
        else if (tile.unit.HasFollower())
        {
            gridDirection = tile.unit.FollowerDirection(gameState);
        }
        else if (tile.unit.HasLeader())
        {
            gridDirection = tile.unit.LeaderDirection(gameState);
        }
        gameState.GameLogicData.TryGetData(tile.unit.type, out var data);
        bool flag = data.IsVehicle();
        TileData tileData = null;
        for (int i = 0; i < GridDirections.COUNT; i++)
        {
            int num = (i + 1) / 2 * ((i % 2 == 0) ? 1 : (-1));
            GridDirection direction = (GridDirection)((int)(gridDirection + num + GridDirections.COUNT) % GridDirections.COUNT);
            TileData tile2 = gameState.Map.GetTile(tile.coordinates + direction.ToCoordinates());
            if (tile2 == null || tile2.unit != null)
            {
                continue;
            }
            if (tile.IsWater && !tile2.IsWater && flag && tileData == null)
            {
                tileData = tile2;
                continue;
            }
            Il2Gen.List<WorldCoordinates> path = gameState.GetPath(tile.coordinates, tile2.coordinates, 1, tile.unit);
            if (path != null)
            {
                gameState.ActionStack.Add(new MoveAction(tile.unit.owner, tile.unit.id, path, MoveAction.MoveReason.Push));
                return true;
            }
        }
        if (tileData != null)
        {
            Il2Gen.List<WorldCoordinates> path2 = gameState.GetPath(tile.coordinates, tileData.coordinates, 1, tile.unit);
            if (path2 != null)
            {
                gameState.ActionStack.Add(new MoveAction(tile.unit.owner, tile.unit.id, path2, MoveAction.MoveReason.Push));
                return true;
            }
        }
        return false;
    }
}
