using Koubot.SDK.API;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.System;
using Koubot.Tool.Extensions;
using System;
using System.Linq;
using System.Threading;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

// ReSharper disable once CheckNamespace
namespace KouFunctionPlugin.Pixiv
{
    [KouPluginClass("setu", "涩图",
        Introduction = "随机涩图",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouSetu : KouPlugin<KouSetu>
    {
        private const int WorkFee = 8;

        [KouPluginFunction(Name = "随机一张涩图", NeedCoin = WorkFee, OnlyUsefulInGroup = true)]
        public override object? Default([KouPluginArgument(Name = "涩图要求")] string? str = null)
        {
            if (CDOfFunctionGroupIsIn(new TimeSpan(0, 0, 10), out var remain))
                return $"大触们还在休息中（剩余{remain.TotalSeconds:0.#}秒）";
            PixivWork? img = str == null
                ? PixivWork.RandomGetOne(p => !p.R18)
                : PixivWork.RandomGetOne(p => !p.R18 && (p.Tags.Any(t => t.Name.Contains(str, StringComparison.OrdinalIgnoreCase)) || p.Title.Contains(str, StringComparison.OrdinalIgnoreCase)));
            if (img == null)
            {
                CDOfGroupFunctionReset();
                return $"Kou找遍了{PixivAuthor.Count()}位大触都画不出你要求的作品";
            }
            if (!CurKouUser.ConsumeCoinFree(WorkFee)) return $"需要{CurKouGlobalConfig.CoinFormat(WorkFee)}来请人画涩图噢";
            CurGroup!.SendGroupMessage(
                $"{CurUser.Name}花费了{CurKouGlobalConfig.CoinFormat(WorkFee)}" +
                $"请来了\"{img.Author.Name}\"画了一张「{img.Title}」(pid{img.Pid})" +
                $"{img.Tags?.ToStringJoin("、")?.BeIfNotEmpty("，据说有如下要素：\n{0}", true)}");
            Thread.Sleep(2000);
            return new KouImage(img.GetUrl());
        }

        // static KouSetu()
        // {
        //     Task.Factory.StartNew(() =>
        //     {
        //         while (true)
        //         {
        //             FetchWorkInfos();
        //             Thread.Sleep(10000);
        //         }
        //     });
        // }
        //
        // private static void FetchWorkInfos()
        // {
        //     SetuAPI api = new();
        //     var response = api.Call(100);
        //     if (response == null) return;
        //     SaveToDatabase(response);
        // }
        // private static readonly object _saveLock = new(); 
        // private static void SaveToDatabase(ResponseDto.Root root)
        // {
        //     if(root.Data.IsNullOrEmptySet()) return;
        //     lock (_saveLock)
        //     {
        //         using var context = new KouContext();
        //         foreach (var item in root.Data)
        //         {
        //             if (context.Set<PixivWork>().Any(p=>p.Pid == item.Pid && p.P == item.P)) continue;
        //             PixivWork.Add(item.ToModel(context), out _, context);
        //         }
        //     }
        // }
    }
}