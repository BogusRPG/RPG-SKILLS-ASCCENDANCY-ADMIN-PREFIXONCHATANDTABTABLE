using System.Collections.Generic;
using PoE2ModRPG.Models;

namespace PoE2ModRPG.Services
{
    public class PlayerService
    {
        private readonly Dictionary<ulong, Player> _players = new();

        public void AddPlayer(Player player)
        {
            _players[player.SteamId] = player;
        }

        public void RemovePlayer(ulong steamId)
        {
            _players.Remove(steamId);
        }

        public Player? GetPlayer(ulong steamId)
        {
            return _players.TryGetValue(steamId, out var player) ? player : null;
        }

        public IEnumerable<Player> GetAllPlayers()
        {
            return _players.Values;
        }
    }
}
