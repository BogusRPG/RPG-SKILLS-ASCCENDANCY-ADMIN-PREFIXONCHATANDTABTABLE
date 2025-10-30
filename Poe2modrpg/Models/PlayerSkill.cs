using System.ComponentModel.DataAnnotations.Schema;

namespace PoE2ModRPG.Models
{
    [Table("player_skills")]
    public class PlayerSkill
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("player_steam_id")]
        public ulong PlayerSteamId { get; set; }

        [Column("skill_name")]
        public string SkillName { get; set; } = "";

        [Column("level")]
        public int Level { get; set; } = 1;
    }
}