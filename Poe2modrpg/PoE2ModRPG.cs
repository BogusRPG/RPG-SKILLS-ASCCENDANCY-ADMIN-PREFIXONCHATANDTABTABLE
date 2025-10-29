#nullable enable
using System;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using PoE2ModRPG.API;
using PoE2ModRPG.Configs;
using PoE2ModRPG.Database;
using PoE2ModRPG.Models;
using PoE2ModRPG.Services;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;
using MySqlConnector;

namespace PoE2ModRPG
{
    [MinimumApiVersion(343)]
    public class PoE2ModRPG : BasePlugin, IPluginConfig<PluginConfig>, IPoe2ModApi
    {
        public override string ModuleName => "PoE2ModRPG";
        public override string ModuleVersion => "0.3.0";
        public override string ModuleAuthor => "Patryk & Jules";
        public static IPoe2ModApi Instance { get; private set; } = null!;

        private static readonly string Prefix = $" {ChatColors.Gold}[PoE2Mod]{ChatColors.Default}";
        private readonly Dictionary<ulong, Timer> _manaRegenTimers = new();
        private readonly List<Services.Skills.ActiveVampirismEffect> _activeVampirismEffects = new();

        private DatabaseManager _dbManager = null!;
        private PlayerService _playerService = null!;
        private LevelingService _levelingService = null!;
        private SkillManager _skillManager = null!;
        private CooldownManager _cooldownManager = null!;

        public PluginConfig Config { get; set; } = new();
        public void OnConfigParsed(PluginConfig config) => Config = config;

        private const int ExpKill = 20;
        private const int ExpHeadshotKill = 30;
        private const int ExpAssist = 10;
        private const int ExpBombPlant = 25;
        private const int ExpBombDefuse = 25;
        private const int ExpRoundWinPerPlayer = 15;
        private const int ExpKnifeKill = 75;

        public override void Load(bool hotReload)
        {
            Instance = this;

            try
            {
                _dbManager = new DatabaseManager(Config.Database);
                _dbManager.InitializeDatabase();
                _playerService = new PlayerService();
                _levelingService = new LevelingService(_playerService, _dbManager);
                _skillManager = new SkillManager();
                _skillManager.RegisterSkill(new Services.Skills.HealSkill());
                _skillManager.RegisterSkill(new Services.Skills.SpeedBoostSkill());
                _skillManager.RegisterSkill(new Services.Skills.VampirismSkill());
                _cooldownManager = new CooldownManager();
                Server.PrintToConsole("PoE2ModRPG: Plugin działa i DB init OK");
            }
            catch (Exception e)
            {
                Server.PrintToConsole($"[PoE2ModRPG] FATAL: Failed to initialize database: {e.Message}");
                throw;
            }

            AddCommand("staty", "Pokazuje statystyki", OnStatsCommand);
            AddCommand("poe_staty", "Pokazuje statystyki", OnStatsCommand);
            AddCommand("str", "Dodaje punkty do Siły", OnStrCommand);
            AddCommand("poe_str", "Dodaje punkty do Siły", OnStrCommand);
            AddCommand("int", "Dodaje punkty do Inteligencji", OnIntCommand);
            AddCommand("poe_int", "Dodaje punkty do Inteligencji", OnIntCommand);
            AddCommand("dex", "Dodaje punkty do Zręczności", OnDexCommand);
            AddCommand("poe_dex", "Dodaje punkty do Zręczności", OnDexCommand);
            AddCommand("reset", "Resetuje statystyki", OnResetCommand);
            AddCommand("poe_reset", "Resetuje statystyki", OnResetCommand);
            AddCommand("dbtest", "Test zapisu do bazy danych", OnDbTestCommand);
            AddCommand("poe_dbtest", "Test zapisu do bazy danych", OnDbTestCommand);
            AddCommand("skills", "Shows available skills", OnSkillsCommand);
            AddCommand("poe_skills", "Shows available skills", OnSkillsCommand);
            AddCommand("learn", "Learn a skill", OnLearnCommand);
            AddCommand("poe_learn", "Learn a skill", OnLearnCommand);
            AddCommand("cast", "Casts a skill", OnCastCommand);
            AddCommand("poe_cast", "Casts a skill", OnCastCommand);

            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
            RegisterEventHandler<EventBombDefused>(OnBombDefused);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
        }

