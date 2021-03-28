using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Plugin;

namespace KouFunctionPlugin
{
    [KouPluginClass("dict", "词典",
        Introduction = "词典",
        Author = "7zou",
        PluginType = KouEnum.PluginType.Function)]
    public class KouDictionary : KouPlugin
    {
        [KouPluginFunction(Help = "现有英文字典(en)、成语字典(idiom)，使用/dict.en help查看英文表详情")]
        public override object Default(string str = null)
        {
            return ReturnHelp(true);
        }
    }
}
