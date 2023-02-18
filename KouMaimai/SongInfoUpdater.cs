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

namespace KouMaimai;

public class SongInfoUpdater : IKouError
{
    private Dictionary<string, SongInfo> _originDictionary;
    public Dictionary<Root, List<SongInfo>> _similarDict { get; set; } = new();
    public List<SongInfo> UpdatedList { get; set; }
    public List<SongInfo> AddedList { get; set; }
    public bool StartUpdate()
    {
        var res = KouHttp.Create("https://maimai.sega.jp/data/maimai_songs.json").SendRequest(HttpMethods.GET);
        if (res.HasError)
        {
            return this.ReturnFalseWithError(res.Body);
        }

        var list = JsonSerializer.Deserialize<List<Root>>(res.Body);
        if (list.IsNullOrEmptySet()) return this.ReturnNullWithError("获取到0条歌曲记录");
        using var context = new KouContext();
        _originDictionary = context.Set<SongInfo>().ToDictionary(p => p.SongTitleKaNa, p => p);
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

                AddedList.Add(new SongInfo()
                {
                    SongTitle = song.title,
                    SongTitleKaNa = song.title_kana,
                    SongArtist = song.artist,
                    SongGenre = song.catcode,
                    JacketUrl = $"https://maimaidx.jp/maimai-mobile/img/Music/{song.image_url}",
                    ChartInfo = new List<SongChart>()
                    {
                        new()
                        {
                            SongChartType = song.IsDx ? SongChart.ChartType.DX : SongChart.ChartType.SD,
                            SongTitleKaNa = song.title_kana,
                            ChartBasicRating = song.IsDx ? song.dx_lev_bas : song.lev_bas,
                            ChartAdvancedRating = song.IsDx ? song.dx_lev_adv: song.lev_adv,
                            ChartExpertRating =  song.IsDx ? song.dx_lev_exp: song.lev_exp,
                            ChartMasterRating =  song.IsDx ? song.dx_lev_mas:song.lev_mas,
                            ChartRemasterRating = song.IsDx ? song.dx_lev_remas: song.lev_remas
                        }
                    }
                });
            }
            else
            {
                if(NeedUpdate(song, originSong)) UpdatedList.Add(originSong);
            }
        }

        return true;
    }

    public int SaveToDb()
    {
        using var context = new KouContext();
        context.Set<SongInfo>().AddRange(AddedList);
        return context.SaveChanges();
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

        if (origin.ChartInfo.IsNullOrEmptySet())
        {
            origin.ChartInfo = new List<SongChart>()
            {
                new()
                {
                    SongChartType = data.IsDx ? SongChart.ChartType.DX : SongChart.ChartType.SD,
                    SongTitleKaNa = data.title_kana,
                    ChartBasicRating = data.IsDx ? data.dx_lev_bas : data.lev_bas,
                    ChartAdvancedRating = data.IsDx ? data.dx_lev_adv: data.lev_adv,
                    ChartExpertRating =  data.IsDx ? data.dx_lev_exp: data.lev_exp,
                    ChartMasterRating =  data.IsDx ? data.dx_lev_mas:data.lev_mas,
                    ChartRemasterRating = data.IsDx ? data.dx_lev_remas: data.lev_remas
                }
            };
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
        public bool IsDx => (dx_lev_bas != null || dx_lev_adv != null )&& lev_bas == null && lev_mas == null;
    }

    public string ErrorMsg { get; set; }
    public ErrorCodes ErrorCode { get; set; }
}