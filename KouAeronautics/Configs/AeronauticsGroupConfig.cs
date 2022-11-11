using Koubot.Shared.Models;
using Koubot.Tool.Extensions;

namespace KouFunctionPlugin.Configs;

public class AeronauticsGroupConfig : PluginGroupConfig<AeronauticsGroupConfig>
{
    public List<string> VpnList { get; set; } = new();
    public Dictionary<string, UserAccount> UseStatus { get; set; } = new();

    public string GetAllSourceStatus()
    {
        var status = VpnList
            .Select(p => $"{p} —— {(UseStatus.TryGetValue(p, out var user) ? $"{user.Nickname}占用中" : "未使用")}")
            .StringJoin('\n');
        return $"当前使用状况：\n{status}";
    }
}