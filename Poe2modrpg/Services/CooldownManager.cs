using System;
using System.Collections.Generic;

namespace PoE2ModRPG.Services
{
    public class CooldownManager
    {
        private readonly Dictionary<ulong, Dictionary<string, DateTime>> _cooldowns = new();

        public void SetCooldown(ulong steamId, string skillName)
        {
            if (!_cooldowns.ContainsKey(steamId))
            {
                _cooldowns[steamId] = new Dictionary<string, DateTime>();
            }
            _cooldowns[steamId][skillName] = DateTime.UtcNow;
        }

        public bool IsOnCooldown(ulong steamId, string skillName, int cooldownSeconds)
        {
            if (_cooldowns.TryGetValue(steamId, out var playerCooldowns) && playerCooldowns.TryGetValue(skillName, out var lastUsed))
            {
                return (DateTime.UtcNow - lastUsed).TotalSeconds < cooldownSeconds;
            }
            return false;
        }

        public int GetRemainingCooldown(ulong steamId, string skillName, int cooldownSeconds)
        {
            if (_cooldowns.TryGetValue(steamId, out var playerCooldowns) && playerCooldowns.TryGetValue(skillName, out var lastUsed))
            {
                var elapsed = (DateTime.UtcNow - lastUsed).TotalSeconds;
                if (elapsed < cooldownSeconds)
                {
                    return (int)Math.Ceiling(cooldownSeconds - elapsed);
                }
            }
            return 0;
        }
    }
}
