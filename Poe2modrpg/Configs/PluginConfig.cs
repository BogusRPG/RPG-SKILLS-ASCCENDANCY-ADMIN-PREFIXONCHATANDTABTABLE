using CounterStrikeSharp.API.Core;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PoE2ModRPG.Configs
{
    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("Database")]
        public DBConfig Database { get; set; } = new();

        [JsonPropertyName("Admins")]
        public List<ulong> Admins { get; set; } = new();
    }
}