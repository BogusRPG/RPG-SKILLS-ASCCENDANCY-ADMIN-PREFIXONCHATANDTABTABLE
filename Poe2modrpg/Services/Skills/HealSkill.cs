using CounterStrikeSharp.API.Core;
using PoE2ModRPG.Models;
using System;

namespace PoE2ModRPG.Services.Skills
{
    public class HealSkill : Skill
    {
        public override string Name => "Heal";
        public override string Description => "Natychmiastowo przywraca punkty życia.";
        public override int MaxLevel => 5;

        private readonly int[] _healAmounts = { 20, 35, 50, 70, 100 };
        private readonly int[] _manaCosts = { 25, 30, 35, 40, 50 };

        public override int Cooldown => 20;
        public override int RequiredInt => 25;
        public override int RequiredLevel => 15;

        public override int GetManaCost(int level) => _manaCosts[level - 1];

        public override void OnLearn(Player player, int level) { }

        public override void OnActivate(PoE2ModRPG plugin, CCSPlayerController controller, Player player, int level)
        {
            var pawn = controller.PlayerPawn.Value;
            if (pawn == null) return;

            int healAmount = _healAmounts[level - 1];
            pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + healAmount);
        }
    }
}