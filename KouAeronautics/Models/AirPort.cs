using Koubot.Shared.Protocol.Attribute;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouFunctionPlugin.Aeronautics;


[Table("plugin_aeronautics_airport")]
public partial class AirPort
{
    [Key]
    public int ID { get; set; }
    [KouAutoModelField]
    public string Code4 { get; set; }
    [KouAutoModelField]
    public string? Code3 { get; set; }
    [KouAutoModelField]
    public string? City { get; set; }
    [KouAutoModelField]
    public string? Name { get; set; }
    [KouAutoModelField]
    public List<string>? Surname { get; set; }
    [KouAutoModelField]
    public string? Country { get; set; }
    [Column("DomesticFlag")]
    public bool IsDomestic { get; set; }
    [Column("InternationalFlag")]
    public bool IsInternational { get; set; }
    [Column("MilitaryFlag")]
    public bool IsMilitary { get; set; }
    [Column("CivilFlag")]
    public bool IsCivil { get; set; }
    [Column("RegionalFlag")]
    public bool IsRegional { get; set; }
    public bool IsManual { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public double Altitude { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    //value converters which map to multiple columns https://github.com/dotnet/efcore/issues/13947
}