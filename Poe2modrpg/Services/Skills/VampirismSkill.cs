using CounterStrikeSharp.API.Core;
using PoE2ModRPG.Models;
using System;
using System.Collections.Generic;

namespace PoE2ModRPG.Services.Skills
{
    public class VampirismSkill : Skill
    {
        public override string Name => "Vampirism";
        public override string Description => "Przez krótki czas, część zadanych obrażeń leczy Cię.";
        public override int MaxLevel => 5;

        private readonly float[] _lifestealPercent = { 0.05f, 0.08f, 0.12f, 0.16f, 0.20f };
        private readonly int[] _manaCosts = { 30, 38, 45, 52, 60 };
        private readonly int _duration = 10;

        public override int Cooldown => 10;
        public override int RequiredDex => 30;
        public override int RequiredLevel => 20;

        public override int GetManaCost(int level) => _manaCosts[level - 1];

        public override void OnLearn(Player player, int level) { }

        public override void OnActivate(PoE2ModRPG plugin, CCSPlayerController controller, Player player, int level)
        {
            var activeEffect = new ActiveVampirismEffect(player.SteamId, _lifestealPercent[level - 1]);
            plugin.AddActiveVampirismEffect(activeEffect);

            plugin.AddTimer(_duration, () =>
            {
                plugin.RemoveActiveVampirismEffect(activeEffect);
            });
        }
    }

    public class ActiveVampirismEffect
    {
        public ulong PlayerSteamId { get; }
        public float LifestealPercent { get; }

        public ActiveVampirismEffect(ulong steamId, float percent)
        {
            PlayerSteamId = steamId;
            LifestealPercent = percent;
        }
    }
}
