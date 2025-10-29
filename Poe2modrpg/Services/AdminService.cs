using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using PoE2ModRPG.Models;

namespace PoE2ModRPG.Services
{
    public class AdminService
    {
        private readonly PlayerService _playerService;
        private readonly LevelingService _levelingService;
        private readonly Database.DatabaseManager _dbManager;

        public AdminService(PlayerService playerService, LevelingService levelingService, Database.DatabaseManager dbManager)
        {
            _playerService = playerService;
            _levelingService = levelingService;
            _dbManager = dbManager;
        }

        public Player? FindPlayer(string playerName)
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (p != null && p.IsValid && p.PlayerName.Equals(playerName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return _playerService.GetPlayer(p.SteamID);
                }
            }
            return null;
        }

        public void GiveExperience(Player player, int amount)
        {
            _levelingService.AddExp(player, amount, Utilities.GetPlayerFromSteamId(player.SteamId), "dar od admina");
        }

        public void GiveSkillPoints(Player player, int amount)
        {
            player.SkillPoints += amount;
            _dbManager.SavePlayer(player);
        }

        public void TakeSkillPoints(Player player, int amount)
        {
            player.SkillPoints = System.Math.Max(0, player.SkillPoints - amount);
            _dbManager.SavePlayer(player);
        }

        public void GiveStatPoints(Player player, int amount)
        {
            player.StatPoints += amount;
            _dbManager.SavePlayer(player);
        }

        public void TakeStatPoints(Player player, int amount)
        {
            player.StatPoints = System.Math.Max(0, player.StatPoints - amount);
            _dbManager.SavePlayer(player);
        }

        public void ResetPlayerProgress(Player player)
        {
            player.Level = 1;
            player.Exp = 0;
            player.Strength = 0;
            player.Intelligence = 0;
            player.Dexterity = 0;
            player.StatPoints = 0;
            player.SkillPoints = 0;
            player.LearnedSkills.Clear();
            _dbManager.SavePlayer(player);
        }
    }
}
