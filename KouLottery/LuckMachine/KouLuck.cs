using Koubot.SDK.PluginInterface;
using Koubot.Shared.Interface;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin.LuckMachine
{
    [KouPluginClass("luck", "运势",
        Author = "7zou",
        Authority = Authority.NormalUser,
        Introduction = "运势相关功能",
        PluginType = PluginType.Function)]
    public class KouLuck : KouPlugin<KouLuck>
    {
        private static List<string> DirectionList { get; } =
            new() { "东", "南", "西", "北", "东南", "东北", "西南", "西北" };

        private static List<string> MusicGameList { get; } =
            new()
            {
                "Cytus",
                "Hachi Hachi",
                "P:h Diver",
                "osu!",
                "Tone Sphere",
                "Groove Coaster",
                "Deemo",
                "Malody",
                "Dynamix",
                "同步音律喵赛克",
                "VOEZ",
                "缪斯计划",
                "Lanota",
                "Arcaea",
                "Pianista",
                "maimai",
                "Cytus II",
                "Muse Dash",
                "O2Jam",
                "Phigros",
                "polytone",
                "阳春白雪",
                "Beatmania IIDX",
                "LoveLive!",
                "The IdolM@ster Cinderella Girls Starlight Stage",
                "BanG Dream!",
                "D4DJ",
                "Project Sekai Colorful Stage!",
                "Groove Coaster",
                "DJMAX",
                "Taiko no Tatsujin",
                "Beat Saber",
                "WACCA",
                "maimai",
                "CHUNITHM",
                "SOUND VOLTEX",
                "Jubeat",
                "DANCERUSH",
                "O.N.G.E.K.I.",
                "WAVEAT",
                "Cytoid",
                "Dynamite",
                "東方鍵盤遊戲",
                "节奏大师 Plus(?",
                "O2Jam u",
                "Sonolus",
                "BanGround",
                "钢琴块",
                "初音ミク -Project DIVA-",
                "GITADORA",
                "音灵 INVAXION",
                "OverRapid",
                "初音未来：梦幻歌姬",
                "舞立方",
                "舞萌DX",
                "maimai でらっくす Splash",
                "乐动时代"
            };


        private string GetTodayLuck(bool remake = false)
        {
            var luckValue = CurUser.KouUser.LuckValue(remake);
            var result = $"{CurUser.Name}的今日人品：{luckValue}";
            var hashString = $"{luckValue}{CurUser.PlatformUserId}";
            var list = Almanac.GetCache();
            if (list.Count == 0) return result;
            if (luckValue >= 99)
            {
                result += "\n今天诸事皆宜！";
            }
            else if (luckValue <= 1)
            {
                result += "\n今天似乎运气不太好呢...";
            }
            else
            {
                var luckThing = luckValue < 10 ? null : list.Where(p => p.IsOminous == false).ToList().RandomGetOne(hashString);
                var ominousThing = luckValue > 90 ? null : list.Where(p => p.IsOminous && p.Title != luckThing?.Title).ToList().RandomGetOne(hashString);
                result += (luckThing?.Be($"\n{luckThing.ToString(FormatType.Customize1)}") ?? "\n今天没有宜事项") +
                          (ominousThing?.Be($"\n{ominousThing.ToString(FormatType.Customize1)}") ?? "\n今天没有忌事项");
            }
            result += $"\n今日音游：{MusicGameList.RandomGetOne(hashString)}";
            result += $"\n建议机位：P{RandomTool.GenerateRandomInt(1, 2, hashString)}";
            result += $"\n建议朝向：{DirectionList.RandomGetOne(hashString)}";

            return result;
        }

        [KouPluginFunction(Help = "重新获取今日运势（限免）", ActivateKeyword = "remake")]
        public string Remake()
        {
            return GetTodayLuck(true);
        }


        [KouPluginFunction(Help = "查看今日运势", Name = "获取今日运势")]
        public override object? Default(string? str = null)
        {
            return GetTodayLuck();
        }
        [KouPluginFunction(Name = "遗忘", ActivateKeyword = "del|delete", Help = "删除学习过的黄历")]
        public string DeleteItem([KouPluginArgument(Name = "黄历ID")] List<int> id)
        {
            var result = new StringBuilder();
            foreach (var i in id)
            {
                var almanac = Almanac.SingleOrDefault(a => a.ID == i);
                if (almanac == null) result.Append($"\n不记得ID{i}");
                else if (almanac.SourceUser != null && almanac.SourceUser != CurUser &&
                         !CurUser.HasTheAuthority(Authority.BotManager))
                    result.Append($"\nID{i}是别人贡献的，不可以删噢");
                else
                {
                    result.Append($"\n忘记了{almanac.ToString(FormatType.Brief)}");
                    almanac.DeleteThis();
                };
            }

            return result.ToString().TrimStart();
        }

        [KouPluginFunction(Help = "教Kou有哪些忌或宜的黄历", Name = "教教", ActivateKeyword = "add")]
        public string AddItem(
            [KouPluginArgument(Name = "事项名(使用忌或宜开头)")]
            string item,
            [KouPluginArgument(Name = "事项内容")]
            string itemIntro)
        {
            if (item.IsNullOrWhiteSpace() || itemIntro.IsNullOrWhiteSpace() ||
                item.Length <= 1) return "好好教我嘛";
            bool isOminous = item.StartsWith("忌");
            if (isOminous || item.StartsWith("宜"))
            {
                item = item.Substring(1);
            }

            var success = Almanac.Add(almanac =>
            {
                almanac.Content = itemIntro;
                almanac.Title = item;
                almanac.IsOminous = isOminous;
                almanac.SourceUser = CurUser.FindThis(Context);
            }, out var added, out var error, Context);
            if (success)
            {
                var reward = RandomTool.GenerateRandomInt(8, 15);
                CurUser.KouUser.GainCoinFree(reward);
                return $"学会了，ID{added.ToString(FormatType.Brief)}\n您获得了{CurKouGlobalConfig.CoinFormat(reward)}!";
            }
            return $"没学会，就突然：{error}";
        }
    }
}