using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;

namespace KouGamePlugin.Maimai.Models;

[KouAutoModelTable("record", new[] { nameof(KouMaimai) })]
public partial class SongRecord : KouFullAutoModel<SongRecord>
{
    [KouAutoModelField(ActivateKeyword = "曲名")]
    [NotMapped] public string? SongTitle { get; set; }

    protected override KouMessage ReplyOnFailingToSearch() => "没有找到相应的成绩哦";

    static SongRecord()
    {
        AddCustomFunc(nameof(SongTitle), (song, o) =>
        {
            if (o is string input)
            {
                return song.CorrespondingChart.BasicInfo.SongTitle.Contains(input, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        });
    }
    public static List<SongRecord> GetB40Charts(UserAccount user)
    {
        var list = Find(p => p.User == user);
        if (list.IsNullOrEmptySet()) return new List<SongRecord>();
        var newSong = list.Where(p => p.CorrespondingChart.BasicInfo.IsNew is true).OrderByDescending(p => p.Rating).Take(15);
        var oldSong = list.Where(p => p.CorrespondingChart.BasicInfo.IsNew is false).OrderByDescending(p => p.Rating).Take(25);
        var b40Song = newSong.ToList();
        b40Song.AddRange(oldSong);
        return b40Song;
    }
    public override bool UseCustomDefaultFieldSplit(string userInput, out Dictionary<string, string> ruleDictionary, out string relationStr)
    {
        ruleDictionary = new Dictionary<string, string>();
        relationStr = $"{{{nameof(CorrespondingChart.SongAlias)}}}||{{{nameof(SongTitle)}}}";
        if (userInput.IsNullOrEmpty())
        {
            return false;
        }
        //if (userInput.MatchOnceThenReplace("^(.+?)的(.+?)",
        //        out userInput, out var groupResult3))
        //{
        //    var user = groupResult3[1].Value;
        //    ruleDictionary.Add($"{nameof(RatingColor)}", color);
        //}
        if (userInput.MatchOnceThenReplace("(白|紫|红|黄|绿)",
                out userInput, out var groupResult2))
        {
            var color = groupResult2[1].Value;
            ruleDictionary.Add($"{nameof(RatingColor)}", color);
        }
        //处理限定难度类型信息
        if (userInput.MatchOnceThenReplace("[ ](dx|sd)",
                out userInput, out var groupResult, RegexOptions.IgnoreCase | RegexOptions.RightToLeft))
        {
            var ratingClass = groupResult[1].Value;
            ruleDictionary.Add($"{nameof(CorrespondingChart.SongChartType)}", ratingClass);
        }

        ruleDictionary.Add($"{nameof(SongTitle)}", userInput);
        ruleDictionary.Add($"{nameof(CorrespondingChart)}.{nameof(CorrespondingChart.SongAlias)}", userInput);
        return true;
    }

    /// <summary>
    /// 当前成绩Rating
    /// </summary>
    [KouAutoModelField(ActivateKeyword = "rating")]
    public int Rating
    {
        get
        {
            _rating ??= CorrespondingChart.CalRating(RatingColor, Achievements) ?? 0;
            return _rating.Value;
        }
    }

    private int? _rating;


    protected override dynamic? SetModelIncludeConfig(IQueryable<SongRecord> set)
        => set.Include(p => p.User).Include(p => p.CorrespondingChart)
            .ThenInclude(p => p.BasicInfo).ThenInclude(p => p.Aliases);
    public override bool Equals(object? obj)
    {
        if (obj is SongRecord record)
        {
            return User.Equals(record.User) && CorrespondingChart.Equals(record.CorrespondingChart) &&
                   RatingColor == record.RatingColor;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return User.GetHashCodeWith(CorrespondingChart).GetHashCodeWith(RatingColor);
    }

    public override Action<EntityTypeBuilder<SongRecord>>? ModelSetup()
    {
        return builder =>
        {
            builder.HasOne(p => p.User).WithMany();
            builder.HasOne(p => p.CorrespondingChart).WithMany();
        };
    }

    public override string? ToString(FormatType formatType, object? supplement = null, KouCommand? command = null)
    {
        return formatType switch
        {
            FormatType.Brief =>
                $"{CorrespondingChart.ToSpecificRatingString(RatingColor)}({Achievements / 100.0:P4}{FcStatus?.Be($"{FcStatus.GetDescription()}")}{FsStatus?.Be($" {FsStatus.GetDescription()}")})" +
                $"——{Rating}",
            FormatType.Detail =>
                                 //$"{CorrespondingChart.BasicInfo.JacketUrl?.Be(new KouImage(CorrespondingChart.BasicInfo.JacketUrl, CorrespondingChart).ToKouResourceString())}" +
                                 $"{CorrespondingChart.ToSpecificRatingString(RatingColor)}" +
                                 $"\n成绩：{Achievements / 100.0:P4}" +
                                 $"{FcStatus?.Be($" {FcStatus.GetDescription()}")}" +
                                 $"{FsStatus?.Be($" {FsStatus.GetDescription()}")}" +
                                 $"\nDX分数:{DxScore}" +
                                 $"\nRating：{CorrespondingChart.CalRating(RatingColor, Achievements)}",

            _ => throw new ArgumentOutOfRangeException(nameof(formatType), formatType, null)
        };
    }
}