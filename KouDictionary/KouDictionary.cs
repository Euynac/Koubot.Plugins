using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Plugin;

namespace KouFunctionPlugin
{
    [KouPluginClass("dict", "词典",
        Introduction = "词典",
        Author = "7zou",
        PluginType = KouEnum.PluginType.Function)]
    public class KouDictionary : KouPlugin<KouDictionary>
    {
        [KouPluginFunction(Help = "现有英文字典(en)、成语字典(idiom)，使用/dict.en help查看英文表详情")]
        public override object Default(string str = null)
        {
            return ReturnHelp(true);
        }
    }
}
