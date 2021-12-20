using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;

namespace KouGamePlugin.Maimai.Models;

/// <summary>
/// 歌曲成绩
/// </summary>
public class SongRecord
{
    public enum FcType
    {
        Fc,
        Fcp,
        Ap,
        App
    }

    public enum FsType
    {
        Fs,
        Fsp,
        Fsd
    }

    [Key]
    [KouAutoModelField(ActivateKeyword = "id", UnsupportedActions = AutoModelActions.CannotAlter)]
    public int Id { get; set; }
    /// <summary>
    /// 成绩
    /// </summary>
    public double Achievements { get; set; }
    /// <summary>
    /// DX分数
    /// </summary>
    public int DxScore { get; set; }
    /// <summary>
    /// 难度类型
    /// </summary>
    public SongChart.RatingType RatingType { get; set; }
    /// <summary>
    /// 相关歌曲信息
    /// </summary>
    public SongInfo CorrespondingSong { get; set; }
    /// <summary>
    /// 谱面类型
    /// </summary>
    public SongChart.ChartType SongChartType { get; set; }
    /// <summary>
    /// 用户
    /// </summary>
    public virtual UserAccount User { get; set; }
    /// <summary>
    /// 多人谱面成就
    /// </summary>
    public FsType FsStatus { get; set; }
    /// <summary>
    /// 单人谱面成就
    /// </summary>
    public FcType FcStatus { get; set; }
}