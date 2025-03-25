﻿using System;
using Koubot.SDK.Models.Entities;
using Koubot.Shared.Models;
using Koubot.Tool.Extensions;
using System.Collections.Generic;
using System.Linq;
using Koubot.Tool.General;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;

namespace KouGamePlugin.Maimai.Models;



public class DivingFishBest40ResponseDto
{
    public int additional_rating { get; set; }
    public Charts charts { get; set; }
    public string nickname { get; set; }
    public string plate { get; set; }
    public int rating { get; set; }
    public string user_data { get; set; }
    public string user_id { get; set; }
    public string username { get; set; }

    public void SaveToDb(UserAccount user)
    {
        if (charts == null) return;
        charts.dx ??= new List<DivingFishRecord>();
        charts.sd ??= new List<DivingFishRecord>();
        var list = new List<DivingFishRecord>(charts.dx);
        list.AddRange(charts.sd);
        using var context = new KouContext();
        foreach (var chart in list)
        {
            var record = new SongRecord
            {
                Achievements = chart.achievements,
                CorrespondingChart = SongChart.SingleOrDefault(p => p.OfficialId == chart.song_id, context),
                RatingColor = (SongChart.RatingColor)chart.level_index,
                FcStatus = chart.fc.ToEnum<SongRecord.FcType>(),
                FsStatus = chart.fs.ToEnum<SongRecord.FsType>(),
                User = user.FindThis(context),
                DxScore = chart.dxScore,
            };
            if (SongRecord.SingleOrDefault(p => p.Equals(record)) is { } recordInDb)
            {
                record.Id = recordInDb.Id;
            }
            SongRecord.Update(record, out _, context);
        }
    }
    public class Charts
    {
        public List<DivingFishRecord> dx { get; set; }
        public List<DivingFishRecord> sd { get; set; }
    }
}

public class DivingFishChartStatusDto
{
    public Dictionary<string, List<ChartStatus>> charts { get; set; }

    public class ChartStatus
    {
        public double cnt { get; set; }
        /// <summary>
        /// 拟合定数
        /// </summary>
        public double fit_diff { get; set; }
        public double avg { get; set; }
        public double avg_dx { get; set; }
        /// <summary>
        /// 明面难度
        /// </summary>
        public string diff { get; set; }
        public List<int> dist { get; set; }
        //public int sssp_count { get; set; }
        //public string tag { get; set; }
        //public int v { get; set; }
        //public int t { get; set; }

        public SongChart.ChartStatus ToDbChartStatus()
        {
            return new SongChart.ChartStatus
            {
                //DifficultTag = tag.IsNullOrEmpty() ? SongChart.ChartStatus.Tag.None : tag.ToKouEnum<SongChart.ChartStatus.Tag>(),
                AverageRate = avg,
                //SSSRankOfSameDifficult = v,
                //SameDifficultCount = t,
                ChartRating = diff,
                SSSCount = dist?.TakeLast(2).Sum() ?? 0,
                TotalCount = (int)cnt,
                AverageDxScore = avg_dx,
                FitConstant = fit_diff,
            };
        }
    }

