using Koubot.SDK.Models.Entities;
using Koubot.Shared.Models;
using Koubot.Tool.Extensions;
using System.Collections.Generic;

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
public class DivingFishRecordResponseDto
{
    public int additional_rating { get; set; }
    public List<DivingFishRecord> records { get; set; }
    public string username { get; set; }

    public void SaveToDb(UserAccount user)
    {
        if (records == null) return;
        using var context = new KouContext();
        foreach (var chart in records)
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

            context.Set<SongRecord>().Update(record);
        }

        context.SaveChanges();
        SongRecord.UpdateCache();
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