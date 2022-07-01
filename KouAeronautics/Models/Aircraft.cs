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
    [KouAutoModelField]
    public string AircraftType { get; set; }
    public string? AircraftClass { get; set; }
    [KouAutoModelField]
    public string? Manufacturer { get; set; }
    public WakeTurbulanceType? WakeTurbulance { get; set; }
    [KouAutoModelField]
    public List<string>? Surname { get; set; }
    [KouAutoModelField]
    public int FueledWeight { get; set; }
    [KouAutoModelField]
    public int FuelCapacity { get; set; }
    [KouAutoModelField]
    public int Range { get; set; }
    [KouAutoModelField]
    public int PassengerSize { get; set; }
    [Column("CruiseAltd")]
    [KouAutoModelField]
    public int CruiseAltitude { get; set; }
    [KouAutoModelField]
    public int CruiseSpeed { get; set; }
    [KouAutoModelField]
    public int MinSpeed { get; set; }
    [KouAutoModelField]
    public int MaxSpeed { get; set; }
    [KouAutoModelField]
    public string? ICAOCode { get; set; }
    public DateTime? CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }

}