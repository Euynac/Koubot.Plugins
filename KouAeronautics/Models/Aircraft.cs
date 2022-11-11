using Koubot.Shared.Protocol.Attribute;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouFunctionPlugin.Aeronautics;


[Table("plugin_aeronautics_aircraft")]
public partial class Aircraft
{
    public enum WakeTurbulanceType
    {
        L,
        M,
        H,
        J
    }
    [Key]
    public int AircraftID { get; set; }
    [AutoField]
    public string AircraftType { get; set; }
    public string? AircraftClass { get; set; }
    [AutoField]
    public string? Manufacturer { get; set; }
    public WakeTurbulanceType? WakeTurbulance { get; set; }
    [AutoField]
    public List<string>? Surname { get; set; }
    [AutoField]
    public int FueledWeight { get; set; }
    [AutoField]
    public int FuelCapacity { get; set; }
    [AutoField]
    public int Range { get; set; }
    [AutoField]
    public int PassengerSize { get; set; }
    [Column("CruiseAltd")]
    [AutoField]
    public int CruiseAltitude { get; set; }
    [AutoField]
    public int CruiseSpeed { get; set; }
    [AutoField]
    public int MinSpeed { get; set; }
    [AutoField]
    public int MaxSpeed { get; set; }
    [AutoField]
    public string? ICAOCode { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }

}