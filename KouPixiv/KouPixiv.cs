using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Plugin;
using Koubot.Tool.Extensions;

namespace KouFunctionPlugin.Pixiv
{
    [KouPluginClass("pixiv", "Pixiv助手",
        Introduction = "Pixiv相关插件（施工中）",
        Author = "7zou",
        PluginType = KouEnum.PluginType.Function)]
    public class KouPixiv : KouPlugin<KouPixiv>
    {
        [KouPluginFunction(ActivateKeyword = "count", Name = "当前本地Pixiv作品信息数量")]
        public object CurLocalWorkInfoCount()
        {
            var cache = PixivWork.GetAutoModelCache();
            if (cache.IsNullOrEmptySet()) return "当前没有Pixiv的作品信息";
            return $"现在一共有{cache.Count}个作品信息";
        }


    }
}