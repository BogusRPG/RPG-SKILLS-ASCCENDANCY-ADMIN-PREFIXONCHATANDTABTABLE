using CounterStrikeSharp.API.Core;
using PoE2ModRPG.Models;
using PoE2ModRPG.Services;

namespace PoE2ModRPG.Utils
{
    public static class PlayerExtensions
    {
        public static bool IsAdmin(this CCSPlayerController player, PlayerService playerService)
        {
            if (player == null || !player.IsValid)
                return false;

            var playerData = playerService.GetPlayer(player.SteamID);

            // RankValue >= 2 corresponds to Opiekun and OWNER ranks
            return playerData?.Rank.RankValue >= 2;
        }
    }
}
