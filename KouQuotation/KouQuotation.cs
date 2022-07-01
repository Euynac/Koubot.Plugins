using System.Text;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Event;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Random;
using Koubot.Tool.String;

namespace KouFunctionPlugin;

[KouPluginClass("quotation", "语录",
    Introduction = "",
    Author = "7zou",
    PluginType = PluginType.Function,
    Authority = Authority.BotManager)]
public class KouQuotation : KouPlugin<KouQuotation>, IWantPluginGlobalConfig<QuotationConfig>
{

    public static QuotationConfig Config { get; set; }


    static KouQuotation()
    {
        Config = GetSingleton().GlobalConfig();
    }

    [KouPluginFunction(Name = "随机获取一个语录")]
    public object? Default([KouPluginArgument(Name = "指定类型的语录")]Quotation.QuotationType? type = null)
    {
        return Quotation.RandomGetOne(p =>
                type != null && p.Type == type || type == null)
            ?.GetParsedContent() ?? $"没有找到{(type != null ? $"{type.GetKouEnumName()}类型的" : "语录")}呢";
    }
    [KouPluginEventHandler]
    public override KouEventHandlerResult? OnReceiveGroupMessage(GroupMessageEventArgs e)
    {
        if (Config.IsEnable && Config.TriggerRate.ProbablyTrue() && e.FromUser != null &&  e.FromUser.PlatformUserId == "2734283478")
        {
            return Quotation.RandomGetOne(p =>
                    Config.QuotationType.HasFlag(p.Type))?
                .GetParsedContent() is { } s
                ? $"{e.FromUser.ToKouResourceString()} {s}"
                : null;
        }

        return null;
    }

    [KouPluginFunction(ActivateKeyword = "夸夸概率", Name = "设置夸夸概率")]
    public object SetPraiseProbability([KouPluginArgument(Name = "夸夸概率", Max = 1, Min = 0)] double p)
    {
        var config = this.GlobalConfig();
        var previous = config.TriggerRate;
        config.TriggerRate = p;
        config.SaveChanges();
        Config = config;
        return $"夸夸概率：{previous:P} => {config.TriggerRate:P}";
    }

    [KouPluginFunction(ActivateKeyword = "夸夸开关", Name = "开关夸夸功能")]
    public object PraiseSwitch()
    {
        var config = this.GlobalConfig();
        config.IsEnable = !config.IsEnable;
        config.SaveChanges();
        Config = config;
        return $"夸夸开关：{config.IsEnable}";
    }

    [KouPluginFunction(Name = "遗忘", ActivateKeyword = "del|delete")]
    public string DeleteQuotation([KouPluginArgument(Name = "语录ID")] List<int> id)
    {
        var result = new StringBuilder();
        foreach (var i in id)
        {
            var reply = Quotation.SingleOrDefault(a => a.ID == i);
            if (reply == null) result.Append($"\n不记得ID{i}");
            else if (reply.Contributor is not null && reply.Contributor != CurKouUser &&
                     !CurUser.HasTheAuthority(Authority.BotManager))
                result.Append($"\nID{i}是别人贡献的，不可以删噢");
            else
            {
                result.Append($"\n忘记了{reply.ToString(FormatType.Brief)}");
                reply.DeleteThis();
            };
        }

        return result.ToString().TrimStart();
    }

    [KouPluginFunction(Name = "加语录", ActivateKeyword = "add", EnableAutoNext = true)]
    public string AddQuotation([KouPluginArgument(Name = "语录类型")] Quotation.QuotationType type,
        [KouPluginArgument(Name = "语录内容")] string content)
    {
        if (Context.Set<Quotation>().Any(p => p.Content == content && p.Type == type))
        {
            return "已经存在相同的语录了哦";
        }
        var success = Quotation.Add(quotation =>
        {
            quotation.Content = content;
            quotation.Type = type;
            quotation.Contributor = CurKouUser.FindThis(Context);
        }, out var added, out var error, Context);
        if (success)
        {
            var reward = RandomTool.GenerateRandomInt(8, 15);
            CurUser.KouUser.GainCoinFree(reward);
            return $"学会了说{type.GetKouEnumName()}：{added.ToString(FormatType.Brief)}\n" +
                   $"[您获得了{CurKouGlobalConfig.CoinFormat(reward)}!]";
        }
        return $"没学会，就突然：{error}";
    }
}