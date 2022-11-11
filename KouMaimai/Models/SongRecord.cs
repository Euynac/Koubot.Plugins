using Koubot.Shared.Models;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouGamePlugin.Maimai.Models;

/// <summary>
/// 歌曲成绩
/// </summary>
[Table("plugin_maimai_song_records")]
public partial class SongRecord
{
    public enum FcType
    {
        [Description("FC")]
        Fc,
        [Description("FC+")]
        Fcp,
        [Description("AP")]
        Ap,
        [Description("AP+")]
        App
    }

    public enum FsType
    {
        /// <summary>
        /// FULL SYNC
        /// </summary>
        [Description("FS")]
        Fs,
        /// <summary>
        /// FULL SYNC PLUS
        /// </summary>
        [Description("FS+")]
        Fsp,
        /// <summary>
        /// FULL SYNC DX
        /// </summary>
        [Description("FDX")]
        Fsd,
        /// <summary>
        /// FULL SYNC DX PLUS
        /// </summary>
        [Description("FDX+")]
        Fsdp,
    }

    [Key]
    public int Id { get; set; }
    /// <summary>
    /// 成绩
    /// </summary>
    [AutoField(ActivateKeyword = "达成率")]
    public double Achievements { get; set; }
    /// <summary>
    /// DX分数
    /// </summary>
    [AutoField(ActivateKeyword = "DX分数")]
    public int DxScore { get; set; }
    /// <summary>
    /// 难度类型
    /// </summary>
    [AutoField(ActivateKeyword = "颜色")]
    [Column("RatingType")]
    public SongChart.RatingColor RatingColor { get; set; }
    /// <summary>
    /// 相关歌曲谱面信息
    /// </summary>
    [AutoField(true)]
    public virtual SongChart CorrespondingChart { get; set; }
    /// <summary>
    /// 用户
    /// </summary>
    [AutoField(Features = AutoModelFieldFeatures.AutoUseCurKouUser, ActivateKeyword = "玩家")]
    public virtual UserAccount User { get; set; }
    /// <summary>
    /// 多人谱面成就
    /// </summary>
    [AutoField(ActivateKeyword = "同步")]
    public FsType? FsStatus { get; set; }
    /// <summary>
    /// 单人谱面成就
    /// </summary>
    [AutoField(ActivateKeyword = "成就")]
    public FcType? FcStatus { get; set; }
}