using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using PoE2ModRPG.Models;
using CounterStrikeSharp.API.Modules.Utils;

namespace PoE2ModRPG.Services
{
    public class LevelingService
    {
        private readonly PlayerService _playerService;
        private readonly Database.DatabaseManager _dbManager;
        private static readonly string Prefix = $" {ChatColors.Gold}[PoE2Mod]{ChatColors.Default}";

        public LevelingService(PlayerService playerService, Database.DatabaseManager dbManager)
        {
            _playerService = playerService;
            _dbManager = dbManager;
        }

        private int RequiredExp(int level) => 100 + (level - 1) * 50;

        public void AddExp(Player playerData, int amount, CCSPlayerController? player, string reason)
        {
            playerData.Exp += amount;
            if (player != null && player.IsValid)
            {
                player.PrintToChat($"{Prefix} {ChatColors.Green}+{amount} EXP{ChatColors.Default} za {reason}");
            }

            while (playerData.Level < 100 && playerData.Exp >= RequiredExp(playerData.Level))
            {
                playerData.Exp -= RequiredExp(playerData.Level);
                playerData.Level++;
                playerData.StatPoints++;
                if (playerData.Level % 5 == 0)
                {
                    playerData.SkillPoints++;
                }

                if (player != null && player.IsValid)
                {
                    Server.PrintToChatAll($"{Prefix} Gracz {player.PlayerName} awansował na poziom {playerData.Level}!");
                }
            }

            if (player != null && player.IsValid)
            {
                player.PrintToChat($"{Prefix} Poziom: {playerData.Level} | {ChatColors.Yellow}EXP: {playerData.Exp}/{RequiredExp(playerData.Level)}");
            }

            _dbManager.SavePlayer(playerData);
        }
    }
}