    public int SaveToDb()
    {
        if (charts == null) return -1;
        using var context = new KouContext();
        var chartsInDb = context.Set<SongChart>().ToList();
        foreach (var (id, statusList) in charts)
        {
            if (!id.IsInt(out var idInt)) continue;
            try
            {
                var chart = chartsInDb.SingleOrDefault(p => p.OfficialId == idInt);
                if (chart == null)
                {
                    statusList.PrintLn($"No data:{idInt}");
                    continue;
                }
                chart.ChartStatusList = statusList.Select(p => p.ToDbChartStatus()).Where(p => p != null).ToList();
            }
            catch (Exception e)
            {
                idInt.PrintLn("存在错误：" + e.Message);
                continue;
            }
            //context.Set<SongChart>().Update(chart);
        }

        var sameDiffChartStatus = chartsInDb.Where(p => p.ChartStatusList != null).SelectMany(p => p.ChartStatusList).OrderByDescending(p => p.SSSPeopleRatio).GroupBy(p => p.ChartRating).ToList();
        foreach (var (id, statusList) in charts)
        {
            if (!id.IsInt(out var idInt)) continue;
            var chart = chartsInDb.SingleOrDefault(p => p.OfficialId == idInt);
            if (chart?.ChartStatusList == null)
            {
                continue;
            }

            foreach (var status in chart.ChartStatusList)
            {
                var difficult = status.ChartRating;
                status.SameDifficultCount = sameDiffChartStatus.SingleOrDefault(p => p.Key == difficult)?.Count() ?? 0;
                status.SSSRankOfSameDifficult = sameDiffChartStatus.SingleOrDefault(p => p.Key == difficult)?.ToList().IndexOf(status) + 1 ?? 0;
                status.DifficultTag =
                    (SongChart.ChartStatus.Tag)
                    ((status.SSSRankOfSameDifficult / (double)status.SameDifficultCount) * 5).Ceiling() - 1;
            }
            //context.Set<SongChart>().Update(chart);
        }

        var effect = context.SaveChanges();
        SongChart.UpdateCache();
        return effect;
    }
}


public class DivingFishChartInfoDto
{
    public class ChartsItem
    {
        public List<int> notes { get; set; }

        public string charter { get; set; }
    }
    public class Basic_info
    {
        public string title { get; set; }

        public string artist { get; set; }

        public string genre { get; set; }

        public int bpm { get; set; }

        public string release_date { get; set; }

        public string @from { get; set; }

        public bool is_new { get; set; }
    }
    public class ChartInfo
    {
        public string id { get; set; }

        public string title { get; set; }

        public string type { get; set; }

        public List<double> ds { get; set; }

        public List<string> level { get; set; }

        public List<int> cids { get; set; }

        public List<ChartsItem> charts { get; set; }

        public Basic_info basic_info { get; set; }
    }

    private static Dictionary<string, string> _dict = new()
    {
        {"　"," "},
        {"３","3"}
    };


