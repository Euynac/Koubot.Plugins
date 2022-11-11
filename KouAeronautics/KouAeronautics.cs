using Koubot.SDK.PluginInterface;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using KouFunctionPlugin.Aeronautics;
using KouFunctionPlugin.Configs;

namespace KouFunctionPlugin
{
    [PluginClass("aero", "航空",
        PluginType = PluginType.Function)]
    public class KouAeronautics : KouPlugin<KouAeronautics>, IWantPluginGroupConfig<AeronauticsGroupConfig>
    {
        [PluginParameter(ActivateKeyword = "f", Name = "增加时相同条目也继续增加")]
        public bool? AddWhenSame { get; set; }

        [PluginParameter(ActivateKeyword = "source", Name = "来源")]
        public string? Source { get; set; }

        [PluginFunction(ActivateKeyword = "add vpn", Name = "增加VPN资源")]
        public object? AddVpn([PluginArgument(Name = "资源名")] List<string> sourceNames)
        {
            var config = this.GroupConfig()!;
            config.VpnList.AddRange(sourceNames);
            config.VpnList = config.VpnList.Distinct().ToList();
            if (!config.SaveChanges()) return "添加失败";
            return $"添加成功！当前群可用的VPN：{config.VpnList.StringJoin('、')}";
        }
        [PluginFunction(ActivateKeyword = "status vpn", Name = "VPN空闲状态")]
        public object? VpnStatus()
        {
            var config = this.GroupConfig()!;
            return config.GetAllSourceStatus();
        }

        [PluginFunction(ActivateKeyword = "apply vpn", Name = "申请使用VPN")]
        public object? ApplyVpn([PluginArgument(Name = "指定资源名")] string? specificName = null)
        {
            var config = this.GroupConfig()!;
            if (config.UseStatus.Values.Contains(CurKouUser))
            {
                return $"现在正在使用{config.UseStatus.First(p=>p.Value == CurKouUser).Key}VPN";
            }

            if (specificName != null && config.UseStatus.TryGetValue(specificName, out var userUsed)) return $"{specificName}VPN已被{userUsed.Nickname}占用";
            var used = config.UseStatus.Keys.ToHashSet();
            var rest = config.VpnList.Where(p => !used.Contains(p)).ToList();
            if (rest.IsNullOrEmptySet()) return $"已经没有可供分配的VPN，再等等吧\n{config.GetAllSourceStatus()}";
            var resource = specificName ?? rest.RandomGetOne();
            if (resource == null) return "分配失败";
            config.UseStatus.Add(resource, CurKouUser);

            return config.SaveChanges() ? $"已分配使用{resource}VPN，还余{rest.Count-1}可用" : "状态保存失败";
        }

        [PluginFunction(ActivateKeyword = "end vpn", Name = "结束使用VPN")]
        public object? EndUseVpn()
        {
            var config = this.GroupConfig()!;
            if (!config.UseStatus.Values.Contains(CurKouUser))
            {
                return "现在没有正在使用的VPN";
            }

            var used = config.UseStatus.First(p => p.Value == CurKouUser).Key;
            config.UseStatus.Remove(used);
            return !config.SaveChanges() ? "状态变更失败" : $"已结束使用{used}VPN，还余{config.VpnList.Count - config.UseStatus.Count}可用";
        }


        [PluginFunction(ActivateKeyword = "add term", Name = "增加术语")]
        public object? AddTerm([PluginArgument(Name = "缩写")] string? abbr,
            [PluginArgument(Name = "全称")] string? fullName,
            [PluginArgument(Name = "标题")] string? title,
            [PluginArgument(Name = "备注")] string? remark = null)
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