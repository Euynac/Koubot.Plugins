using System.Text.Json.Serialization;
using Koubot.Shared.Models;

namespace KouFunctionPlugin;

public class WaterReminder : PluginUserConfig<WaterReminder>
{
    /// <summary>
    /// 当天已喝水次数
    /// </summary>
    public int TodayHaveTimes { get; set; }
    /// <summary>
    /// 一天喝几次
    /// </summary>
    public int Times { get; set; }
    /// <summary>
    /// 剩余喝水次数
    /// </summary>
    [JsonIgnore]
    public int RemainTimes => Times - TodayHaveTimes;
    [JsonIgnore]
    public int TodayHasRemindTimes { get; set; }
    /// <summary>
    /// 每次之间的间隔
    /// </summary>
    public TimeSpan? Duration { get; set; }
    /// <summary>
    /// 绑定群提醒
    /// </summary>
    public int? RemindGroupId { get; set; }

    /// <summary>
    /// 绑定群提醒
    /// </summary>
    [JsonIgnore]
    public PlatformGroup? RemindGroup => PlatformGroup.SingleOrDefault(p => p.Id == RemindGroupId);
    /// <summary>
    /// 是私人提醒
    /// </summary>
    public bool PrivateRemind { get; set; }
    /// <summary>
    /// 喝水记录（当天）
    /// </summary>
    public List<DateTime> Record { get; set; } = new();
    /// <summary>
    /// 下一次刷新日期
    /// </summary>
    public DateTime? NextRefreshDate { get; set; }
    /// <summary>
    /// 开关
    /// </summary>
    public bool IsEnabled { get; set; }
}