    public class Root
    {
        public List<ChartInfo> Infos { get; set; }
        public int SaveToDb()
        {
            using var context = new KouContext();
            var dbInfos = context.Set<SongInfo>().Include(p => p.ChartInfo).ToList();
            var hasUpdatedDict = new Dictionary<int, bool>();
            foreach (var item in Infos)
            {
                try
                {
                    var title = item.title.ReplaceBasedOnDict(_dict);
                    var dbInfo =
                        dbInfos.SingleOrDefault(p => p.SongTitle.Equals(title, StringComparison.OrdinalIgnoreCase));
                    if (dbInfo == null)
                    {
                        KouLog.QuickAdd($"不存在{item.ToJsonString()}相关SongInfo记录，更新失败", KouLog.LogLevel.Warning);
                        continue;
                    }

                    
                    var dbChartInfo = dbInfo.ChartInfo.FirstOrDefault(p => p.SongChartType.ToString() == item.type);
                    if (dbChartInfo == null)
                    {
                        KouLog.QuickAdd($"不存在{item.ToJsonString()}相关{item.type} Chart记录，更新失败", KouLog.LogLevel.Warning);
                        continue;
                    }
                    hasUpdatedDict.Add(dbChartInfo.ChartId, true);
                    dbChartInfo.Date = item.basic_info.release_date.IsNullOrWhiteSpace()
                        ? dbChartInfo.Date
                        : int.Parse(item.basic_info.release_date);
                    dbChartInfo.IsNew = item.basic_info.is_new;
                    dbInfo.SongBpm = item.basic_info.bpm.ToString().BeNullIfWhiteSpace() ?? dbInfo.SongBpm;
                    dbInfo.SongGenre = item.basic_info.genre.BeNullIfWhiteSpace() ?? dbInfo.SongGenre;
                    if (item.type == "DX")
                    {
                        dbInfo.DxVersion = item.basic_info.@from.ToKouEnum<SongVersion>();
                    }
                    else
                    {
                        dbInfo.Version = item.basic_info.@from.ToKouEnum<SongVersion>();
                    }
                    dbInfo.SongArtist = item.basic_info.artist.BeNullIfWhiteSpace() ?? dbInfo.SongArtist;
                    dbChartInfo.OfficialId = int.Parse(item.id);
                    for (var i = 0; i < 5; i++)
                    {
                        var rating = item.level.ElementAtOrDefault(i);
                        var constant = item.ds.ElementAtOrDefault(i);
                        var chartInfo = item.charts.ElementAtOrDefault(i);
                        if (chartInfo != null)
                        {
                            if (dbChartInfo.ChartDataList.IsNullOrEmptySet())
                            {
                                dbChartInfo.ChartDataList = new List<SongChart.ChartData>().AddRepeatValue(null, 5);
                            }

                            if (dbChartInfo.ChartDataList.Count < 5)
                            {
                                dbChartInfo.ChartDataList.AddRepeatValue(null, 5 - dbChartInfo.ChartDataList.Count);
                            }

                            dbChartInfo.ChartDataList[i] ??= new SongChart.ChartData();
                            dbChartInfo.ChartDataList[i].Charter = chartInfo.charter;
                            dbChartInfo.ChartDataList[i].Notes = chartInfo.notes;
                        }

                        switch (i)
                        {
                            case 0:
                                dbChartInfo.ChartBasicConstant = constant != 0 ? constant : dbChartInfo.ChartBasicConstant;
                                dbChartInfo.ChartBasicRating = rating.BeNullIfWhiteSpace() ?? dbChartInfo.ChartBasicRating;
                                break;
                            case 1:
                                dbChartInfo.ChartAdvancedConstant = constant != 0 ? constant : null;
                                dbChartInfo.ChartAdvancedRating = rating.BeNullIfWhiteSpace() ?? dbChartInfo.ChartAdvancedRating;
                                break;
                            case 2:
                                dbChartInfo.ChartExpertConstant = constant != 0 ? constant : null;
                                dbChartInfo.ChartExpertRating = rating.BeNullIfWhiteSpace() ?? dbChartInfo.ChartExpertRating;
                                break;
                            case 3:
                                dbChartInfo.ChartMasterConstant = constant != 0 ? constant : null;
                                dbChartInfo.ChartMasterRating = rating.BeNullIfWhiteSpace() ?? dbChartInfo.ChartMasterRating;
                                break;
                            case 4:
                                dbChartInfo.ChartRemasterConstant = constant != 0 ? constant : null;
                                dbChartInfo.ChartRemasterRating = rating.BeNullIfWhiteSpace() ?? dbChartInfo.ChartRemasterRating;
                                break;
                        }
                    }

                    if (!dbChartInfo.ChartDataList.IsNullOrEmptySet())
                    {
                        var tmp = new List<SongChart.ChartData>();
                        tmp.AddRange(dbChartInfo.ChartDataList);
                        dbChartInfo.ChartDataList = tmp;
                    }
                    context.Update(dbInfo);
                }
                catch (Exception e)
                {
                    var log = $"更新ID{item.id}.{item.title}时出错：{e.Message}";
                    log.PrintLn();
                    KouLog.QuickAdd(log);
                }
            }

            foreach (var chart in dbInfos.SelectMany(p=>p.ChartInfo))
            {
                if(hasUpdatedDict.ContainsKey(chart.ChartId)) continue;
                if (chart.OfficialId == null) continue;
                chart.OfficialId = 0;
            }
            
            var row = context.SaveChanges();
            KouLog.QuickAdd($"更新完maimai chartInfo，{row}行影响到");
            return row;
        }
    }
}



public class DivingFishRecordResponseDto
{
    public int additional_rating { get; set; }
    public List<DivingFishRecord> records { get; set; }
    public string username { get; set; }