        private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

            var playerData = _dbManager.LoadPlayer(player.SteamID);
            playerData.Name = player.PlayerName;
            _playerService.AddPlayer(playerData);

            if (!_manaRegenTimers.ContainsKey(player.SteamID))
            {
                _manaRegenTimers[player.SteamID] = AddTimer(1.0f, () => RegenerateMana(player), TimerFlags.REPEAT);
            }

            return HookResult.Continue;
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid) return HookResult.Continue;

            var playerData = _playerService.GetPlayer(player.SteamID);
            if (playerData != null)
            {
                _dbManager.SavePlayer(playerData);
                _playerService.RemovePlayer(player.SteamID);
            }

            if (_manaRegenTimers.TryGetValue(player.SteamID, out var timer))
            {
                timer.Kill();
                _manaRegenTimers.Remove(player.SteamID);
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath ev, GameEventInfo info)
        {
            var attacker = ev.Attacker;
            if (attacker != null && attacker.IsValid && attacker != ev.Userid)
            {
                var playerData = _playerService.GetPlayer(attacker.SteamID);
                if (playerData != null)
                {
                    int expGained;
                    string reason;
                    if (ev.Weapon.Contains("knife")) { expGained = ExpKnifeKill; reason = "zabójstwo nożem"; }
                    else if (ev.Headshot) { expGained = ExpHeadshotKill; reason = "zabójstwo strzałem w głowę"; }
                    else { expGained = ExpKill; reason = "zabójstwo"; }
                    _levelingService.AddExp(playerData, expGained, attacker, reason);
                }
            }

            var assister = ev.Assister;
            if (assister != null && assister.IsValid && assister != attacker)
            {
                var playerData = _playerService.GetPlayer(assister.SteamID);
                if (playerData != null)
                {
                    _levelingService.AddExp(playerData, ExpAssist, assister, "asystę");
                }
            }
            return HookResult.Continue;
        }

