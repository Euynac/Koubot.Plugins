using Koubot.SDK.Models.Entities;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using KouFunctionPlugin.Aeronautics;

namespace KouFunctionPlugin
{
    [KouPluginClass("aero", "航空",
        PluginType = PluginType.Function)]
    public class KouAeronautics : KouPlugin<KouAeronautics>
    {
        [KouPluginParameter(ActivateKeyword = "f", Name = "增加时相同条目也继续增加")]
        public bool? AddWhenSame { get; set; }

        [KouPluginParameter(ActivateKeyword = "source", Name = "来源")]
        public string? Source { get; set; }
        [KouPluginFunction(ActivateKeyword = "add term", Name = "增加术语")]
        public object? AddTerm([KouPluginArgument(Name = "缩写")] string? abbr,
            [KouPluginArgument(Name = "全称")] string? fullName,
            [KouPluginArgument(Name = "标题")] string? title,
            [KouPluginArgument(Name = "备注")] string? remark = null)
        {
            if (abbr == null && fullName == null && remark == null) return "缩写、全称、标题至少需要一项值";
            var repeat = AeronauticsTerm.DbFind(p => (fullName != null && p.FullName == fullName) || (title != null && p.Title == title) || (abbr != null && p.Abbreviation == abbr)).ToList();
            if (repeat.Count > 0)
            {
                if (AddWhenSame == null)
                {
                    return $"可能存在重复项：\n{repeat.ToKouSetString(oneDetailFormatType: FormatType.Brief, useItemID:true)}";
                }
            }

            var added = new AeronauticsTerm()
            {
                Abbreviation = abbr,
                FullName = fullName,
                Title = title,
                Remark = remark,
                Source = Source
            };
            if (AeronauticsTerm.Add(added, out var error, Context))
            {
                return $"添加了ID{added.ID}.{added.ToString(FormatType.Detail)}";
            }

            return $"添加失败：{error.ErrorMsg}";
        }
    }
}