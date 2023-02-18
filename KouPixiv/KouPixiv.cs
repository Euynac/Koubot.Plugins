using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using Koubot.SDK.API;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.System;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using Koubot.Tool.Web;

namespace KouFunctionPlugin.Pixiv
{
    [PluginClass("pixiv", "Pixiv助手",
        Introduction = "Pixiv相关插件（施工中）",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouPixiv : KouPlugin<KouPixiv>
    {
        [PluginFunction(ActivateKeyword = "count", Name = "当前本地Pixiv作品信息数量")]
        public object CurLocalWorkInfoCount()
        {
            var count = PixivWork.Count();
            if (count == 0) return "当前没有Pixiv的作品信息";
            return $"现在一共有{count}个作品信息";
        }

        [PluginFunction(ActivateKeyword = "好图", Name = "获取好图做壁纸", NeedCoin = 10)]
        public object GetBeautyImage()
        {
            if (!CurKouUser.HasEnoughFreeCoin(10))
            {
                return FormatNotEnoughCoin(10);
            }
            if (CurGroup != null)
            {
                if (CDOfFunctionGroupIsIn(new TimeSpan(0, 0, 0, 5), out var duration))
                {
                    return FormatIsInCD(duration);
                }
            }
            else
            {
                if (CDOfFunctionKouUserIsIn(new TimeSpan(0, 0, 0, 5), out var duration))
                {
                    return FormatIsInCD(duration);
                }
            }
       
            var failed = "获取失败了呢";
            var url = "https://iw233.cn/API/MirlKoi.php?type=json".ProbablyBe(
                "https://iw233.cn/API/Random.php?type=json", 0.5);
            var response = KouHttp.Create(url).SetQPS(1).SendRequest(HttpMethods.GET);
            if (response == null) return failed;
            var img = JsonNode.Parse(response.Body)?["pic"]?.GetValue<string>();
            if (img == null) return failed;
            CurKouUser.ConsumeCoinFree(10);
            return new KouImage(img);
        }


        private const int WorkFee = 15;

        [PluginFunction(Name = "随机一张涩图", NeedCoin = WorkFee, OnlyUsefulInGroup = true)]
        public object? Setu([PluginArgument(Name = "涩图要求")] string? str = null)
        {
            if (CDOfFunctionGroupIsIn(new TimeSpan(0, 0, 10), out var remain))
                return $"大触们还在休息中（剩余{remain.TotalSeconds:0.#}秒）";
            var img = str == null
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
                $"{img.Tags?.StringJoin("、")?.BeIfNotEmpty("，据说有如下要素：\n{0}", true)}");
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