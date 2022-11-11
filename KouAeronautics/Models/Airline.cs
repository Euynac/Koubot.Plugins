using Koubot.Shared.Protocol.Attribute;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouFunctionPlugin.Aeronautics;


[Table("plugin_aeronautics_airline")]
public partial class Airline
{
    [Key]
    public int CompanyID { get; set; }
    [AutoField]
    public string Code3 { get; set; }
    [AutoField]
    public string? Code2 { get; set; }
    [AutoField(ActivateKeyword = "公司")]
    public string? CompanyName { get; set; }
    [AutoField]
    public string? EnglishName { get; set; }
    [AutoField]
    public List<string>? Surname { get; set; }
    [AutoField]
    public string? Country { get; set; }
    [Column("DomesticFlag")]
    public bool IsDomestic { get; set; }
    [AutoField(ActivateKeyword = "sita")]
    public string? SITAAddress { get; set; }
    [AutoField(ActivateKeyword = "aftn")]
    public string? AFTNAddress { get; set; }
    public bool ServiceFlag { get; set; }
    public bool ProxyFlag { get; set; }

}