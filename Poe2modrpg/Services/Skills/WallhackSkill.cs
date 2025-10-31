using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Utils;
using PoE2ModRPG.Models;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;

namespace PoE2ModRPG.Services.Skills
{
    public class WallhackSkill : Skill
    {
        public override string Name => "Wallhack";
        public override string Description => "Pozwala widzieć przeciwników przez ściany.";
        public override int MaxLevel => 3;

        private readonly int[] _durations = { 5, 7, 10 };
        private readonly int[] _manaCosts = { 40, 50, 60 };

        public override int Cooldown => 60;
        public override int RequiredInt => 25;
        public override int RequiredLevel => 15;

        public static readonly ConcurrentDictionary<ulong, byte> PlayersInAction = new();
        public static readonly ConcurrentBag<(CDynamicProp, CDynamicProp, CsTeam)> Glows = new();

        public override int GetManaCost(int level) => _manaCosts[level - 1];

        public override void OnLearn(Player player, int level) { }

        public override void OnActivate(PoE2ModRPG plugin, CCSPlayerController controller, Player player, int level)
        {
            PlayersInAction.TryAdd(controller.SteamID, 0);
            if (Glows.IsEmpty)
            {
                SetGlowEffectForAll(controller);
            }

            plugin.AddTimer(_durations[level - 1], () => OnDeactivate(controller));
        }

        private void OnDeactivate(CCSPlayerController controller)
        {
            PlayersInAction.TryRemove(controller.SteamID, out _);
            if (PlayersInAction.IsEmpty)
            {
                Cleanup();
            }
        }

        public static void Cleanup()
        {
            foreach (var glow in Glows)
            {
                if (glow.Item1 != null && glow.Item1.IsValid) glow.Item1.Remove();
                if (glow.Item2 != null && glow.Item2.IsValid) glow.Item2.Remove();
            }
            Glows.Clear();
        }

        public static bool IsPlayerUsing(ulong steamId) => PlayersInAction.ContainsKey(steamId);

        private void SetGlowEffectForAll(CCSPlayerController activator)
        {
            foreach (var enemy in Utilities.GetPlayers().Where(p => p.IsValid && p.PawnIsAlive && p.TeamNum != activator.TeamNum))
            {
                var enemyPawn = enemy.PlayerPawn.Value;
                if (enemyPawn == null) continue;

                var modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
                var modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");

                if (modelRelay == null || modelGlow == null) return;

                modelRelay.SetModel(enemyPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
                modelRelay.Spawnflags = 256u;
                modelRelay.RenderMode = RenderMode_t.kRenderNone;
                modelRelay.DispatchSpawn();

                modelGlow.SetModel(enemyPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName);
                modelGlow.Spawnflags = 256u;
                modelGlow.Render = Color.FromArgb(1, 255, 255, 255);
                modelGlow.DispatchSpawn();

                modelGlow.Glow.GlowColorOverride = enemy.TeamNum == (byte)CsTeam.Terrorist ? Color.FromArgb(255, 255, 165, 0) : Color.FromArgb(255, 173, 216, 230);
                modelGlow.Glow.GlowRange = 5000;
                modelGlow.Glow.GlowTeam = -1;
                modelGlow.Glow.GlowType = 3;
                modelGlow.Glow.GlowRangeMin = 100;

                modelRelay.AcceptInput("FollowEntity", enemyPawn, modelRelay, "!activator", 0);
                modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator", 0);

                Glows.Add((modelRelay, modelGlow, (CsTeam)enemy.TeamNum));
            }
        }

        public static void CheckTransmit(CCheckTransmitInfoList infoList)
        {
            foreach (var (info, player) in infoList)
            {
                if (player == null) continue;

                var observerTarget = player.Pawn?.Value?.ObserverServices?.ObserverTarget?.Value;
                var observedPlayer = observerTarget != null ? Utilities.GetPlayerFromPawn(observerTarget) : null;

                bool shouldSeeGlow = IsPlayerUsing(player.SteamID) || (observedPlayer != null && IsPlayerUsing(observedPlayer.SteamID));

                foreach (var glow in Glows)
                {
                    bool isEnemyGlow = glow.Item3 != (CsTeam)player.TeamNum;

                    if (shouldSeeGlow && isEnemyGlow)
                    {
                        // Player has WH and the glow is for an enemy, so they should see it. Don't remove transmission.
                        continue;
                    }

                    // For all other cases (player doesn't have WH, or the glow is for a teammate), remove the glow.
                    var glowEntity1 = Utilities.GetEntityFromIndex<CBaseEntity>((int)glow.Item1.Index);
                    if (glowEntity1 != null && glowEntity1.IsValid) info.TransmitEntities.Remove(glowEntity1.Index);

                    var glowEntity2 = Utilities.GetEntityFromIndex<CBaseEntity>((int)glow.Item2.Index);
                    if (glowEntity2 != null && glowEntity2.IsValid) info.TransmitEntities.Remove(glowEntity2.Index);
                }
            }
        }
    }
}
