using System.Text;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.System;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;

namespace KouFunctionPlugin.Cook;

[KouPluginClass("food", "美食",
    Introduction = "吃货用",
    Author = "7zou",
    PluginType = PluginType.Function)]
public class KouFood : KouPlugin<KouFood>
{
    [KouPluginFunction(Name = "随机获取一个美食")]
    public override object? Default([KouPluginArgument(Name = "指定美食")]string? str = null)
    {
        return Food.RandomGetOne(p =>
                (str != null && p.Name.Contains(str, StringComparison.OrdinalIgnoreCase) || str == null))
            ?.ToString(FormatType.Detail)?.Be("建议吃：\n{0}",true) ?? $"没有找到{str ?? "食物"}呢";
    }
    [KouPluginFunction(Name = "遗忘", ActivateKeyword = "del|delete")]
    public string DeleteFood([KouPluginArgument(Name = "美食ID")] List<int> id)
    {
        var result = new StringBuilder();
        foreach (var i in id)
        {
            var reply = Food.SingleOrDefault(a => a.ID == i);
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

    [KouPluginFunction(Name = "加美食", ActivateKeyword = "add", EnableAutoNext = true)]
    public string AddFood(
        [KouPluginArgument(Name = "美食名字")] string foodName,
        [KouPluginArgument(Name = "美食图片")] KouImage foodImage)
    {
        if (foodName.IsNullOrWhiteSpace()) return "美食叫什么名字嘛";
        var img = Food.SaveFoodImage(foodName, foodImage);
        if (img == null) return "保存美食出错了";
        var success = Food.Add(food =>
        {
            food.Name = foodName;
            food.Contributor = CurKouUser.FindThis(Context);
            food.ImageUrl = img.GetFileName(true)!;
        }, out var added, out var error, Context);
        if (success)
        {
            var reward = RandomTool.GenerateRandomInt(8, 15);
            CurUser.KouUser.GainCoinFree(reward);
            return $"美食图鉴+1\n{added.ToString(FormatType.Customize1)}\n" +
                   $"[您获得了{CurKouGlobalConfig.CoinFormat(reward)}!]";
        }
        return $"没学会，就突然：{error}";
    }
}