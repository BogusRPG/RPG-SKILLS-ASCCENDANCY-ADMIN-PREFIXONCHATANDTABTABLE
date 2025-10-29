using CounterStrikeSharp.API.Core;
using PoE2ModRPG.Models;

namespace PoE2ModRPG.Services.Skills
{
    public class SpeedBoostSkill : Skill
    {
        public override string Name => "Speed Boost";
        public override string Description => "Tymczasowo zwiększa prędkość poruszania się.";
        public override int MaxLevel => 5;

        private readonly float[] _speedMultipliers = { 1.1f, 1.15f, 1.2f, 1.25f, 1.3f };
        private readonly int[] _durations = { 5, 6, 7, 8, 10 }; // in seconds
        private readonly int[] _manaCosts = { 20, 25, 30, 35, 40 };

        public override int Cooldown => 15;
        public override int RequiredDex => 20;
        public override int RequiredLevel => 10;

        public override int GetManaCost(int level) => _manaCosts[level - 1];

        public override void OnLearn(Player player, int level) { }

        public override void OnActivate(PoE2ModRPG plugin, CCSPlayerController controller, Player player, int level)
        {
            var pawn = controller.PlayerPawn.Value;
            if (pawn == null) return;

            float originalSpeed = pawn.Speed;
            pawn.Speed *= _speedMultipliers[level - 1];

            plugin.AddTimer(_durations[level - 1], () =>
            {
                if (pawn.IsValid)
                {
                    pawn.Speed = originalSpeed;
                }
            });
        }
    }
}
