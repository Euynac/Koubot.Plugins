using System.ComponentModel.DataAnnotations.Schema;
using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KouFunctionPlugin.Cook;

[Table("plugin_cook_food")]
public partial class Food
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string ImageUrl { get; set; }
    public virtual UserAccount? Contributor { get; set; }
    public StyleOfFood? Style { get; set; }
    public KindOfFood? Kind { get; set; }
    public enum KindOfFood
    {
        Appetizer,
        Soup,
        MainCourse,
        SideDish,
        Dessert
    }
    public enum StyleOfFood
    {
        徽,
        湘,
        浙江,
        闽,
        江苏,
        粤,
        川,
        鲁,
        日本,
        韩国,
        西餐
    }
}