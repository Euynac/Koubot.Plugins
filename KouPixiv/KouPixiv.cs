using System;
using System.Text.Json.Nodes;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.System;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Random;
using Koubot.Tool.Web;

namespace KouFunctionPlugin.Pixiv
{
    [KouPluginClass("pixiv", "Pixiv助手",
        Introduction = "Pixiv相关插件（施工中）",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouPixiv : KouPlugin<KouPixiv>
    {
        [KouPluginFunction(ActivateKeyword = "count", Name = "当前本地Pixiv作品信息数量")]
        public object CurLocalWorkInfoCount()
        {
            var count = PixivWork.Count();
            if (count == 0) return "当前没有Pixiv的作品信息";
            return $"现在一共有{count}个作品信息";
        }

        [KouPluginFunction(ActivateKeyword = "好图", Name = "获取好图做壁纸", NeedCoin = 10)]
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
    }
}