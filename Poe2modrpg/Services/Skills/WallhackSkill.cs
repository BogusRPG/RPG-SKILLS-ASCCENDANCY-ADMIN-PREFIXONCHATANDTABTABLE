using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using PoE2ModRPG.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace PoE2ModRPG.Services.Skills
{
    public class WallhackSkill : Skill
    {
        public override string Name => "Wallhack";
        public override string Description => "Pozwala widzieć przeciwników przez ściany.";
        public override bool IsPassive => false;
        public override int Cooldown => 60;
        public override int RequiredLevel => 10;
        public override int MaxLevel => 5;

        private static readonly Dictionary<ulong, List<CBaseEntity>> _playerGlowEntities = new();
        private static readonly Dictionary<ulong, Timer> _actionTimers = new();

        public override int GetManaCost(int level) => 50 + (level - 1) * 10;

        public override void OnLearn(Player player, int level)
        {
            // This skill has no passive effect on learn, so this is empty.
        }

        public override void OnActivate(PoE2ModRPG plugin, CCSPlayerController player, Player playerData, int skillLevel)
        {
            if (player == null || !player.IsValid || !player.Pawn.IsValid) return;

            CleanupPlayer(player.SteamID);

            var glowEntities = new List<CBaseEntity>();
            _playerGlowEntities[player.SteamID] = glowEntities;

            foreach (var p in Utilities.GetPlayers())
            {
                if (p == null || !p.IsValid || !p.Pawn.IsValid || p.TeamNum == player.TeamNum || p.IsBot || p.SteamID == player.SteamID) continue;

                var pawn = p.Pawn.Value;
                if (pawn == null) continue;

                var glowEntity = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
                if (glowEntity == null) continue;

                glowEntity.SetModel(pawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
                glowEntity.DispatchSpawn();

                glowEntity.AcceptInput("FollowEntity", pawn, glowEntity, "!activator");

                glowEntity.RenderMode = RenderMode_t.kRenderGlow;
                glowEntity.Render = Color.FromArgb(255, 0, 0);
                glowEntity.Glow.GlowColorOverride = Color.FromArgb(255, 0, 0, 150);
                glowEntity.Glow.GlowRange = 10000;

                glowEntities.Add(glowEntity);
            }

            var duration = 5.0f + skillLevel;
            _actionTimers[player.SteamID] = plugin.AddTimer(duration, () => CleanupPlayer(player.SteamID));
        }

        public static void OnCheckTransmit(CCheckTransmitInfoList infoList)
        {
            var allGlowEntities = _playerGlowEntities.Values.SelectMany(list => list).ToList();
            if (allGlowEntities.Count == 0) return;

            foreach ((CCheckTransmitInfo info, CCSPlayerController? player) in infoList)
            {
                if (player == null || !player.IsValid) continue;

                List<CBaseEntity>? myGlowEntities = null;
                if (_playerGlowEntities.TryGetValue(player.SteamID, out myGlowEntities))
                {
                    // This player has WH active. They should see their props, but not others.
                }

                foreach (var glowEntity in allGlowEntities)
                {
                    if (glowEntity.IsValid)
                    {
                        if (myGlowEntities != null && myGlowEntities.Contains(glowEntity))
                        {
                            // This is one of my props, so let it transmit.
                            continue;
                        }
                        // This is a glow prop, but it's for someone else, so hide it.
                        info.TransmitEntities.Remove(glowEntity.Index);
                    }
                }
            }
        }

        public static void CleanupPlayer(ulong steamId)
        {
            if (_actionTimers.TryGetValue(steamId, out var timer))
            {
                timer.Kill();
                _actionTimers.Remove(steamId);
            }

            if (_playerGlowEntities.TryGetValue(steamId, out var entities))
            {
                foreach (var entity in entities)
                {
                    if (entity != null && entity.IsValid)
                    {
                        entity.Remove();
                    }
                }
                _playerGlowEntities.Remove(steamId);
            }
        }

        public static void CleanupAll()
        {
            var playerIds = new List<ulong>(_playerGlowEntities.Keys);
            foreach (var steamId in playerIds)
            {
                CleanupPlayer(steamId);
            }
        }
    }
}
