using System.Text;
using Koubot.SDK.API;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;

namespace KouFunctionPlugin;

[KouPluginClass("water", "喝水提醒小助手",
    PluginType = PluginType.Function)]
public class KouMedicineReminder : KouPlugin<KouMedicineReminder>, IWantPluginUserConfig<WaterReminder>
{
    [KouPluginParameter(ActivateKeyword = "group", Name = "绑定该群提醒")]
    public bool? BindGroup { get; set; }

    [KouPluginParameter(ActivateKeyword = "private", Name = "私人提醒")]
    public bool? Private { get; set; }

    [KouPluginParameter(ActivateKeyword = "times", Name = "一天次数")]
    public int? Times { get; set; }

    [KouPluginParameter(ActivateKeyword = "duration", Name = "每次间隔")]
    public TimeSpan? Duration { get; set; }

    [KouPluginParameter(ActivateKeyword = "enable", Name = "小助手开关")]
    public bool? Enable { get; set; }

    [KouPluginFunction(Name = "当前喝水状态")]
    public override object? Default(string? str = null)
    {
        var config = this.UserConfig();
        RefreshConfig(config);
        if (config.TodayHaveTimes > 0)
        {
            return $"今天已经喝了{config.TodayHaveTimes}次水，是在今天{config.Record.Select(p=>p.ToString("T")).ToStringJoin("、")}时喝的{(config.RemainTimes > 0 ? $"，还有{config.RemainTimes}次水没喝哦":"今天已经完水啦" )}";
        }

        return $"今天还没有喝水哦{(config.RemainTimes > 0 ? $"，还有{config.RemainTimes}次水没喝呢":null)}";
    }

    [KouPluginFunction(ActivateKeyword = "have", Name = "现在喝一次水")]
    public object? HaveMedicine()
    {
        var reminder = this.UserConfig();
        RefreshConfig(reminder);
        if (reminder.TodayHaveTimes >= reminder.Times)
        {
            return $"今天已经喝够水了哦！上次是在今天{reminder.Record.Select(p=>p.ToString("HH:mm:ss")).LastOrDefault()}喝的";
        }
        reminder.Record.Add(DateTime.Now);
        reminder.TodayHaveTimes++;
        if (!reminder.SaveChanges()) return "不知道为什么记录失败了呢";
        var reply = $"真棒！Kou记下来你现在喝了一次水了哦！{(reminder.RemainTimes > 0 ? $"还有{reminder.RemainTimes}次水没喝呢" : "今天已经完水啦")}";
        Reply(reply);
        Thread.Sleep(1000);
        Reply(Meme.PickOne(new[]
            {
                Meme.MemeScene.Praise, Meme.MemeScene.Laborious, Meme.MemeScene.FeelHappy,
                Meme.MemeScene.Love, Meme.MemeScene.Like,
                Meme.MemeScene.Ok, Meme.MemeScene.BeCute, Meme.MemeScene.ComeOn,
                Meme.MemeScene.Kiss, Meme.MemeScene.Hug
            },
            new[]
            {
                Meme.MemeLabel.BugcatCapoo, Meme.MemeLabel.Cute, Meme.MemeLabel.SweetieBunny,
                Meme.MemeLabel.WhiteKittenGirl
            })?.GetImage());
    

        return null;
    }

    private static void RefreshConfig(WaterReminder reminder)
    {
        if (reminder.NextRefreshDate == null || reminder.NextRefreshDate <= DateTime.Now.Date)
        {
            if (reminder.NextRefreshDate <= DateTime.Now.Date)
            {
                reminder.TodayHaveTimes = 0;
                reminder.Record.Clear();
            }
            reminder.NextRefreshDate = DateTime.Now.AddDays(1).Date;
            reminder.SaveChanges();
        }
    }
    [KouPluginFunction(ActivateKeyword = "config", Name = "调整喝水提醒小助手", SupportedParameters =
        new []{nameof(Times), nameof(Duration), nameof(Private), nameof(BindGroup), nameof(Enable)})]
    public object? CreateReminder()
    {
        var config = this.UserConfig();
        RefreshConfig(config);
        var sb = new StringBuilder();
        sb.Append("小助手明白了：");
        if (BindGroup != null)
        {
            if (CurGroup == null && BindGroup.Value)
            {
                sb.Append("绑定群提醒需要在群内使用");
            }
            else
            {
                sb.Append($"\n绑定群提醒开关：{config.RemindGroup != null}=>{BindGroup}");
                config.RemindGroupId = BindGroup.Value ? CurGroup?.Id : null;
            }
        }

        if (Private != null)//暂不支持私人提醒
        {
            sb.Append($"\n私人提醒开关：{config.PrivateRemind}=>{Private}");
            config.PrivateRemind = Private.Value;
        }

        if (Times != null)
        {
            sb.Append($"\n一天次数：{config.Times}=>{Times}");
            config.Times = Times.Value;
        }

        if (Duration != null)
        {
            sb.Append($"\n间隔小时：{config.Duration?.TotalHours:0.#}=>{Duration.Value.TotalHours:0.#}");
            config.Duration = Duration.Value;
        }

        if (Enable != null)
        {
            sb.Append($"\n小助手开关：{config.IsEnabled}=>{Enable.Value}");
            config.IsEnabled = Enable.Value;
        }

        if (!config.SaveChanges())
        {
            return "不知道为什么记录失败了";
        }
        return sb.ToString();
    }

    

    static KouMedicineReminder()
    {
        AddEveryDayCronTab(() =>
        {
            var reminders = GetSingleton().UserConfigs().Where(p=>p.IsEnabled && (p.PrivateRemind || p.RemindGroup != null));
            var nextDayDateTime = DateTime.Now.NextDay();
            foreach (var item in reminders)
            {
                RefreshConfig(item);
                KouTaskDelayer.AddTask(DateTime.Now.Date.AddHours(12), 
                    () => RemainAction(item, nextDayDateTime));
            }
        });
    }

    private static void RemainAction(WaterReminder item, DateTime validUntil)
    {
        if(!item.IsEnabled)return;
        if(DateTime.Now >= validUntil) return;
        if(item.RemainTimes <= 0) return;
        item.RemindGroup?.SendGroupMessage($"{item.User.Nickname}今天还有{item.RemainTimes}次水没有喝哦！");
        Thread.Sleep(1000);
        item.RemindGroup?.SendGroupMessage(Meme
            .PickOne(new[]
            {
                Meme.MemeScene.GetAngry, Meme.MemeScene.FeelSad, Meme.MemeScene.Gaze, Meme.MemeScene.Doubt, Meme.MemeScene.FeelCold,
                Meme.MemeScene.No, Meme.MemeScene.Weep, 
            }, new[]
            {
                Meme.MemeLabel.SweetieBunny, Meme.MemeLabel.SAZI
            })?.GetImage());
        
        item.TodayHasRemindTimes++;
        item.SaveChanges();
        if (item.TodayHasRemindTimes < 3)
        {
            KouTaskDelayer.AddTask(DateTime.Now.AddHours(1), () => RemainAction(item, validUntil));
        }
    }
}