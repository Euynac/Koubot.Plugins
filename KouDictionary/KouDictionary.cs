using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin
{
    [PluginClass("dict", "词典",
        Introduction = "词典",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouDictionary : KouPlugin<KouDictionary>
    {
        [PluginFunction(Help = "现有英文字典(en)、成语字典(idiom)，使用/dict.en help查看英文表详情")]
        public override object? Default(string? str = null)
        {
            return ReturnHelp(true);
        }
    }
}
