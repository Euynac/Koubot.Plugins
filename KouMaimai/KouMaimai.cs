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
using Koubot.SDK.PluginExtension.Result;
using Koubot.SDK.Services;
using Koubot.SDK.System.Messages;
using Koubot.SDK.Templates;
using Koubot.Shared.Protocol;
using Koubot.Tool.General;
using KouMaimai;

namespace KouGamePlugin.Maimai
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    [PluginClass("mai", "maimai",
        PluginType = PluginType.Game,
        CanUseProxy = true)]
    public class KouMaimai : KouPlugin<KouMaimai>,
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

        [PluginFunction(ActivateKeyword = "难度成绩", Name = "获取指定难度成绩")]
        public object GetDifficultRecords([PluginArgument(Name = "难度如14+")] string rating)
        {
            using var context = new KouContext();
            var records = SongRecord.DbWhere(p =>
                    p.User == CurKouUser && p.CorrespondingChart.GetChartRatingOfSpecificColor(p.RatingColor) == rating, context)
                .OrderByDescending(p => p.Achievements).ToList();
            if (records.Count == 0) return $"{CurUserName}没有{rating}难度的成绩哦";
            return FormatRecords(records, $"{CurUserName}的难度{rating}成绩：");
        }

        [PluginFunction(ActivateKeyword = "b40|b15|b25", Name = "获取B40记录")]
        public object GetRecords()
        {
            var sketch = new StringBuilder();
            var b40 = SongRecord.GetB40Charts(CurKouUser);
            if (b40.IsNullOrEmptySet()) return "当前没有记录哦，是不是没有绑定Diving-Fish账号呢，私聊Kou使用/mai bind 用户名 密码绑定";
            if (CurCommand.FunctionActivateName == "b15")
            {
                var b15 = b40.Where(p => p.CorrespondingChart.BasicInfo.IsNew == true).OrderByDescending(p => p.Rating)
                    .ToList();
                sketch.Append($"{CurUserName}的新B15(总Rating{b15.Sum(p => p.Rating)})");
                return b15.ToAutoPageSetString(sketch.ToString());
            }

            if (CurCommand.FunctionActivateName == "b25")
            {
                var b25 = b40.Where(p => p.CorrespondingChart.BasicInfo.IsNew == false).OrderByDescending(p => p.Rating)
                    .ToList();
                sketch.Append($"{CurUserName}的旧B25(总Rating{b25.Sum(p => p.Rating)})");
                return b25.ToAutoPageSetString(sketch.ToString());
            }
            sketch.Append($"{CurUserName}的B40(总Rating{b40.Sum(p => p.Rating)})");
            return FormatRecords(
                b40.OrderByDescending(p => p.CorrespondingChart.SongChartType).ThenByDescending(p => p.Rating),
                sketch.ToString());
        }

        [PluginFunction(ActivateKeyword = "单曲", Name = "获取自己某个单曲成绩")]
        public object SpecificRecord([PluginArgument(Name = "难度+曲名/id/别名")] string name)
        {
            var chart = TryGetSongChartUseAliasOrNameOrId(name, out var type);
            if (chart == null) return $"不知道{name}这首歌呢";
            var config = this.UserConfig();
            if (HasBindError(config, out var reply)) return reply;
            using var context = new KouContext();
            var record = SongRecord.SingleOrDefault(p => p.User == CurKouUser && p.CorrespondingChart.Equals(chart) && p.RatingColor == type, context);
            if (record == null) return $"{config.Nickname}没有{chart.ToSpecificRatingString(type)}这歌的记录哦";
            return record.ToString(FormatType.Detail);
        }

        [PluginFunction(ActivateKeyword = "info", Name = "获取用户信息")]
        public object UserProfile()
        {
            var config = this.UserConfig();
            if (HasBindError(config, out var reply)) return reply;
            return config.ToUserProfileString(CurKouUser);
        }

        private const string _refreshCD = "RefreshRecords";
        [PluginFunction(ActivateKeyword = "bind",
            Name = "绑定查分账号", Help = "绑定Diving-Fish账号，需要私发Kou用户名密码", CanUseProxy = false)]
        public object BindAccount(
            [PluginArgument(Name = "用户名")] string username,
            [PluginArgument(Name = "密码")] string password)
        {
            var api = new DivingFishApi(username, password, null, null);
            if (!api.Login())
            {
                return $"{CurUser.Name}登录失败了呢，Diving-Fish说：{api.ErrorMsg}";
            }

            if (api.GetProfile() is not { } profile)
            {
                return $"{CurUser.Name}登录成功，但获取用户信息失败：{api.ErrorMsg}";
            }

            if (CDOfFunctionKouUserIsIn(_refreshCD, new TimeSpan(0, 0, 1, 0), out var remaining))
            {
                return FormatIsInCD(remaining);
            }
            var config = this.UserConfig();
            var isFirstTime = config.Username == null;
            config.Username = username;
            config.Password = password;
            config.LoginTokenValue = api.TokenValue;
            config.TokenRefreshTime = DateTime.Now;
            config.Nickname = profile.nickname;
            config.Plate = profile.plate;
            config.AdditionalRating = profile.additional_rating;
            config.SaveChanges();
            var bindSuccessFormat = $"{CurUser.Name}绑定成功！";
            var profileAppend = $"\n{config.ToUserProfileString(CurKouUser)}";
            if (isFirstTime)
            {
                Reply(bindSuccessFormat + "初次绑定，正在获取所有成绩..." + profileAppend);
                if (!api.FetchUserRecords(CurKouUser))
                {
                    CDOfUserFunctionReset(_refreshCD);
                    return $"获取成绩失败：{api.ErrorMsg}";
                }

                return $"{config.Nickname}记录刷新成功！";
            }

            return bindSuccessFormat + profileAppend;
        }

        #endregion

        #region 牌子

        [PluginFunction(ActivateKeyword = "牌子", Name = "获取牌子距离信息")]
        public object GetPlateInfo([PluginArgument(Name = "如桃极")] string plateName)
        {
            var failed = "请输入正确的牌子名称，如桃极、舞神、橙将等";
            if (plateName.Length < 2) return failed;
            var plateType = plateName[^1].ToString();
            if (plateType is "舞" or "者")
            {
                plateType = plateName[^2..];
            }
            var versionStr = plateName.Remove(plateName.Length - plateType.Length, plateType.Length);
            if (plateName is "霸者" or "覇者")
            {
                versionStr = "霸者";
            }
            if (versionStr == "")
            {
                return failed;
            }
            if (TypeService.TryConvert(plateType, out PlateType type))
            {
                if (!TypeService.TryConvert(versionStr, out SongVersion version))
                {
                    if (type != PlateType.覇者)
                    {
                        return $"不知道{versionStr}是哪个版本的牌子";
                    }
                }

                string plateFullName;
                switch (type)
                {
                    case PlateType.覇者:
                        plateFullName = type.ToString();
                        break;
                    default:
                        plateFullName = version.GetKouEnumName(2) + type;
                        break;
                }
                var plateDesc = $"【{plateFullName}】\n{version.GetKouEnumName()}{type.GetDescription()}";
                var config = this.UserConfig();
                if (HasBindError(config, out var notBindReply))
                {
                    return $"{plateDesc}\n{notBindReply}";
                }

                var chartType = SongChart.ChartType.SD;
                if (version >= SongVersion.maimaiでらっくす) chartType |= SongChart.ChartType.DX;


                var sb = new StringBuilder(plateDesc);
                using var context = new KouContext();
                var relativeSongs = SongChart.Find(p => p.BasicInfo.Version != null && version.HasFlag(p.BasicInfo.Version.Value) && chartType.HasFlag(p.SongChartType!.Value));
                var relativeRecords = SongRecord.DbWhere(p =>
                    p.User == CurKouUser && p.RatingColor == SongChart.RatingColor.Master &&
                    p.CorrespondingChart.BasicInfo.Version is { } v && version.HasFlag(v) &&
                    chartType.HasFlag(p.CorrespondingChart.SongChartType!.Value), context).ToHashSet();
                var relativePlayedSongs = relativeRecords.Select(p => p.CorrespondingChart).ToHashSet();
                var reachRequiredRecords = ReachedRecords(relativeRecords).ToHashSet();
                var notReachedRecords = relativeRecords.Where(p => !reachRequiredRecords.Contains(p)).OrderByDescending(p => p.Achievements).ToList();
                var notPlaySongs = relativeSongs.Where(s => !relativePlayedSongs.Contains(s)).ToList();
                sb.Append($"\n{CurUserName}的紫谱进度：{reachRequiredRecords.Count}/{relativeSongs.Count}");
                if (notReachedRecords.Count > 0 || notPlaySongs.Count > 0)
                {
                    sb.Append(
                        $"，其中有{notReachedRecords.Count}首未达成，{notPlaySongs.Count}首未游玩。");
                    if (notReachedRecords.Count > 0) sb.Append($"\n未达成曲目：\n{notReachedRecords.ToKouSetStringWithID()}");
                    if (notPlaySongs.Count > 0) sb.Append($"\n未游玩曲目：\n{notPlaySongs.ToKouSetStringWithID()}");
                }
                else
                {
                    sb.Append($"，恭喜{plateFullName}确认！");
                }

                if (reachRequiredRecords.Count > 0)
                {
                    sb.Append($"\n已达成曲目：\n{reachRequiredRecords.ToKouSetStringWithID()}");
                }
                return sb.ToString();
            }

            return $"不知道{plateType}是哪种牌子，可选的有：{PlateType.将.GetAllValues().StringJoin(',')}";

            HashSet<SongRecord> ReachedRecords(HashSet<SongRecord> records)
            {
                switch (type)
                {
                    case PlateType.極:
                        return records.Where(p => p.FcStatus != null).ToHashSet();
                    case PlateType.将:
                        return records.Where(p => p.Achievements >= 100).ToHashSet();
                    case PlateType.神:
                        return records.Where(p => p.FcStatus >= SongRecord.FcType.Ap).ToHashSet();
                    case PlateType.舞舞:
                        return records.Where(p => p.FsStatus >= SongRecord.FsType.Fsd).ToHashSet();
                    case PlateType.覇者:
                        return records.Where(p => p.Achievements >= 80).ToHashSet();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

        }

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
        [PluginFunction(Name = "计算单曲rating", ActivateKeyword = "cal", Help = "如果不输入达成率，默认输出跳变阶段的所有rating")]
        public string CalRating([PluginArgument(Name = "定数/歌曲名")] string constantOrName,
            [PluginArgument(Name = "达成率", Min = 0, Max = 101)] double? rate = null)
        {
            SongChart song = null;
            if (!double.TryParse(constantOrName, out var constant))
            {
                if (constantOrName != null)
                {
                    song = TryGetSongChartUseAliasOrNameOrId(constantOrName, out var type);
                    if (song == null) return $"不知道{constantOrName}是什么歌呢";
                    constant = song.GetChartConstantOfSpecificColor(type) ?? 0;
                    if (constant == 0) return $"Kou还不知道{type.GetKouEnumName()}{song.BasicInfo.SongTitle}的定数呢";
                }
                else
                {
                    return "需要提供正确的歌曲名或者定数哦";
                }
            }

            var songFormat = song?.Be(song.ToString(FormatType.Brief) + "\n");
            if (rate != null)
            {
                if (rate > 1.01)
                {
                    rate /= 100.0;
                }
                var rating = DxCalculator.CalSongRating(rate.Value, constant);
                return $"{songFormat}定数{constant:F1}，达成率{rate:P4}时，Rating为{rating}";
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{songFormat}定数{constant}时：\n");
            for (var i = _rateSeq.Length - 1; i >= 0; i--)
            {
                var r = _rateSeq[i];
                var rating = DxCalculator.CalSongRating(r, constant);
                stringBuilder.Append($"{r}% ———— {rating}\n");
            }

            return stringBuilder.ToString().TrimEnd();
        }
        #endregion

        #region 歌曲别名
        [PluginFunction(Name = "获取歌曲详情", ActivateKeyword = "song")]
        public object GetSongByAlias(string aliasName)
        {
            var chart = TryGetSongChartUseAliasOrNameOrId(aliasName, out var type);
            if (chart == null) return $"不知道{aliasName}是什么歌呢";
            return $"您要找的是不是：\n{chart.ToString(FormatType.Detail)}";
        }

        private SongChart TryGetSongChartUseAliasOrNameOrId(string aliasOrName, out SongChart.RatingColor type)
        {
            SongChart song = null;
            if (!aliasOrName.StartsWithAny(false, out var difficultStr, "白", "紫", "红", "黄", "绿") ||
                !difficultStr.TryToKouEnum(out type))
            {
                type = SongChart.RatingColor.Master;
            }
            else
            {
                aliasOrName = aliasOrName[1..].Trim();
            }

            var list = SongChart.Find(p => p.BasicInfo.Aliases.Any(a => a.Alias.Equals(aliasOrName, StringComparison.OrdinalIgnoreCase)));
            if (list.Count == 1) return list[0];
            list = SongChart.Find(p => p.OfficialId.ToString() == aliasOrName || p.ChartId.ToString() == aliasOrName ||
                                       p.BasicInfo.Aliases.Any(a =>
                                           a.Alias.Contains(aliasOrName, StringComparison.OrdinalIgnoreCase)) ||
                                       p.BasicInfo.SongTitle.Contains(aliasOrName, StringComparison.OrdinalIgnoreCase) || p.BasicInfo.SongTitleKaNa.Contains(aliasOrName, StringComparison.OrdinalIgnoreCase));
            if (list.Count == 0 && aliasOrName.Length > 6)
            {
                var similarityList = SongChart.GetCache()!.OrderByDescending(p => StringTool.Similarity(p.BasicInfo.SongTitle, aliasOrName))
                    .Take(5).ToList();
                if (similarityList.Count == 0) return null;
                using (SessionService)
                {
                    var id = SessionService.Ask<int>($"为你找到以下可能的歌[相似度依次为{similarityList.Select(p=>$"{StringTool.Similarity(p.BasicInfo.SongTitle, aliasOrName):P}").StringJoin(",")}]，输入id：\n{similarityList.ToKouSetStringWithID(5)}");
                    song = similarityList.ElementAtOrDefault(id - 1);
                }
            }
            if (list.Count == 1)
            {
                song = list[0];
            }
            else if (list.Count > 1)
            {
                using (SessionService)
                {
                    var id = SessionService.Ask<int>($"具体是下面哪首歌呢？输入id：\n{list.ToKouSetStringWithID(5)}");
                    song = list.ElementAtOrDefault(id - 1);
                }
            }

            return song;
        }

        [PluginFunction(ActivateKeyword = "add|教教", Name = "学新的歌曲别名", Help = "教kou一个歌曲的别名。", CanEarnCoin = true)]
        public object KouLearnAnotherName(
            [PluginArgument(Name = "歌曲名/ID等")] string songName,
            [PluginArgument(Name = "要学的歌曲别名")] string songAnotherName)
        {
            if (songName.IsNullOrWhiteSpace() || songAnotherName.IsNullOrWhiteSpace()) return "好好教我嘛";
            var haveTheAlias = SongAlias.SingleOrDefault(p => p.Alias == songAnotherName);
            if (haveTheAlias != null)
                return $"可是我之前就知道{haveTheAlias.CorrespondingSong.SongTitle}可以叫做{songAnotherName}(ID{haveTheAlias.AliasID})了";

            var chart = TryGetSongChartUseAliasOrNameOrId(songName, out _);
            if (chart == null) return $"不知道哪个歌叫{songName}噢";
            var song = chart.BasicInfo;
            var sourceUser = CurUser.FindThis(Context);
            var dbSong = song.FindThis(Context);
            var havenHadAliases = dbSong.Aliases?.Select(p => p.Alias).StringJoin("、");
            var success = SongAlias.Add(alias =>
            {
                alias.CorrespondingSong = dbSong;
                alias.Alias = songAnotherName;
                alias.SourceUser = sourceUser;
            }, out var added, out var error, Context);
            if (success)
            {
                SongChart.UpdateCache();
                var reward = RandomTool.GetInt(5, 15);
                CurUser.KouUser.GainCoinFree(reward);
                return $"学会了，{song.SongTitle}可以叫做{songAnotherName}(ID{added.AliasID})" +
                       $"{havenHadAliases?.BeIfNotEmpty($"，我知道它还可以叫做{havenHadAliases}！")}\n" +
                       $"[{FormatGainFreeCoin(reward)}]";
            }
            return $"没学会，就突然：{error}";
        }
        [PluginFunction(ActivateKeyword = "del|delete|忘记", Name = "忘记歌曲别名", Help = "叫kou忘掉一个歌曲的别名。")]
        public string KouForgetAnotherName(
            [PluginArgument(Name = "别名或别名ID")] List<string> ids)
        {
            if (ids.IsNullOrEmptySet()) return "这是叫我忘掉什么嘛";
            var result = new StringBuilder();
            foreach (var i in ids)
            {
                SongAlias alias = null;
                if (i.IsInt(out var id))
                {
                    alias = SongAlias.SingleOrDefault(a => a.AliasID == id);
                }
                alias ??= SongAlias.SingleOrDefault(a => a.Alias.Equals(i));

                if (alias == null) result.Append($"\n不记得有别名或ID是{i}");
                else if (alias.SourceUser != null && alias.SourceUser != CurUser &&
                         !CurUser.HasTheAuthority(Authority.BotManager))
                    result.Append($"\nID{i}是别人贡献的，不可以删噢");
                else
                {
                    result.Append($"\n忘记了{alias.ToString(FormatType.Brief)}");
                    alias.DeleteThis();
                    SongChart.UpdateCache();
                };
            }

            return result.ToString().TrimStart();
        }
        #endregion

        #region 地图

        [PluginFunction(ActivateKeyword = "哪里少人|哪里人少", Name = "看哪个机厅人少", OnlyUsefulInGroup = true)]
        public object GetLessPeopleArcade()
        {
            var config = this.GroupConfig()!;
            if (config.MapDefaultArea == null)
            {
                return "没有设置当前群的查卡默认地区哦，使用如/mai 地区 广州市 设置";
            }

            var list = ArcadeMap.Find(p => p.Address.Contains(config.MapDefaultArea));
            if (list.Count == 0) return $"群设置的默认地区{config.MapDefaultArea}地区没有收录机厅呢";
            return list.OrderBy(p => p.PeopleCount?.Sum(r => r.AlterCount) ?? 1000).ToAutoPageSetString($"当前{config.MapDefaultArea}地区排卡情况",
                s =>
                {
                    s.MultiFormatType = FormatType.Customize3;
                    s.UseItemID = true;
                });
        }

        [PluginFunction(ActivateKeyword = "地区", Name = "设置DX几卡查询默认地区", OnlyUsefulInGroup = true)]
        public object SetDefaultArea([PluginArgument(Name = "地区名（如广州市）")] string area)
        {
            var config = this.GroupConfig()!;
            config.MapDefaultArea = area;
            config.SaveChanges();
            return $"当前群查卡默认地区修改为{area}成功！";
        }

        [PluginFunction(ActivateKeyword = "有谁", Name = "看谁加了卡", Help = "照抄几卡Bot")]
        public object WhoAreThere([PluginArgument(Name = "机厅名")] string name)
        {
            if (!TryGetArcade(name, out var arcade, out var reply)) return reply;
            return arcade.ToString(FormatType.Customize2);
        }


        [PluginFunction(ActivateKeyword = "加卡", Name = "往机厅加卡", Help = "照抄几卡Bot")]
        public object AlterCards([PluginArgument(Name = "机厅名")] string name,
            [PluginArgument(Name = "加1|-1等")] string alterCount)
        {
            var isSub = false;
            if (alterCount.MatchOnceThenReplace("([+加减-])", out alterCount, out var result))
            {
                if (result[1].Value is "-" or "减")
                {
                    isSub = true;
                }
            }
            if (!TypeService.TryConvert(alterCount, out int alterCountInt))
            {
                return "请输入正确的数字噢";
            }

            if (isSub) alterCountInt *= -1;

            if (!TryGetArcade(name, out var arcade, out var reply))
            {
                return reply;
            }
            using var context = new KouContext();
            arcade.PeopleCount ??= new List<ArcadeMap.CardRecord>();
            arcade.PeopleCount.Add(new ArcadeMap.CardRecord(CurKouUser.FindThis(context), alterCountInt));
            ArcadeMap.Update(arcade, out _, context);
            return arcade.ToString(FormatType.Customize1);
        }
        /// <summary>
        /// 获取机厅
        /// </summary>
        private bool TryGetArcade(string name, out ArcadeMap arcade, out string reply)
        {
            arcade = null;
            reply = null;
            string? defaultArea = null;
            if (CurGroup != null)
            {
                var config = this.GroupConfig()!;
                defaultArea = config.MapDefaultArea;
            }
            ArcadeMap map = null;
            if (!name.IsInt(out var id))
            {
                var list = ArcadeMap.Find(p => (defaultArea == null || defaultArea != null && p.Address.Contains(defaultArea))
                                               && (p.Aliases != null &&
                                                   p.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase))
                                                   || p.ArcadeName.Contains(name)));

                switch (list.Count)
                {
                    case 0:
                        reply = $"{defaultArea}没有收录{name}该机厅呢";
                        return false;
                    case > 1:
                        map = list.ElementAtOrDefault(SessionService.Ask<int>($"是下面第几个？输入id：{list.ToKouSetStringWithID(5)}") - 1);
                        break;
                    case 1:
                        map = list[0];
                        break;
                }
            }
            else
            {
                map = ArcadeMap.SingleOrDefault(p => p.LocationId == id);
            }


            if (map == null)
            {
                reply = "(⊙﹏⊙)？";
                return false;
            }

            arcade = map;
            return true;
        }

        [PluginFunction(ActivateKeyword = "几卡", Name = "机厅几卡", Help = "照抄几卡Bot")]
        public object HowManyCards(string name)
        {
            if (!TryGetArcade(name, out var arcade, out var reply)) return reply;
            return arcade.ToString(FormatType.Customize1);
        }

        [PluginFunction(ActivateKeyword = "加地区别名", Name = "增加地区别名")]
        public object AddAreaAlias([PluginArgument(Name = "机厅名")] string name, [PluginArgument(Name = "别名")] List<string> aliases)
        {
            if (!TryGetArcade(name, out var arcade, out var reply)) return reply;
            arcade.Aliases ??= new List<string>();
            aliases.AddRange(arcade.Aliases);
            aliases = aliases.Distinct().ToList();
            if (!SessionService.AskConfirm($"{arcade.ArcadeName}的别名：{aliases.StringJoin(',')}，输入y确认"))
                return null;
            arcade.Aliases = aliases;
            ArcadeMap.Update(arcade, out _);
            return "添加成功";
        }
        [PluginFunction(ActivateKeyword = "清除所有加卡记录", Authority = Authority.BotManager)]
        public object ClearAreaCards()
        {
            using var context = new KouContext();
            foreach (var map in ArcadeMap.DbWhere(p => p.PeopleCount != null, context))
            {
                map.PeopleCount = null;
            }
            var effect = context.SaveChanges();
            ArcadeMap.UpdateCache();
            return $"影响到了{effect}条记录";
        }

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

            TemplatePool.Put(nameof(TemplateResources.MaimaiRecordListTemplate),
                TemplateResources.MaimaiRecordListTemplate);
        }
    }
}
