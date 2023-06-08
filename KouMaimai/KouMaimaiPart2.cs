using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FuzzySharp;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.Services;
using Koubot.SDK.System.Messages;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using Koubot.Tool.String;
using KouGamePlugin.Maimai.Models;
using KouMaimai;
using static KouGamePlugin.Maimai.Models.DivingFishBest40ResponseDto;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace KouGamePlugin.Maimai;

public partial class KouMaimai
{
    [PluginParameter(ActivateKeyword = "b40", Name = "使用B40时代的算分方法")]
    public bool UseB40Algorithm { get; set; }

    #region Diving-Fish

    [PluginFunction(Name = "获取指定难度所有歌曲信息", ActivateKeyword = "难度|nd")]
    public object? GetSpecificDifficultSongs([PluginArgument(Name = "难度如14+")] string givenRating,
        [PluginArgument(Name = "达成率阈值")] double? threshold = null)
    {
        if (this.UserConfig().UseHtml != true)
        {
            return "还没有开启maimai皮肤噢，使用 /mai config skin 开启";
        }
        AutoCheckIfNeedRefresh();
        givenRating = givenRating.Replace("＋", "+");
        using var context = new KouContext();
        var records = SongRecord.DbWhere(p =>
                    p.User == CurKouUser && p.CorrespondingChart.GetChartRatingOfSpecificColor(p.RatingColor) == givenRating,
                context)
            .OrderByDescending(p => p.Achievements).ToList();
        var charts = SongChart.GetCache();
        if (charts.IsNullOrEmptySet()) return "当前没有歌曲信息哦";
        //var matchedCharts = new List<(SongChart.RatingColor Color, double? Contant, int? DenseDegree, string ImageUrl,
        //    double? Achievement, bool IsDx)>();
        var matchedCharts = new List<dynamic>();
        foreach (var chart in charts)
        {
            foreach (var color in Enum.GetValues<SongChart.RatingColor>())
            {
                var rating = chart.GetChartRatingOfSpecificColor(color);
                var constant = chart.GetChartConstantOfSpecificColor(color);
                if (rating.IsNullOrWhiteSpace() || rating != givenRating) continue;
                var status = chart.GetChartStatus(color);
                if (status == null)
                {
                    continue;
                }
                var achievement = records.FirstOrDefault(p => p.CorrespondingChart.Equals(chart) && p.RatingColor == color)?.Achievements;

                matchedCharts.Add(new
                {
                    Color = SongChart.GetCssColorClass(color),
                    Constant = constant,
                    DenseDegree = GetDenseCss(status?.DifficultTag.ToInt()),
                    FitConstant = status?.FitConstant ?? constant,
                    ImageUrl = StaticServices.BrowserService?.ResolveFileUrl(chart.BasicInfo.JacketUrl, new SongChart()),
                    Achievement = achievement?.ToString("F4"),
                    IsDx = chart.SongChartType == SongChart.ChartType.DX,
                    Id = chart.ChartId,
                    IsDiscarded = threshold != null && achievement != null && achievement >= GetAchievementFromGivenNum(threshold),
                });
            }
        }

        var containers = matchedCharts.Where(p => p.Constant != null && p.Constant != 0).OrderByDescending(p => p.Constant).ThenByDescending(p => p.FitConstant).GroupBy(p => p.Constant).Select(tuples => (tuples.Key?.ToString() ?? "未知", tuples.ToList())).Select(dummy => new { Constant = dummy.Item1, Items = dummy.Item2 }).ToList();
        return new HtmlMessage(new { Header = $"maimai DX {givenRating} 歌曲", AppendInfo=new MarkdownMessage().AddText($"\n刷新时间：{this.UserConfig().LastGetRecordsTime}").MarkdownContent.ToString(), Containers = containers },
            new KouTemplate(TemplateResources.SongListCardTemplate))
        { DpiRank = 2 };
    }
    private static string GetDenseCss(int? degree)
    {
        return degree switch
        {
            4 => "dense5",
            3 => "dense4",
            2 => "dense3",
            1 => "dense2",
            _ => "dense1"
        };
    }
    [PluginFunction(ActivateKeyword = "难度成绩", Name = "获取指定难度成绩")]
    public object GetDifficultRecords([PluginArgument(Name = "难度如14+")] string rating)
    {
        rating = rating.Replace("＋", "+");
        AutoCheckIfNeedRefresh();
        using var context = new KouContext();
        var records = SongRecord.DbWhere(p =>
                    p.User == CurKouUser && p.CorrespondingChart.GetChartRatingOfSpecificColor(p.RatingColor) == rating,
                context)
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
        AutoCheckIfNeedRefresh();
        if (CurCommand.FunctionActivateName == "b15")
        {
            var b15 = b40.Where(p => p.CorrespondingChart.BasicInfo.IsNew == true).OrderByDescending(p => p.B40Rating)
                .ToList();
            sketch.Append($"{CurUserName}的新B15(总Rating{b15.Sum(p => p.B40Rating)})");
            return b15.ToAutoPageSetString(sketch.ToString());
        }