    public IReadOnlyList<SongRecord>? SaveToDb(UserAccount user)
    {
        if (records == null) return null;
        using var context = new KouContext();
        var oldRecords = context.Set<SongRecord>().AsNoTracking().Include(p => p.CorrespondingChart).ThenInclude(p=>p.BasicInfo)
            .Include(p => p.User).Where(p => p.User == user).ToList();
        var charts = context.Set<SongChart>().ToList();
        foreach (var chart in records)
        {
            var chartInDb = charts.SingleOrDefault(p => p.OfficialId == chart.song_id);
            if (chartInDb == null)
            {
                var log = $"当前曲库已过期，请更新新Chart：未找到{chart.ToJsonString()}";
                Console.WriteLine(log);
                KouLog.QuickAdd(log, KouLog.LogLevel.Warning);
                continue;
            }
            var record = new SongRecord
            {
                Achievements = chart.achievements,
                CorrespondingChart = chartInDb,
                RatingColor = (SongChart.RatingColor)chart.level_index,
                FcStatus = chart.fc.ToEnum<SongRecord.FcType>(),
                FsStatus = chart.fs.ToEnum<SongRecord.FsType>(),
                User = user.FindThis(context),
                DxScore = chart.dxScore,
            };

            var oldRecord = oldRecords.SingleOrDefault(p => p.CorrespondingChart.OfficialId == chart.song_id && p.RatingColor == (SongChart.RatingColor)chart.level_index);
            if (oldRecord != null)
            {
                record.Id = oldRecord.Id;
            }
            context.Set<SongRecord>().Update(record);

            //if (SongRecord.SingleOrDefault(p => p.Equals(record)) is { } recordInDb)
            //{
            //    record.Id = recordInDb.Id;
            //    recordInDb.CloneParameters(record, nameof(SongRecord.User), nameof(SongRecord.CorrespondingChart),
            //        nameof(SongRecord.SongTitle));
            //}
            //else
            //{
            //    var dbChart = context.Set<SongChart>().Include(p => p.BasicInfo)
            //        .ThenInclude(p => p.Aliases).SingleOrDefault(p => p.OfficialId == chart.song_id);
            //    if (dbChart == null)
            //    {
            //        var log = $"当前曲库已过期，请更新新Chart：未找到{chart.ToJsonString()}";
            //        Console.WriteLine(log);
            //        KouLog.QuickAdd(log, KouLog.LogLevel.Warning);
            //        continue;
            //    }
            //    record.CorrespondingChart = dbChart;
            //    SongRecord.GetAutoModelCache()?.Add(record);
            //}

            //context.Set<SongRecord>().Update(record);
        }

        context.SaveChanges();
        //SongRecord.UpdateCache();
        return oldRecords;
    }
}
public class DivingFishRecord
{
    public double achievements { get; set; }
    public double ds { get; set; }
    public int dxScore { get; set; }
    public string fc { get; set; }
    public string fs { get; set; }
    public string level { get; set; }
    public int level_index { get; set; }
    public string level_label { get; set; }
    public int ra { get; set; }
    public string rate { get; set; }
    public int song_id { get; set; }
    public string title { get; set; }
    public string type { get; set; }
}

public class DetailProfile
{
    public int additional_rating { get; set; }
    //public Charts charts { get; set; }
    public string nickname { get; set; }
    public string plate { get; set; }
    public int rating { get; set; }
    public UserData? user_data { get; set; }
    public string username { get; set; }

    public void FillInfo(MaiUserConfig config)
    {
        config.Nickname = nickname;
        config.Plate = plate;
        config.AdditionalRating = additional_rating;
        config.OfficialRating = rating;
        if (user_data != null)
        {
            config.Nickname = user_data.userName;
            config.PlayCount = user_data.playCount;
            config.FirstPlayDate = DateTime.Parse(user_data.firstPlayDate);
            config.LastPlayDate = DateTime.Parse(user_data.lastPlayDate);
            config.LastRegionId = user_data.lastRegionId;
            config.LastRegionName = user_data.lastRegionName;
            config.FirstGameId = user_data.firstGameId;
        }
    }
}

public class UserData
{
    public string firstGameId { get; set; }
    public string firstPlayDate { get; set; }
    public string lastPlayDate { get; set; }
    public int lastRegionId { get; set; }
    public string lastRegionName { get; set; }
    public int playCount { get; set; }
    /// <summary>
    /// DX用户名
    /// </summary>
    public string userName { get; set; }
}