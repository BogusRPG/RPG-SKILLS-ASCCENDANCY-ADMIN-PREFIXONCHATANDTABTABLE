using System;
using System.Collections.Generic;
using CounterStrikeSharp.API.Core;
using PoE2ModRPG.Models;

namespace PoE2ModRPG.Services
{
    public abstract class Skill
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract int MaxLevel { get; }
        public virtual bool IsPassive => false;

        public virtual int Cooldown => 0; // in seconds
        public virtual int RequiredLevel => 0;
        public virtual int RequiredStr => 0;
        public virtual int RequiredInt => 0;
        public virtual int RequiredDex => 0;

        public virtual int GetManaCost(int level) => 0;

        public abstract void OnLearn(Player player, int level);
        public virtual void OnActivate(PoE2ModRPG plugin, CCSPlayerController controller, Player player, int level) { }
    }

    public class SkillManager
    {
        private readonly Dictionary<string, Skill> _skills = new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);

        public SkillManager()
        {
            // Skills will be registered here
        }

        public void RegisterSkill(Skill skill)
        {
            _skills[skill.Name] = skill;
        }

        public Skill? GetSkill(string name)
        {
            _skills.TryGetValue(name, out var skill);
            return skill;
        }

        public IEnumerable<Skill> GetAllSkills() => _skills.Values;
    }
}