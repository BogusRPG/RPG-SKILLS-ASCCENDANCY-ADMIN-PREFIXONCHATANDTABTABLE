using CounterStrikeSharp.API.Core;

namespace PoE2ModRPG.Configs
{
    public class PluginConfig : BasePluginConfig
    {
        public DBConfig Database { get; set; } = new();
    }
}
