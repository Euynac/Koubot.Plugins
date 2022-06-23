using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Models;
using Koubot.Tool.String;

namespace KouFunctionPlugin;
[Table("plugin_quotation_list")]
public partial class Quotation
{
    public int ID { get; set; }
    public string Content { get; set; }
    public QuotationType Type { get; set; }
    [Flags]
    public enum QuotationType
    {
        None,
        [KouEnumName("情话")]
        Love =1 << 0,
        [KouEnumName("冷笑话")]
        ColdJoke = 1 << 1,
        [KouEnumName("有大病")]
        Stupid = 1 << 2,
        [KouEnumName("哲理")]
        Philosophy = 1 << 3,
        [KouEnumName("鼓励")]
        Encourage = 1 << 4,
        [KouEnumName("赞美")]
        Praise = 1 << 5,
    }
    public virtual UserAccount? Contributor { get; set; }
}