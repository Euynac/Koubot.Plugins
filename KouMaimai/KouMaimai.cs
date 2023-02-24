using Koubot.SDK.PluginInterface;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using Koubot.Tool.String;
using KouGamePlugin.Maimai.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.PluginExtension;
using Koubot.SDK.PluginExtension.Result;
using Koubot.SDK.Services;
using Koubot.SDK.System;
using Koubot.SDK.System.Messages;
using Koubot.SDK.Templates;
using Koubot.Shared.Protocol;
using Koubot.Tool.General;
using KouMaimai;
using KouMaimai.Room;

namespace KouGamePlugin.Maimai
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    [PluginClass("mai", "maimai",
        PluginType = PluginType.Game,
        CanUseProxy = true)]
    public partial class KouMaimai : KouPlugin<KouMaimai>,
        IWantPluginUserConfig<MaiUserConfig>, IWantPluginGroupConfig<MaiGroupConfig>,
        IWantPluginGlobalConfig<MaiGlobalConfig>
    {
        [PluginFunction]
        public override object? Default(string? str = null)
        {
            return GetSongByAlias(str);
        }

        private object? FormatRecords(IEnumerable<SongRecord> records, string sketch)
        {
            Action<ResultAutoPage.Setting>? action = null;
            if (this.UserConfig().UseHtml is true)
            {
                action = s =>
                {
                    s.PageSketch = sketch;
                    s.OnePageMaxItemCount = 20;
                    s.UseSkin = true;
                };
            }

            return records.ToAutoPageSetString(sketch, action);
        }

        #region WahLap

        [PluginFunction(ActivateKeyword = "map status", Name = "DX地图状态")]
        public object? DxMapStatus()
        {
            var count = ArcadeMap.Count();
            return $"店铺总数：{count}\n" +
                   $"上次更新时间：{this.GlobalConfig().DxMapLastUpdateTime}";
        }
        [PluginFunction(Name = "更新机台地址", Authority = Authority.BotManager)]
        public object? UpdateDxMap()
        {
            var locations = WahLapApi.GetDxLocation();
            if (locations.IsNullOrEmptySet()) return "更新失败，未能获取到店铺信息";
            using var context = new KouContext();
            var oriDict = context.Set<ArcadeMap>().Select(p => new MutableTuple<bool, ArcadeMap>(false, p))
                .ToDictionary(p => p.Item2.LocationId);
            var addedCount = 0;
            foreach (var location in locations)
            {
                if (oriDict.TryGetValue(int.Parse(location.id), out var pair))
                {
                    pair.Item1 = true;
                    pair.Item2.MachineCount = location.machineCount;
                    pair.Item2.Province = location.province;
                    pair.Item2.ArcadeName = location.arcadeName;
                    pair.Item2.MallName = location.mall;
                    pair.Item2.Address = location.address;
                    context.Update(pair.Item2);
                }
                else
                {
                    addedCount++;
                    context.Set<ArcadeMap>().Add(new ArcadeMap()
                    {
                        LocationId = int.Parse(location.id),
                        MachineCount = location.machineCount,
                        Province = location.province,
                        ArcadeName = location.arcadeName,
                        MallName = location.mall,
                        Address = location.address,
                    });
                }
            }

            var closedCount = 0;
            foreach (var closedLocation in oriDict.Where(p => p.Value.Item1 == false))
            {
                closedCount++;
                closedLocation.Value.Item2.IsClosed = true;
                context.Update(closedLocation.Value.Item2);
            }
            var effected = context.SaveChanges();
            if (effected <= 0) return $"新增店铺{addedCount}，关停店铺{closedCount}，但更新失败，影响0条记录";
            var config = this.GlobalConfig();
            config.DxMapLastUpdateTime = DateTime.Now;
            config.SaveChanges();
            return $"新增店铺{addedCount}，关停店铺{closedCount}，影响了{effected}条记录。";
        }

        #endregion

        #region Diving-Fish

        private bool HasBindError(MaiUserConfig config, out string reply)
        {
            if (config.Username.IsNullOrEmpty())
            {
                reply = $"{CurUser.Name}暂未绑定Diving-Fish账号呢，私聊Kou使用/mai bind 用户名 密码绑定";
                return true;
            }

            if ((DateTime.Now - config.TokenRefreshTime).Days >= 29)//29天刷新一次Token
            {
                var api = new DivingFishApi(config);
                if (!api.Login())
                {
                    reply = $"刷新Token时，{CurUser.Name}登录失败了呢，Diving-Fish说：{api.ErrorMsg}";
                    return true;
                }

                config.LoginTokenValue = api.TokenValue;
                config.TokenRefreshTime = DateTime.Now;
                config.SaveChanges();
            }

            reply = "";
            return false;
        }
        //private static readonly KouColdDown<UserAccount> _getRecordsCd = new();

        [PluginFunction(ActivateKeyword = "刷新", Name = "重新获取所有成绩", NeedCoin = 10)]
        public object RefreshRecords()
        {
            var config = this.UserConfig();
            if (HasBindError(config, out var reply)) return reply;
            var api = new DivingFishApi(config);
            if (!CurKouUser.HasEnoughFreeCoin(10))
            {
                return FormatNotEnoughCoin(10);
            }
            if (CDOfFunctionKouUserIsIn(_refreshCD, new TimeSpan(0, 1, 0), out var remaining))
            {
                return FormatIsInCD(remaining);
            }
            Reply($"正在刷新中...请稍后\n[{FormatConsumeFreeCoin(10)}]");

            if (!api.FetchUserRecords(CurKouUser))
            {
                CDOfUserFunctionReset(_refreshCD);
                return $"获取成绩失败：{api.ErrorMsg}";
            }

            CurKouUser.ConsumeCoinFree(10);
            config.GetRecordsTime = DateTime.Now;
            config.SaveChanges();
            return $"{config.Nickname}记录刷新成功！";
        }

        private const string _refreshCD = "RefreshRecords";

        #endregion

        #region 计算

        [PluginFunction(ActivateKeyword = "card", Name = "歌曲卡片（测试）")]
        public object? SongCard([PluginArgument(Name = "难度+曲名/id/别名")] string name)
        {
            if (this.UserConfig().UseHtml != true)
            {
                return "还没有开启maimai皮肤噢，使用 /mai config skin 开启";
            }
            var chart = TryGetSongChartUseAliasOrNameOrId(name, out var color);
            if (chart == null) return $"不知道{name}这首歌呢";
            var data = chart.GetChartData(color);
            if (data is null) return $"没有{chart.BasicInfo.SongTitle}({color})的谱面数据哦";
            var song = chart.BasicInfo;
            var status = chart.GetChartStatus(color)!;
            var imageUrl = StaticServices.BrowserService.ResolveFileUrl(song.JacketUrl, new SongChart());
            var constant = chart.GetChartConstantOfSpecificColor(color) ?? default;
            double totalScore = 500 * data.Tap + 1500 * data.Slide + 1000 * data.Hold + 500 * data.Touch +
                                2500 * data.Break;
            var cpBreakBonus = 0.01 / data.Break;//break bonus分分开计算。总共提供1%的额外奖励率，这里得出一个Critical Prefect break能提供的奖励率
            var p1BreakBonusReduce = (1 - 0.75) * cpBreakBonus;//cp额外分100，p1额外分75
            var p2BreakBonusReduce = (1 - 0.5) * cpBreakBonus;
            var greatBreakBonusReduce = (1 - 0.4) * cpBreakBonus;
            var goodBreakBonusReduce = (1 - 0.3) * cpBreakBonus;
            var missBreakBonusReduce = cpBreakBonus;


            var tapGreatReduce = -100 / totalScore;//因为tap满分500分，great400分，少了100分。
            var g1BreakReduce = 5 * tapGreatReduce - greatBreakBonusReduce;//因为cp break满分2500，g1 break分数2000，少了500分，较great少100少了5倍。
            var g2BreakReduce = 10 * tapGreatReduce - greatBreakBonusReduce;
            var g3BreakReduce = 12.5 * tapGreatReduce - greatBreakBonusReduce;
            var goodBreakReduce = 15 * tapGreatReduce - goodBreakBonusReduce;
            var p1BreakReduce = -p1BreakBonusReduce;
            var p2BreakReduce = -p2BreakBonusReduce;
            var missBreakReduce = 25 * tapGreatReduce - missBreakBonusReduce;

            var tapArray = new[] { 0, tapGreatReduce, 2.5 * tapGreatReduce, 5 * tapGreatReduce }.Select(p => p == 0 ? "-" : $"{p:P5}").ToList();
            var slideArray = new[] { 0, tapGreatReduce * 3, 7.5 * tapGreatReduce, 15 * tapGreatReduce }.Select(p => p == 0 ? "-" : $"{p:P5}").ToList();
            var holdArray = new[] { 0, tapGreatReduce * 2, 5 * tapGreatReduce, 10 * tapGreatReduce }.Select(p => p == 0 ? "-" : $"{p:P5}").ToList();
            var touchArray = new[] { 0, tapGreatReduce, 2.5 * tapGreatReduce, 5 * tapGreatReduce }.Select(p => data.Touch == 0 ? "-" : p == 0 ? "-" : $"{p:P5}")
                .ToList();
            var breakArray = new[] { p1BreakReduce, p2BreakReduce, g1BreakReduce, g2BreakReduce, g3BreakReduce, goodBreakReduce, missBreakReduce }.Select(p => p == 0 ? "-" : $"{p:P5}").ToList();

            var targetLine = 100.5;
            if (targetLine > 1.01)
            {
                targetLine /= 100.0;
            }

            targetLine *= 100;

            var lineRemark = $"BREAK 50落等价于 {(p1BreakReduce / tapGreatReduce):F3} 个 TAP GREAT\n" +
                             $"BREAK 粉2000等价于{(g1BreakReduce / tapGreatReduce):F3} 个 TAP GREAT";
            var remarkList = new List<string>();
            if (!song.Remark.IsNullOrEmpty()) remarkList.Add(song.Remark);
            remarkList.Add(lineRemark);
            remarkList.Add($"达成分数线 {100 / 100:P4} 允许的最多 TAP GREAT 数量为 {(101 - 100.0) / 100 / -tapGreatReduce:F3}个\n" +
                           $"达成分数线 {100.5 / 100:P4} 允许的最多 TAP GREAT 数量为 {(101 - 100.5) / 100 / -tapGreatReduce:F3}个");
            if (constant != 0)
            {
                remarkList.Add($"定数{constant}，100% Rating {DxCalculator.CalSongRating(100, constant)}，100.5% Rating {DxCalculator.CalSongRating(100.5, constant)}");
            }



            remarkList = remarkList.Select(p => p.Replace("\n", "</br>")).ToList();
            return new HtmlMessage(new
            {
                chart.ChartId,
                CurDifficult = chart.GetChartRatingOfSpecificColor(color),
                RemarkList = remarkList,
                Song = song,
                ImageUrl = imageUrl,
                Aliases = song.Aliases?.ToKouSetString(FormatType.Customize1, "，", false),
                Constant = chart.ToConstantString().Replace("/", " / "),
                Difficulty = chart.ToRatingString().Replace("/", " / "),
                CurColor = color,
                SongType = chart.SongChartType,
                CurColorClass = SongChart.GetCssColorClass(color),
                Chart = data,
                Status = status,
                SSSRank =
                        (((status.SSSRankOfSameDifficult + 1) / (double)status.SameDifficultCount) * 100).Round(1),
                SSSPeopleCount = $"{status.SSSCount}/{status.TotalCount}({status.SSSPeopleRatio:P})",
                AverageRate = $"{status.AverageRate:0.##}",
                TapArray = tapArray,
                SlideArray = slideArray,
                HoldArray = holdArray,
                TouchArray = touchArray,
                BreakArray = breakArray
            },
                new KouTemplate(TemplateResources.MaiSongInfoTemplate))
            { DpiRank = 3 };
        }

        [PluginFunction(ActivateKeyword = "分数线|line", Name = "计算分数线", Help =
                  "例如：/mai line 白潘 100\n" +
                  "命令将返回分数线允许的TAP GREAT容错以及BREAK 50落等价的TAP GREAT数。\n" +
                  "以下为 TAP GREAT 的对应表：\n" +
                  "GREAT/GOOD/MISS\n" +
                  "TAP    1/2.5/5\n" +
                  "HOLD   2/5/10\n" +
                  "SLIDE  3/7.5/15\n" +
                  "TOUCH  1/2.5/5\n" +
                  "BREAK  5/12.5/25(外加200落)\n鸣谢：Chiyuki-Bot")]
        public object? CalScoreLine([PluginArgument(Name = "难度+曲名/id/别名")] string name,
                  [PluginArgument(Name = "达成率", Min = 0, Max = 101)] double? line = null)
        {
            if (this.UserConfig().UseHtml == true)
            {
                return SongCard(name);
            }

            var chart = TryGetSongChartUseAliasOrNameOrId(name, out var type);
            if (chart == null) return $"不知道{name}这首歌呢";
            line ??= 100.5;
            if (line > 1.01)
            {
                line /= 100.0;
            }

            line *= 100;

            var data = chart.GetChartData(type);
            if (data is null) return $"没有{chart.BasicInfo.SongTitle}({type})的谱面数据哦";
            double totalScore = 500 * data.Tap + 1500 * data.Slide + 1000 * data.Hold + 500 * data.Touch +
                             2500 * data.Break;
            var breakBonus = 0.01 / data.Break;
            var break50Reduce = totalScore * breakBonus / 4.0;
            var reduce = 101 - line.Value;
            return
                $"{chart.BasicInfo.SongTitle}({type})分数线 {line / 100:P4} 允许的最多 TAP GREAT 数量为 {totalScore * reduce / 10000:F2}(每个-{100 / totalScore:P4})\n" +
                $"BREAK 50落(一共{data.Break}个)等价于 {(break50Reduce / 100):F3} 个 TAP GREAT(-{break50Reduce / totalScore:P4})";
        }


        private static readonly double[] _rateSeq = { 49, 50, 60, 70, 75, 80, 90, 94, 97, 98, 99, 99.5, 100, 100.5 };

        #endregion

        #region 数据采集

        [PluginFunction(Name = "更新谱面统计数据", Authority = Authority.BotManager)]
        public object UpdateChartStatus()
        {
            var statusData = DivingFishApi.GetChartStatusList();
            return $"影响到{statusData?.SaveToDb()}条记录";
        }

        [PluginFunction(Name = "更新谱面数据", Authority = Authority.BotManager)]
        public object UpdateChartInfos()
        {
            var statusData = DivingFishApi.GetChartInfoList();
            return $"影响到{statusData?.SaveToDb()}条记录";
        }

        [PluginFunction(Name = "更新歌曲封面到本地图片路径", Authority = Authority.BotManager)]
        public object? UpdateSongInfoImg()
        {
            using var context = new KouContext();
            foreach (var songInfo in context.Set<SongInfo>())
            {
                if (songInfo.JacketUrl is { } url && url.StartsWith("http"))
                {
                    songInfo.JacketUrl = $"{songInfo.SongTitleKaNa}\\base.png";
                }
            }
            return $"影响了{context.SaveChanges()}条记录";
        }

        [PluginFunction(Name = "更新歌曲数据", Authority = Authority.BotMaster)]
        public object? UpdateSongInfos()
        {
            var updater = new SongInfoUpdater();
            if (!updater.StartUpdate())
            {
                return updater.ErrorMsg;
            }

            var sb = new StringBuilder();
            sb.Append($"数据库影响了{updater.SaveToDb()}条数据");

            sb.Append($"相似{updater._similarDict.Count}个，");
            sb.Append($"添加{updater.AddedList.Count}个，");
            sb.Append($"更新{updater.UpdatedList.Count}个\n");

            sb.Append($"********相似{updater._similarDict.Count}个：\n");
            sb.Append(updater._similarDict.Select(p =>
                    $"————\n{p.Key.ToJsonStringForce(customOptions: new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault })}\n候选：{p.Value.Select(s => s.ToString(FormatType.Customize1)).StringJoin('\n')}————\n")
                .ToList().StringJoin('\n'));
            sb.Append($"\n********添加{updater.AddedList.Count}个：\n");
            sb.Append(updater.AddedList.Select(p => p.ToJsonStringForce(customOptions: new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault })).StringJoin('\n'));
            sb.Append($"\n********更新{updater.UpdatedList.Count}个：\n");
            sb.Append(updater.UpdatedList.Select(p => p.ToJsonStringForce(customOptions: new JsonSerializerOptions() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault })).StringJoin('\n'));

            return sb.ToString();
        }

        #endregion
        #region 配置

        [PluginFunction(ActivateKeyword = "config skin", Name = "签到皮肤", NeedCoin = 500)]
        public object? ConfigUseHtml()
        {
            var config = this.UserConfig()!;
            var consumeDesc = "";
            if (config.UseHtml == null)
            {
                if (!CurKouUser.HasEnoughFreeCoin(500)) return FormatNotEnoughCoin(500);
                CurKouUser.ConsumeCoinFree(500);
                consumeDesc = $"\n[{FormatConsumeFreeCoin(500)}]";
                config.UseHtml = false;
            }

            config.UseHtml = !config.UseHtml.Value;
            config.SaveChanges();
            return $"maimai皮肤已{config.UseHtml.Value.IIf("启用", "关闭")}{consumeDesc}";
        }


        #endregion

        #region 段位

        [PluginFunction(ActivateKeyword = "段位表", Name = "段位表")]
        public object? RankTable()
        {
            var dictionary = new Dictionary<string, int>()
            {
                {"初学者",0} ,{"实习生",250} ,{"初出茅庐",500} ,{"修行中",750} ,{"初段",1000} ,{"二段",1200} ,{"三段",1400} ,{"四段",1500} ,{"五段",1600} ,{"六段",1700} ,{"七段",1800} ,{"八段",1850} ,{"九段",1900} ,{"十段",1950} ,{"真传",2000} ,{"真传壹段",2010} ,{"真传贰段",2020} ,{"真传叁段",2030} ,{"真传肆段",2040} ,{"真传伍段",2050} ,{"真传陆段",2060} ,{"真传柒段",2070} ,{"真传捌段",2080} ,{"真传玖段",2090},{"真传拾段",2100}
            };
            return dictionary.Select(p => $"{p.Key}——{p.Value}").StringJoin('\n');
        }

        #endregion
        static KouMaimai()
        {
            AddEveryDayCronTab(() =>
            {
                var statusData = DivingFishApi.GetChartStatusList();
                statusData?.SaveToDb();

                using (var context = new KouContext())
                {
                    //每天0点清空地图卡数量
                    foreach (var map in ArcadeMap.DbWhere(p => p.PeopleCount != null, context))
                    {
                        map.PeopleCount = null;
                    }
                    context.SaveChanges();
                }
                ArcadeMap.UpdateCache();
            });

            PluginEventList.FetchGroupGameInfo += sender =>
            {
                if (sender.CurGroup?.HasInstallPlugin(GetPluginMetadataStatic().Info) is true)
                {
                    var info = GetPluginMetadataStatic().Info;
                    var func = info.GetFunctionInfo(nameof(GuessImage));
                    return new PluginEventList.GameInfo()
                    {
                        GameCommand =
                            $"{KouCommand.GetPluginRoute(sender.CurKouGlobalConfig, info, nameof(GuessImage))} --help",
                        Introduce = func?.FunctionHelp ?? "??",
                        GameName = func?.FunctionName ?? "??",
                        IsSessionRoomGame = true,
                    };
                }

                return null;
            };

            TemplatePool.Put(nameof(TemplateResources.MaimaiRecordListTemplate),
                TemplateResources.MaimaiRecordListTemplate);
        }

        #region 房间游戏

        [PluginFunction(Name = "maimai猜图游戏", ActivateKeyword = "guess image|猜图",Help = MaiImageGuessGameRoom.Help
        , OnlyUsefulInGroup = true, NeedCoin = 10, CanEarnCoin = true)]
        public object? GuessImage([PluginArgument(Name = "入场费(最低10)", Min = 10)] int? fee = null)
        {
            fee ??= 10;
            if (!CurKouUser.ConsumeCoinFree(fee.Value)) return FormatNotEnoughCoin(fee.Value, CurUserName);
            var room = new MaiImageGuessGameRoom("maimai猜图", CurUser, CurGroup, fee)
            {
                LastTime = CurCommand.CustomTimeSpan ?? new TimeSpan(0,10,0)
            };
            ConnectRoom(
                $"{CurUserName}消耗{CurKouGlobalConfig.CoinFormat(fee.Value)}创建了游戏房间：{room.RoomName}，后续收到的入场费({CurKouGlobalConfig.CoinFormat(fee.Value)})将累计在奖池中，按排名发放奖励",
                room);
            return null;
        }

        [PluginFunction(Name = "检查图片情况", Authority = Authority.BotMaster)]
        public object? CheckImage()
        {
            var list = new List<string>();
            foreach (var (url, p) in SongChart.GetCache()!.Select(p=>(p.BasicInfo.JacketUrl, p)))
            {
                if(url.IsNullOrWhiteSpace()) continue;
                var u = new KouImage(url, new SongChart());
                if (!u.LocalExists())
                {
                    list.Add(p.ToString(FormatType.Brief));
                }
            }

            return $"共找到{list.Count}个缺失：\n{list.StringJoin("\n")}";
        }
        #endregion
    }
}
