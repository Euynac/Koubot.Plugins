using Koubot.SDK.AutoModel;
using Koubot.SDK.System;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;


namespace KouGamePlugin.Maimai.Models
{
    [KouAutoModelTable]
    public partial class SongInfo : KouFullAutoModel<SongInfo>
    {
        public override int GetHashCode() => HashCode.Combine(SongId, SongTitleKaNa);
        
        public override bool Equals(object? obj)
        {
            if (obj is SongInfo song)
            {
                return this.TryEqual(SongId, song.SongId)
                       || SongTitleKaNa == song.SongTitleKaNa;
            }

            return false;
        }
        public override Action<EntityTypeBuilder<SongInfo>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.SongId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.SongId);

                entity.Property(p => p.Version)
                    .HasConversion(v => v.GetKouEnumName(1) ?? v.ToString(),
                    s => s.ToKouEnum<SongVersion>());

                entity.HasIndex(e => e.SongTitle);

                entity.HasMany(p => p.Aliases)
                    .WithOne(p => p.CorrespondingSong)
                    .HasForeignKey(p => p.SongKanaId)
                    .HasPrincipalKey(p => p.SongTitleKaNa);
            };
        }

        public override int? GetItemID() => SongId;

        public override string? ToString(FormatType formatType, object? supplement = null, KouCommand? command = null)
        {
            if (supplement is not SongChart chartInfo)
            {
                return formatType switch
                {
                    FormatType.Brief => $"{SongTitle}",
                    FormatType.Detail => $"{SongId}.{SongTitle}",
                    _ => throw new NotImplementedException(),
                };
            }

            var id = chartInfo.ChartId;
            var songType = chartInfo.SongChartType;
            var constantData = chartInfo.ToConstantString();
            switch (formatType)
            {
                case FormatType.Brief:
                    return
                        $"{SongTitle}({songType}) {(constantData is null ? $"*[{chartInfo.ToRatingString()}]" : $"[{constantData}]")}";
                case FormatType.Detail:
                    string difficultData = $"{chartInfo.ToRatingString()?.Be("\n难度：{0}", true)}" +
                                           $"{constantData?.Be("\n定数：{0}", true)}";
                    return
                        $"{JacketUrl?.Be(new KouImage(JacketUrl, chartInfo).ToKouResourceString())}" + //BUG 需要解决翻页可能会使得图片资源字符串裂开的问题
                        $"{id}.{SongTitle} [{songType}]" +
                        difficultData +
                        SongGenre?.Be($"\n分类：{SongGenre}") +
                        Version?.Be($"\n版本：{Version.Value.GetKouEnumName()}") + SongArtist?.Be($"\n曲师：{SongArtist}") +
                        SongBpm?.Be($"\nBPM：{SongBpm}") + SongLength?.Be($"\n歌曲长度：{SongLength}") +
                        Remark?.Be($"\n注：{Remark}") +
                        Aliases?.ToKouSetString(FormatType.Customize1, "，", false)?.Be("\n别名：{0}", true) +
                        chartInfo.GetChartData(SongChart.SongRatingColor.Value)?.Be(
                            $"\n[{SongChart.SongRatingColor.Value.GetKouEnumName()}谱数据]\n{chartInfo.GetChartStatus(SongChart.SongRatingColor.Value)}\n{{0}}",
                            true);
                        //$"{chartInfo.ToOldRatingString()?.Be("\n旧难度：{0}", true)}" +
                //$"{chartInfo.ToOldConstantString()?.Be("\n旧定数：{0}", true)}";
                default:
                    return null;
            }
        }
    }
}
