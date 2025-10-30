using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PoE2ModRPG.Models
{
    [Table("player_stats")]
    public class Player
    {
        [Column("steam_id")]
        public ulong SteamId { get; set; }

        [NotMapped] // Nickname is not saved to the DB, it's fetched on connect
        public string Name { get; set; } = "";

        [Column("level")]
        public int Level { get; set; } = 1;

        [Column("exp")]
        public int Exp { get; set; } = 0;

        [Column("strength")]
        public int Strength { get; set; } = 0;

        [Column("intelligence")]
        public int Intelligence { get; set; } = 0;

        [Column("dexterity")]
        public int Dexterity { get; set; } = 0;

        [Column("stat_points")]
        public int StatPoints { get; set; } = 0;

        [Column("skill_points")]
        public int SkillPoints { get; set; } = 0;

        [NotMapped]
        public int Mana { get; set; } = 100;

        [NotMapped]
        public int MaxMana { get; set; } = 100;

        [NotMapped]
        public List<PlayerSkill> LearnedSkills { get; set; } = new();

        [NotMapped]
        public PlayerRank Rank { get; set; } = new();
    }
}