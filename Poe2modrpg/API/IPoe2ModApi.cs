using PoE2ModRPG.Models;

namespace PoE2ModRPG.API
{
    public interface IPoe2ModApi
    {
        /// <summary>
        /// Gets the current level of a player.
        /// Returns -1 if the player is not found.
        /// </summary>
        int GetPlayerLevel(ulong steamId);

        /// <summary>
        /// Adds experience points to a player and handles level ups.
        /// </summary>
        void AddExp(ulong steamId, int amount, string reason);

        /// <summary>
        /// Retrieves the full Player object for a given player.
        /// Returns null if the player is not found.
        /// </summary>
        Player? GetPlayer(ulong steamId);
    }
}