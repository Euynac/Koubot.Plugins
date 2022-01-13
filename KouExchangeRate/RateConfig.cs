using Koubot.Shared.Models;

namespace KouExchangeRate;

public class RateConfig : PluginGlobalConfig
{
    public DateTime LastSuccessUpdateTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public bool LastUpdateSuccess { get; set; }
}