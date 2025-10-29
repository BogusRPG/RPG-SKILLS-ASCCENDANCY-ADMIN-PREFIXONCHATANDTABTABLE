using PoE2ModRPG.Database;
using PoE2ModRPG.Models;
using MySqlConnector;

namespace PoE2ModRPG.Services
{
    public class RankService
    {
        private readonly DatabaseManager _dbManager;

        public RankService(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
        }

        public PlayerRank GetPlayerRank(ulong steamId)
        {
            using var connection = _dbManager.GetConnection();
            connection.Open();
            var command = new MySqlCommand("SELECT * FROM PlayerRanks WHERE SteamID = @steamId", connection);
            command.Parameters.AddWithValue("@steamId", steamId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new PlayerRank
                {
                    SteamID = reader.GetUInt64("SteamID"),
                    PlayerName = reader.GetString("PlayerName"),
                    RankValue = reader.GetInt32("RankValue")
                };
            }
            return new PlayerRank { SteamID = steamId, RankValue = 0 };
        }

        public void UpdatePlayerName(ulong steamId, string playerName)
        {
            using var connection = _dbManager.GetConnection();
            connection.Open();
            var command = new MySqlCommand(
                @"INSERT INTO PlayerRanks (SteamID, PlayerName, RankValue) VALUES (@steamId, @playerName, 0)
                  ON DUPLICATE KEY UPDATE PlayerName = @playerName;",
                connection);
            command.Parameters.AddWithValue("@steamId", steamId);
            command.Parameters.AddWithValue("@playerName", playerName);
            command.ExecuteNonQuery();
        }

        public string GetRankName(int rankValue)
        {
            return rankValue switch
            {
                1 => "Admin",
                2 => "Opiekun",
                3 => "OWNER",
                _ => "Gracz",
            };
        }
    }
}
