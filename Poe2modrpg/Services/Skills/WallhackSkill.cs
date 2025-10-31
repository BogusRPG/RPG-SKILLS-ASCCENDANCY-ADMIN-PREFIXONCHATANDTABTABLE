using CounterStrikeSharp.API.Core;
using PoE2ModRPG.Models;

namespace PoE2ModRPG.Services.Skills
{
    public class WallhackSkill : Skill
    {
        public override string Name => "Wallhack";
        public override string Description => "Pozwala widzieć przeciwników przez ściany na 10 sekund.";
        public override int MaxLevel => 1;
        public override bool IsPassive => false;
        public override int Cooldown => 60;
        public override int RequiredLevel => 25;
        public override int RequiredInt => 50;
        public override int GetManaCost(int level) => 100;

        public override void OnLearn(Player player, int level) { }

        public override void OnActivate(PoE2ModRPG plugin, CCSPlayerController controller, Player player, int level)
        {
            plugin.ActivateWallhack(player.SteamId);
        }
    }
}
