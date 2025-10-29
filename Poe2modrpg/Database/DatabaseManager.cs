using System;
using MySqlConnector;
using PoE2ModRPG.Configs;
using PoE2ModRPG.Models;
using CounterStrikeSharp.API;

namespace PoE2ModRPG.Database
{
    public class DatabaseManager
    {
        private readonly string _connectionString;

        public DatabaseManager(DBConfig config)
        {
            _connectionString = new MySqlConnectionStringBuilder
            {
                Server = config.Host,
                Port = (uint)config.Port,
                UserID = config.User,
                Password = config.Password,
                Database = config.Database
            }.ConnectionString;
        }

        public MySqlConnection GetConnection() => new(_connectionString);

        public void InitializeDatabase()
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                var command = new MySqlCommand(
                    @"CREATE TABLE IF NOT EXISTS player_stats (
                        steam_id BIGINT(20) NOT NULL PRIMARY KEY,
                        level INT NOT NULL DEFAULT 1,
                        exp INT NOT NULL DEFAULT 0,
                        strength INT NOT NULL DEFAULT 0,
                        intelligence INT NOT NULL DEFAULT 0,
                        dexterity INT NOT NULL DEFAULT 0,
                        stat_points INT NOT NULL DEFAULT 0,
                        skill_points INT NOT NULL DEFAULT 0
                    );", connection);
                command.ExecuteNonQuery();

                command.CommandText = @"CREATE TABLE IF NOT EXISTS player_skills (
                                            id INT AUTO_INCREMENT PRIMARY KEY,
                                            player_steam_id BIGINT(20) NOT NULL,
                                            skill_name VARCHAR(255) NOT NULL,
                                            level INT NOT NULL,
                                            FOREIGN KEY (player_steam_id) REFERENCES player_stats(steam_id) ON DELETE CASCADE
                                        );";
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Server.PrintToConsole($"[PoE2ModRPG] Błąd inicjalizacji bazy danych: {e.Message}");
            }
        }

        public Player LoadPlayer(ulong steamId)
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                var command = new MySqlCommand("SELECT * FROM player_stats WHERE steam_id = @steam_id", connection);
                command.Parameters.AddWithValue("@steam_id", steamId);

                Player player;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) return new Player { SteamId = steamId };
                    player = new Player
                    {
                        SteamId = reader.GetUInt64("steam_id"),
                        Level = reader.GetInt32("level"),
                        Exp = reader.GetInt32("exp"),
                        Strength = reader.GetInt32("strength"),
                        Intelligence = reader.GetInt32("intelligence"),
                        Dexterity = reader.GetInt32("dexterity"),
                        StatPoints = reader.GetInt32("stat_points"),
                        SkillPoints = reader.GetInt32("skill_points")
                    };
                }

                command.CommandText = "SELECT * FROM player_skills WHERE player_steam_id = @steam_id";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        player.LearnedSkills.Add(new PlayerSkill
                        {
                            Id = reader.GetInt32("id"),
                            PlayerSteamId = reader.GetUInt64("player_steam_id"),
                            SkillName = reader.GetString("skill_name"),
                            Level = reader.GetInt32("level")
                        });
                    }
                }
                return player;
            }
            catch (Exception e)
            {
                Server.PrintToConsole($"[PoE2ModRPG] Błąd wczytywania gracza ({steamId}): {e.Message}");
            }
            return new Player { SteamId = steamId };
        }

        public void SavePlayer(Player player)
        {
            try
            {
                using var connection = GetConnection();
                connection.Open();
                var command = new MySqlCommand(
                    @"INSERT INTO player_stats (steam_id, level, exp, strength, intelligence, dexterity, stat_points, skill_points)
                        VALUES (@steam_id, @level, @exp, @strength, @intelligence, @dexterity, @stat_points, @skill_points)
                        ON DUPLICATE KEY UPDATE
                            level = VALUES(level),
                            exp = VALUES(exp),
                            strength = VALUES(strength),
                            intelligence = VALUES(intelligence),
                            dexterity = VALUES(dexterity),
                            stat_points = VALUES(stat_points),
                            skill_points = VALUES(skill_points);",
                    connection);

                command.Parameters.AddWithValue("@steam_id", player.SteamId);
                command.Parameters.AddWithValue("@level", player.Level);
                command.Parameters.AddWithValue("@exp", player.Exp);
                command.Parameters.AddWithValue("@strength", player.Strength);
                command.Parameters.AddWithValue("@intelligence", player.Intelligence);
                command.Parameters.AddWithValue("@dexterity", player.Dexterity);
                command.Parameters.AddWithValue("@stat_points", player.StatPoints);
                command.Parameters.AddWithValue("@skill_points", player.SkillPoints);

                command.ExecuteNonQuery();

                // First, remove all existing skills for the player to handle updates and removals.
                command.CommandText = "DELETE FROM player_skills WHERE player_steam_id = @steam_id";
                command.ExecuteNonQuery();

                // Now, insert the current skills.
                foreach (var skill in player.LearnedSkills)
                {
                    command.CommandText = @"INSERT INTO player_skills (player_steam_id, skill_name, level)
                                                VALUES (@steam_id, @skill_name, @level)";
                    command.Parameters.AddWithValue("@skill_name", skill.SkillName);
                    command.Parameters.AddWithValue("@level", skill.Level);
                    command.ExecuteNonQuery();
                    command.Parameters.RemoveAt("@skill_name");
                    command.Parameters.RemoveAt("@level");
                }
            }
            catch (Exception e)
            {
                Server.PrintToConsole($"[PoE2ModRPG] Błąd zapisu gracza ({player.SteamId}): {e.Message}");
            }
        }
    }
}
