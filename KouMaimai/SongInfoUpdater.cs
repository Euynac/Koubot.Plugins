using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Castle.Core.Internal;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Algorithm;
using Koubot.Tool.Extensions;
using Koubot.Tool.Web;
using KouGamePlugin.Maimai.Models;
using Microsoft.EntityFrameworkCore;

namespace KouMaimai;

public class SongInfoUpdater : IKouError
{
    private Dictionary<string, SongInfo> _originDictionary;
    public Dictionary<Root, List<SongInfo>> _similarDict { get; set; } = new();
    public List<SongInfo> UpdatedList { get; set; }
    public List<SongInfo> AddedList { get; set; }

    private SongChart FillDxChartData(SongChart chart, Root song)
    {
        chart.SongChartType = SongChart.ChartType.DX;
        chart.SongTitleKaNa = song.title_kana;
        chart.ChartBasicRating = song.dx_lev_bas;
        chart.ChartAdvancedRating = song.dx_lev_adv;
        chart.ChartExpertRating = song.dx_lev_exp;
        chart.ChartMasterRating = song.dx_lev_mas;
        chart.ChartRemasterRating = song.dx_lev_remas;
        return chart;
    }

    private SongChart FillSdChartData(SongChart chart, Root song)
    {
        chart.SongChartType = SongChart.ChartType.SD;
        chart.SongTitleKaNa = song.title_kana;
        chart.ChartBasicRating = song.lev_bas;
        chart.ChartAdvancedRating = song.lev_adv;
        chart.ChartExpertRating = song.lev_exp;
        chart.ChartMasterRating = song.lev_mas;
        chart.ChartRemasterRating = song.lev_remas;
        return chart;
    }


    public bool StartUpdate(out int changedRow)
    {
        changedRow = 0;
        var res = KouHttp.Create("https://maimai.sega.jp/data/maimai_songs.json").SendRequest(HttpMethods.GET);
        if (res.HasError)
        {
            return this.ReturnFalseWithError("请求出错："+res.ErrorMsg);
        }

        var list = JsonSerializer.Deserialize<List<Root>>(res.Body);
        if (list.IsNullOrEmptySet()) return this.ReturnNullWithError("获取到0条歌曲记录");
        using var context = new KouContext();
        _originDictionary = context.Set<SongInfo>().Include(p=>p.ChartInfo).ToDictionary(p => p.SongTitleKaNa, p => p);
        UpdatedList = new List<SongInfo>();
        AddedList = new List<SongInfo>();
        foreach (var song in list)
        {
            if (!_originDictionary.TryGetValue(song.title_kana, out var originSong))
            {
                if (FindSimilar(song))
                {
                    continue;
                }

                var newSong = AddedList.FirstOrDefault(p=>p.SongTitleKaNa == song.title_kana) ?? new SongInfo()
                {
                    SongTitle = song.title,
                    SongTitleKaNa = song.title_kana,
                    SongArtist = song.artist,
                    SongGenre = song.catcode,
                    JacketUrl = $"https://maimaidx.jp/maimai-mobile/img/Music/{song.image_url}",
                    ChartInfo = new List<SongChart>()
                };
                if (song.HasDx)
                {
                    newSong.ChartInfo.Add(FillDxChartData(new SongChart(), song));
                }

                if (song.HasSd)
                {
                    newSong.ChartInfo.Add(FillSdChartData(new SongChart(), song));
                }

                AddedList.Add(newSong);
            }
            else
            {
                if (NeedUpdate(song, originSong))
                {
                    context.Attach(originSong);
                    UpdatedList.Add(originSong);
                }
            }
        }
        context.Set<SongInfo>().AddRange(AddedList);
        context.Set<SongInfo>().UpdateRange(UpdatedList);
        changedRow = context.SaveChanges();
        return true;
    }
    private bool NeedUpdate(Root data, SongInfo origin)
    {
        var updated = false;
        if (origin.SongArtist.IsNullOrEmpty())
        {
            origin.SongArtist = data.artist;
            updated = true;
        }

        if (origin.SongGenre.IsNullOrEmpty())
        {
            origin.SongGenre = data.catcode;
            updated = true;
        }

        if (origin.JacketUrl.IsNullOrEmpty())
        {
            origin.JacketUrl = data.image_url;
            updated = true;
        }

        var shouldHaveCount = 0;
        if (data.HasDx) shouldHaveCount++;
        if (data.HasSd) shouldHaveCount++;
        origin.ChartInfo ??= new List<SongChart>();
        if (origin.ChartInfo.IsNullOrEmptySet() || origin.ChartInfo.Count != shouldHaveCount)
        {
            //origin.ChartInfo.Clear();
            origin.ChartInfo = new List<SongChart>();
            if (data.HasDx)
            {
                origin.ChartInfo.Add(FillDxChartData(new SongChart(), data));
            }

            if (data.HasSd)
            {
                origin.ChartInfo.Add(FillSdChartData(new SongChart(), data));
            }
            updated = true;
        }
        return updated;
    }
    private bool FindSimilar(Root data)
    {
        if (data.title.Length <= 2) return false;
        var candidate = _originDictionary.Values.Where(p =>
            LevenshteinDistance.Calculate(p.SongTitleKaNa, data.title_kana) <= 2 &&
            LevenshteinDistance.Calculate(p.SongTitle, data.title) <= 2 &&
            LevenshteinDistance.Calculate(p.SongArtist, data.artist) <= 3).ToList();
        if (candidate.Any())
        {
            _similarDict.Add(data, candidate);
            return true;
        }
        return false;
    }



    public class Root
    {
        public string artist { get; set; }
        public string catcode { get; set; }
        public string image_url { get; set; }
        public string release { get; set; }
        public string lev_bas { get; set; }
        public string lev_adv { get; set; }
        public string lev_exp { get; set; }
        public string lev_mas { get; set; }
        public string lev_remas { get; set; }
        public string dx_lev_bas { get; set; }
        public string dx_lev_adv { get; set; }
        public string dx_lev_exp { get; set; }
        public string dx_lev_mas { get; set; }
        public string dx_lev_remas { get; set; }
        public string sort { get; set; }
        public string title { get; set; }
        public string title_kana { get; set; }
        public string version { get; set; }
        public bool HasDx => dx_lev_bas != null || dx_lev_adv != null || dx_lev_exp != null;
        public bool HasSd => lev_bas != null || lev_adv != null || lev_exp != null;
    }

    public string ErrorMsg { get; set; }
    public ErrorCodes ErrorCode { get; set; }
}