        #region Simple Event Handlers
        private HookResult OnBombPlanted(EventBombPlanted ev, GameEventInfo info)
        {
            var planter = ev.Userid;
            if (planter != null && planter.IsValid)
            {
                var playerData = _playerService.GetPlayer(planter.SteamID);
                if (playerData != null)
                {
                    _levelingService.AddExp(playerData, ExpBombPlant, planter, "podłożenie bomby");
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnBombDefused(EventBombDefused ev, GameEventInfo info)
        {
            var defuser = ev.Userid;
            if (defuser != null && defuser.IsValid)
            {
                var playerData = _playerService.GetPlayer(defuser.SteamID);
                if (playerData != null)
                {
                    _levelingService.AddExp(playerData, ExpBombDefuse, defuser, "rozbrojenie bomby");
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd ev, GameEventInfo info)
        {
            var winningTeam = (CsTeam)ev.Winner;
            foreach (var p in Utilities.GetPlayers())
            {
                if (p != null && p.IsValid && p.Team == winningTeam)
                {
                    var playerData = _playerService.GetPlayer(p.SteamID);
                    if (playerData != null)
                    {
                        _levelingService.AddExp(playerData, ExpRoundWinPerPlayer, p, "wygraną rundę");
                    }
                }
            }
            return HookResult.Continue;
        }
        #endregion

        #region Commands
        private void OnStatsCommand(CCSPlayerController? caller, CommandInfo info)
        {
            if (caller == null || !caller.IsValid || !caller.PlayerPawn.IsValid) return;
            var playerData = _playerService.GetPlayer(caller.SteamID);
            if (playerData == null) return;

            int requiredExp = 100 + (playerData.Level - 1) * 50;

            caller.PrintToChat($"{Prefix} Twoje statystyki:");
            caller.PrintToChat($" {ChatColors.Grey}Poziom: {playerData.Level} | EXP: {playerData.Exp}/{requiredExp}");
            var pawn = caller.PlayerPawn.Value;
            if (pawn != null) caller.PrintToChat($" {ChatColors.Red}Życie: {pawn.Health}/{pawn.MaxHealth}");
            caller.PrintToChat($" {ChatColors.Blue}Mana: {playerData.Mana}/{playerData.MaxMana}");
            caller.PrintToChat($" {ChatColors.Red}STR: {playerData.Strength}");
            caller.PrintToChat($" {ChatColors.Blue}INT: {playerData.Intelligence}");
            caller.PrintToChat($" {ChatColors.Green}DEX: {playerData.Dexterity}");
            caller.PrintToChat($" {ChatColors.LightYellow}Punkty Statystyk: {playerData.StatPoints}");
            caller.PrintToChat($" {ChatColors.Magenta}Punkty Umiejętności: {playerData.SkillPoints}");

            if (playerData.LearnedSkills.Any())
            {
                caller.PrintToChat($"{Prefix} Twoje Umiejętności:");
                foreach (var skill in playerData.LearnedSkills)
                {
                    caller.PrintToChat($" - {skill.SkillName} (Poziom: {skill.Level})");
                }
            }
        }

        private void OnStrCommand(CCSPlayerController? caller, CommandInfo info) => AllocateStatPoint(caller, info, "str");
        private void OnIntCommand(CCSPlayerController? caller, CommandInfo info) => AllocateStatPoint(caller, info, "int");
        private void OnDexCommand(CCSPlayerController? caller, CommandInfo info) => AllocateStatPoint(caller, info, "dex");

        private void AllocateStatPoint(CCSPlayerController? caller, CommandInfo info, string stat)
        {
            if (caller == null) return;
            var playerData = _playerService.GetPlayer(caller.SteamID);
            if (playerData == null) return;

            int pointsToAdd = 1;
            if (info.ArgCount > 1 && int.TryParse(info.GetArg(1), out int parsedPoints) && parsedPoints > 0)
                pointsToAdd = parsedPoints;

            if (playerData.StatPoints >= pointsToAdd)
            {
                string statName = "";
                string statColor = ChatColors.Default.ToString();
                switch(stat)
                {
                    case "str": playerData.Strength += pointsToAdd; statName = "Siły"; statColor = ChatColors.Red.ToString(); break;
                    case "int": playerData.Intelligence += pointsToAdd; statName = "Inteligencji"; statColor = ChatColors.Blue.ToString(); break;
                    case "dex": playerData.Dexterity += pointsToAdd; statName = "Zręczności"; statColor = ChatColors.Green.ToString(); break;
                }
                playerData.StatPoints -= pointsToAdd;
                caller.PrintToChat($"{Prefix} Dodałeś {pointsToAdd} pkt. do {statColor}{statName}{ChatColors.Default}. Zostało ci {playerData.StatPoints} pkt.");
                _dbManager.SavePlayer(playerData);
            }
            else
            {
                caller.PrintToChat($"{Prefix} Nie masz wystarczającej liczby punktów statystyk.");
            }
        }

        private void OnResetCommand(CCSPlayerController? caller, CommandInfo info)
        {
            if (caller == null) return;
            var playerData = _playerService.GetPlayer(caller.SteamID);
            if (playerData == null) return;

            int refundedPoints = playerData.Strength + playerData.Intelligence + playerData.Dexterity;
            playerData.StatPoints += refundedPoints;
            playerData.Strength = 0;
            playerData.Intelligence = 0;
            playerData.Dexterity = 0;
            caller.PrintToChat($"{Prefix} Twoje statystyki zostały zresetowane.");
            caller.PrintToChat($"{Prefix} Odzyskałeś {refundedPoints} pkt. i masz teraz łącznie {playerData.StatPoints} pkt. do rozdania.");
            _dbManager.SavePlayer(playerData);
        }

        private void OnDbTestCommand(CCSPlayerController? caller, CommandInfo info)
        {
            if (caller == null || !caller.IsValid) return;
            try
            {
                using var connection = new MySqlConnection(_dbManager.GetConnection().ConnectionString);
                connection.Open();
                var command = new MySqlCommand(
                    @"INSERT INTO player_stats (steam_id, level) VALUES (@id, 1) ON DUPLICATE KEY UPDATE level=level+1;", connection);
                command.Parameters.AddWithValue("@id", caller.SteamID);
                command.ExecuteNonQuery();
                caller.PrintToChat($"{Prefix} DB zapis działa!");
            }
            catch(Exception e)
            {
                Server.PrintToConsole($"[PoE2ModRPG] Błąd komendy dbtest: {e.Message}");
                caller.PrintToChat($"{Prefix} {ChatColors.Red}Błąd zapisu do DB! Sprawdź konsolę serwera.");
            }
        }

        private void OnSkillsCommand(CCSPlayerController? caller, CommandInfo info)
        {
            if (caller == null) return;
            caller.PrintToChat($"{Prefix} Dostępne Umiejętności:");
            foreach (var skill in _skillManager.GetAllSkills())
            {
                caller.PrintToChat($" - {skill.Name} (Maks. Poziom: {skill.MaxLevel}): {skill.Description}");
            }
        }

        private void OnLearnCommand(CCSPlayerController? caller, CommandInfo info)
        {
            if (caller == null || info.ArgCount < 2)
            {
                caller?.PrintToChat($"{Prefix} Użycie: /learn <nazwa_umiejętności>");
                return;
            }

            var skillName = info.GetArg(1);
            LearnSkill(caller, skillName);
        }

        private void LearnSkill(CCSPlayerController caller, string skillName)
        {
            var playerData = _playerService.GetPlayer(caller.SteamID);
            if (playerData == null) return;

            var skill = _skillManager.GetSkill(skillName);
            if (skill == null)
            {
                caller.PrintToChat($"{Prefix} Nie znaleziono umiejętności '{skillName}'.");
                return;
            }

            if (playerData.Level < skill.RequiredLevel || playerData.Strength < skill.RequiredStr || playerData.Intelligence < skill.RequiredInt || playerData.Dexterity < skill.RequiredDex)
            {
                caller.PrintToChat($"{Prefix} Nie spełniasz wymagań, by nauczyć się tej umiejętności.");
                return;
            }

            if (playerData.SkillPoints <= 0)
            {
                caller.PrintToChat($"{Prefix} Nie masz punktów umiejętności do wydania.");
                return;
            }

            var learnedSkill = playerData.LearnedSkills.FirstOrDefault(s => s.SkillName.Equals(skill.Name, StringComparison.OrdinalIgnoreCase));
            if (learnedSkill != null)
            {
                if (learnedSkill.Level >= skill.MaxLevel)
                {
                    caller.PrintToChat($"{Prefix} Osiągnąłeś już maksymalny poziom umiejętności {skill.Name}.");
                    return;
                }
                learnedSkill.Level++;
                playerData.SkillPoints--;
                caller.PrintToChat($"{Prefix} Ulepszyłeś {skill.Name} na poziom {learnedSkill.Level}.");
            }
            else
            {
                learnedSkill = new PlayerSkill { PlayerSteamId = playerData.SteamId, SkillName = skill.Name, Level = 1 };
                playerData.LearnedSkills.Add(learnedSkill);
                playerData.SkillPoints--;
                caller.PrintToChat($"{Prefix} Nauczyłeś się {skill.Name}.");
            }

            skill.OnLearn(playerData, learnedSkill.Level);
            _dbManager.SavePlayer(playerData);
        }

        private void OnCastCommand(CCSPlayerController? caller, CommandInfo info)
        {
            if (caller == null || info.ArgCount < 2)
            {
                caller?.PrintToChat($"{Prefix} Użycie: /cast <nazwa_umiejętności>");
                return;
            }

            var skillName = info.GetArg(1);
            var playerData = _playerService.GetPlayer(caller.SteamID);
            if (playerData == null) return;

            var learnedSkill = playerData.LearnedSkills.FirstOrDefault(s => s.SkillName.Equals(skillName, StringComparison.OrdinalIgnoreCase));
            if (learnedSkill == null)
            {
                caller.PrintToChat($"{Prefix} Nie nauczyłeś się tej umiejętności.");
                return;
            }

            var skill = _skillManager.GetSkill(learnedSkill.SkillName);
            if (skill == null || skill.IsPassive)
            {
                caller.PrintToChat($"{Prefix} Nie można rzucić tej umiejętności.");
                return;
            }

            if (_cooldownManager.IsOnCooldown(playerData.SteamId, skill.Name, skill.Cooldown))
            {
                int remaining = _cooldownManager.GetRemainingCooldown(playerData.SteamId, skill.Name, skill.Cooldown);
                caller.PrintToChat($"{Prefix} {skill.Name} jest w trakcie odnowienia. Pozostało: {remaining}s.");
                return;
            }

            int manaCost = skill.GetManaCost(learnedSkill.Level);
            if (playerData.Mana < manaCost)
            {
                caller.PrintToChat($"{Prefix} Masz za mało many, by użyć {skill.Name}.");
                return;
            }

            playerData.Mana -= manaCost;
            _cooldownManager.SetCooldown(playerData.SteamId, skill.Name);
            skill.OnActivate(this, caller, playerData, learnedSkill.Level);
            caller.PrintToChat($"{Prefix} Użyłeś {skill.Name}!");
        }
        #endregion

        #region Other Logic
        private HookResult OnPlayerSpawn(EventPlayerSpawn ev, GameEventInfo info)
        {
            var player = ev.Userid;
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

            var playerData = _playerService.GetPlayer(player.SteamID);
            if (playerData == null) return HookResult.Continue;

            AddTimer(2.0f, () =>
            {
                if (!player.IsValid || !player.PlayerPawn.IsValid) return;
                var pawn = player.PlayerPawn.Value;
                if (pawn == null) return;

                int maxHealth = 100 + (playerData.Strength * 5);
                playerData.MaxMana = 100 + (playerData.Intelligence * 10);

                pawn.MaxHealth = maxHealth;
                pawn.Health = pawn.MaxHealth;
                playerData.Mana = playerData.MaxMana;
            });
            return HookResult.Continue;
        }

        private void RegenerateMana(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            var playerData = _playerService.GetPlayer(player.SteamID);
            if (playerData == null) return;

            if (playerData.Mana < playerData.MaxMana)
            {
                int regenAmount = 2 + (playerData.Intelligence / 5);
                playerData.Mana = Math.Min(playerData.MaxMana, playerData.Mana + regenAmount);
            }
        }
        #endregion

        #region Vampirism Logic
        public void AddActiveVampirismEffect(Services.Skills.ActiveVampirismEffect effect) => _activeVampirismEffects.Add(effect);
        public void RemoveActiveVampirismEffect(Services.Skills.ActiveVampirismEffect effect) => _activeVampirismEffects.Remove(effect);

        private HookResult OnPlayerHurt(EventPlayerHurt ev, GameEventInfo info)
        {
            var attacker = ev.Attacker;
            if (attacker == null || !attacker.IsValid || attacker == ev.Userid) return HookResult.Continue;

            var activeEffect = _activeVampirismEffects.FirstOrDefault(e => e.PlayerSteamId == attacker.SteamID);
            if (activeEffect != null)
            {
                var pawn = attacker.PlayerPawn.Value;
                if (pawn != null)
                {
                    int healAmount = (int)(ev.DmgHealth * activeEffect.LifestealPercent);
                    pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + healAmount);
                }
            }
            return HookResult.Continue;
        }
        #endregion

        #region API Implementation
        public Player? GetPlayer(ulong steamId) => _playerService.GetPlayer(steamId);
        public int GetPlayerLevel(ulong steamId) => GetPlayer(steamId)?.Level ?? -1;
        public void AddExp(ulong steamId, int amount, string reason)
        {
            var player = GetPlayer(steamId);
            if (player == null) return;
            var controller = Utilities.GetPlayerFromSteamId(steamId);
            _levelingService.AddExp(player, amount, controller, reason);
        }
        #endregion
    }
}
