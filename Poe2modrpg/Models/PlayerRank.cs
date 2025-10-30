using System.ComponentModel.DataAnnotations.Schema;

namespace PoE2ModRPG.Models
{
    [Table("PlayerRanks")]
    public class PlayerRank
    {
        [Column("SteamID")]
        public ulong SteamID { get; set; }

        [Column("PlayerName")]
        public string PlayerName { get; set; } = "";

        [Column("RankValue")]
        public int RankValue { get; set; } = 0;
    }
}