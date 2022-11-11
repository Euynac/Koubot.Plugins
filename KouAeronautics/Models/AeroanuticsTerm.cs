using Koubot.Shared.Models;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouFunctionPlugin.Aeronautics;


[Table("plugin_aeronautics_term")]
public partial class AeronauticsTerm
{
    [Key]
    public int ID { get; set; }
    [AutoField(ActivateKeyword = "acronyms|缩写|abbr", CandidateKey = MultiCandidateKey.FirstCandidateKey)]
    public string? Abbreviation { get; set; }
    [AutoField(ActivateKeyword = "全称", CandidateKey = MultiCandidateKey.SecondCandidateKey)]
    public string? FullName { get; set; }
    [AutoField(ActivateKeyword = "名称", CandidateKey = MultiCandidateKey.ThirdCandidateKey)]
    public string? Title { get; set; }
    [AutoField(ActivateKeyword = "备注|解释|remark")]
    public string? Remark { get; set; }
    [AutoField(ActivateKeyword = "来源")]
    public string? Source { get; set; }
    [AutoField(ActivateKeyword = "更新时间")]
    public DateTime? UpdateTime { get; set; }
    public virtual UserAccount? Contributor { get; set; }
}