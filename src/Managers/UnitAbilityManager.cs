using HarmonyLib;
using Polytopia.Data;
using Il2Gen = Il2CppSystem.Collections.Generic;
using Polibrary.PolyScript;

namespace Ancients;

public static class UnitAbilityManager
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommandUtils), nameof(CommandUtils.GetUnitActions))]
    private static void AddCommands(ref Il2Gen.List<CommandBase> __result, GameState gameState, PlayerState player, TileData tile, bool includeUnavailable)
    {
        if (tile.unit == null || tile.unit.owner != player.Id) return;

        if (tile.unit.HasAbility(Main.Discharge) && tile.unit.HasAbility(Main.Capacitor) && !tile.unit.moved && !tile.unit.attacked && ChargeManager.GetChargeCount(tile.unit) > 0)
        {
            CAR.New<DischargeCommand>(out var command);
            command.Coordinates = tile.coordinates;
            command.PlayerId = player.Id;
            command.Level = ChargeManager.GetChargeCount(tile.unit) - 1; //subtract 1 cause its 0 based
            CommandUtils.AddCommand(gameState, __result, command, includeUnavailable);
        }

        if (tile.unit.HasAbility(Main.UnitLightning) && ((!tile.unit.attacked && !tile.unit.moved) || tile.unit.HasAbility(UnitAbility.Type.Dash)))
        {
            bool flag = false;
            if (tile.unit.HasAbility(Main.Capacitor))
            {
                if (ChargeManager.GetChargeCount(tile.unit) > 0)
                {
                    flag = true;
                }
            }
            else
            {
                flag = true;
            }

            if (flag)
            {
                CAR.New(out UnitLightningCommand command);
                command.Coordinates = tile.coordinates;
                command.PlayerId = player.Id;
                CommandUtils.AddCommand(gameState, __result, command, includeUnavailable);
            }
        }
    }
}