        if (CurCommand.FunctionActivateName == "b25")
        {
            var b25 = b40.Where(p => p.CorrespondingChart.BasicInfo.IsNew == false).OrderByDescending(p => p.B40Rating)
                .ToList();
            sketch.Append($"{CurUserName}的旧B25(总Rating{b25.Sum(p => p.B40Rating)})");
            return b25.ToAutoPageSetString(sketch.ToString());
        }

        sketch.Append($"{CurUserName}的B40(总Rating{b40.Sum(p => p.B40Rating)})");
        return FormatRecords(
            b40.OrderByDescending(p => p.CorrespondingChart.SongChartType).ThenByDescending(p => p.B40Rating),
            sketch.ToString());
    }

    [PluginFunction(ActivateKeyword = "单曲", Name = "获取自己某个单曲成绩")]
    public object SpecificRecord([PluginArgument(Name = "难度+曲名/id/别名")] string name)
    {
        var chart = TryGetSongChartUseAliasOrNameOrId(name, out var type);
        if (chart == null) return $"不知道{name}这首歌呢";
        AutoCheckIfNeedRefresh();
        var config = this.UserConfig();
        if (HasBindError(config, out var reply)) return reply;
        using var context = new KouContext();
        var record =
            SongRecord.SingleOrDefault(
                p => p.User == CurKouUser && p.CorrespondingChart.Equals(chart) && p.RatingColor == type, context);
        if (record == null) return $"{config.Nickname}没有{chart.ToSpecificRatingString(type)}这歌的记录哦";
        return record.ToString(FormatType.Detail);
    }

    #endregion

    #region 牌子

    [PluginFunction(ActivateKeyword = "牌子|pz", Name = "获取牌子距离信息")]
    public object GetPlateInfo([PluginArgument(Name = "如桃极")] string plateName)
    {
        var failed = "请输入正确的牌子名称，如桃极、舞神、橙将等";
        if (plateName.Length < 2) return failed;
        AutoCheckIfNeedRefresh();
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
            if (version >= SongVersion.maimaiでらっくす) chartType = SongChart.ChartType.DX;


            var sb = new StringBuilder(plateDesc);
            using var context = new KouContext();
            var relativeSongs = SongChart.Find(p =>
                p.Version != null && version.HasFlag(p.Version));
            var relativeRecords = SongRecord.DbWhere(p =>
                p.User == CurKouUser && p.RatingColor == SongChart.RatingColor.Master &&
                p.CorrespondingChart.Version is { } v && version.HasFlag(v) &&
                chartType.HasFlag(p.CorrespondingChart.SongChartType!.Value), context).ToHashSet();
            var relativePlayedSongs = relativeRecords.Select(p => p.CorrespondingChart).ToHashSet();
            var reachRequiredRecords = ReachedRecords(relativeRecords).ToHashSet();
            var notReachedRecords = relativeRecords.Where(p => !reachRequiredRecords.Contains(p))
                .OrderByDescending(p => p.Achievements).ToList();
            var notPlaySongs = relativeSongs.Where(s => !relativePlayedSongs.Contains(s)).ToList();
            var markdown = new MarkdownMessage();
            var processDesc = $"\n{CurUserName}的紫谱进度：{reachRequiredRecords.Count}/{relativeSongs.Count}";
            markdown.AddText($"\n{version.GetKouEnumName()}{type.GetDescription()}");
            markdown.AddText(processDesc);
            sb.Append(processDesc);
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
            if (this.UserConfig().UseHtml == true)
            {
                var matchedCharts = new List<dynamic>();
                foreach (var chart in relativeSongs)
                {
                    var color = SongChart.RatingColor.Master;
                    var rating = chart.GetChartRatingOfSpecificColor(color);
                    var constant = chart.GetChartConstantOfSpecificColor(color);
                    if (rating.IsNullOrWhiteSpace()) continue;
                    var status = chart.GetChartStatus(color);
                    if (status == null)
                    {
                        continue;
                    }
                    var record = relativeRecords.FirstOrDefault(p =>
                        p.CorrespondingChart.Equals(chart) && p.RatingColor == color);
                    var achievement = record?.Achievements;
                    var isDiscarded = record != null && JudgeComplete(record);
                    matchedCharts.Add(new
                    {
                        Color = SongChart.GetCssColorClass(color),
                        Constant = constant,
                        DenseDegree = GetDenseCss(status?.DifficultTag.ToInt()),
                        FitConstant = status?.FitConstant ?? constant,
                        ImageUrl = StaticServices.BrowserService?.ResolveFileUrl(chart.BasicInfo.JacketUrl,
                            new SongChart()),
                        Achievement = achievement?.ToString("F4"),
                        IsDx = chart.SongChartType == SongChart.ChartType.DX,
                        Id = chart.ChartId,
                        IsDiscarded = isDiscarded,
                    });
                }

                var containers = matchedCharts.Where(p => p.Constant != null && p.Constant != 0)
                    .OrderByDescending(p => p.Constant).ThenByDescending(p => p.FitConstant).GroupBy(p => p.Constant)
                    .Select(tuples => (tuples.Key?.ToString() ?? "未知", tuples.ToList()))
                    .Select(dummy => new { Constant = dummy.Item1, Items = dummy.Item2 }).ToList();
                markdown.AddText($"\n刷新时间：{config.LastGetRecordsTime}");
                return new HtmlMessage(new { Header = $"紫谱{plateFullName}进度", AppendInfo= markdown.MarkdownContent.ToString(), Containers = containers },
                    new KouTemplate(TemplateResources.SongListCardTemplate))
                { DpiRank = 2 };


            }

