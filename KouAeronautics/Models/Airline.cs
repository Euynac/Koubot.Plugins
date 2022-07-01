using Koubot.Shared.Protocol.Attribute;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouFunctionPlugin.Aeronautics;


[Table("plugin_aeronautics_airline")]
public partial class Airline
{
    [Key]
    public int CompanyID { get; set; }
    [KouAutoModelField]
    public string Code3 { get; set; }
    [KouAutoModelField]
    public string? Code2 { get; set; }
    [KouAutoModelField(ActivateKeyword = "公司")]
    public string? CompanyName { get; set; }
    [KouAutoModelField]
    public string? EnglishName { get; set; }
    [KouAutoModelField]
    public List<string>? Surname { get; set; }
    [KouAutoModelField]
    public string? Country { get; set; }
    [Column("DomesticFlag")]
    public bool IsDomestic { get; set; }
    [KouAutoModelField(ActivateKeyword = "sita")]
    public string? SITAAddress { get; set; }
    [KouAutoModelField(ActivateKeyword = "aftn")]
    public string? AFTNAddress { get; set; }
    public bool ServiceFlag { get; set; }
    public bool ProxyFlag { get; set; }

}