            return sb.ToString();
        }

        return $"不知道{plateType}是哪种牌子，可选的有：{PlateType.将.GetAllValues().StringJoin(',')}";

        bool JudgeComplete(SongRecord p)
        {
            switch (type)
            {
                case PlateType.極:
                    return p.FcStatus != null;
                case PlateType.将:
                    return p.Achievements >= 100;
                case PlateType.神:
                    return p.FcStatus >= SongRecord.FcType.Ap;
                case PlateType.舞舞:
                    return p.FsStatus >= SongRecord.FsType.Fsd;
                case PlateType.覇者:
                    return p.Achievements >= 80;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
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

    [PluginFunction(Name = "计算单曲rating", ActivateKeyword = "cal", Help = "如果不输入达成率，默认输出跳变阶段的所有rating", SupportedParameters = new []{nameof(UseB40Algorithm)})]
    public string CalRating([PluginArgument(Name = "定数/歌曲名")] string constantOrName,
        [PluginArgument(Name = "达成率", Min = 0, Max = 101)]
        double? rate = null)
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

            var rating = DxCalculator.CalSongRating(rate.Value, constant, !UseB40Algorithm);
            return $"{songFormat}定数{constant:F1}，达成率{rate:P4}时，Rating为{rating}";
        }

        var stringBuilder = new StringBuilder();
        stringBuilder.Append($"{songFormat}定数{constant}时：\n");
        for (var i = _rateSeq.Length - 1; i >= 0; i--)
        {
            var r = _rateSeq[i];
            var rating = DxCalculator.CalSongRating(r, constant, !UseB40Algorithm);
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

        var list = SongChart.Find(p =>
            p.BasicInfo.Aliases.Any(a => a.Alias.Equals(aliasOrName, StringComparison.OrdinalIgnoreCase)));
        if (list.Count == 1) return list[0];
        list = SongChart.Find(p => p.OfficialId.ToString() == aliasOrName || p.ChartId.ToString() == aliasOrName ||
                                   p.BasicInfo.Aliases.Any(a =>
                                       a.Alias.Contains(aliasOrName, StringComparison.OrdinalIgnoreCase)) ||
                                   p.BasicInfo.SongTitle.Contains(aliasOrName, StringComparison.OrdinalIgnoreCase) ||
                                   p.BasicInfo.SongTitleKaNa.Contains(aliasOrName, StringComparison.OrdinalIgnoreCase));
        if (list.Count == 0 && aliasOrName.Length > 3)
        {
            var similarityList = SongChart.GetCache()!
                .OrderByDescending(p => Fuzz.PartialRatio(p.BasicInfo.SongTitle, aliasOrName))
                .Take(5).ToList();
            if (similarityList.Count == 0) return null;
            using (SessionService)
            {
                var id = SessionService.Ask<int>(
                    $"为你找到以下可能的歌[相似度依次为{similarityList.Select(p => $"{Fuzz.PartialRatio(p.BasicInfo.SongTitle, aliasOrName) / 100.0:P}").StringJoin(",")}]，输入id：\n{similarityList.ToKouSetStringWithID(5)}");
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
            return
                $"可是我之前就知道{haveTheAlias.CorrespondingSong.SongTitle}可以叫做{songAnotherName}(ID{haveTheAlias.AliasID})了";

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
            }

            ;
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
        return list.OrderBy(p => p.PeopleCount?.Sum(r => r.AlterCount) ?? 1000).ToAutoPageSetString(
            $"当前{config.MapDefaultArea}地区排卡情况",
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
        string defaultArea = null;
        if (CurGroup != null)
        {
            var config = this.GroupConfig()!;
            defaultArea = config.MapDefaultArea;
        }

        ArcadeMap map = null;
        if (!name.IsInt(out var id))
        {
            var list = ArcadeMap.Find(p =>
                (defaultArea == null || defaultArea != null && p.Address.Contains(defaultArea))
                && (p.Aliases != null &&
                    p.Aliases.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase))
                    || p.ArcadeName.Contains(name)));

            switch (list.Count)
            {
                case 0:
                    reply = $"{defaultArea}没有收录{name}该机厅呢";
                    return false;
                case > 1:
                    map = list.ElementAtOrDefault(
                        SessionService.Ask<int>($"是下面第几个？输入id：{list.ToKouSetStringWithID(5)}") - 1);
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
    public object AddAreaAlias([PluginArgument(Name = "机厅名")] string name,
        [PluginArgument(Name = "别名")] List<string> aliases)
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
}