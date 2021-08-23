using Koubot.SDK.System;
using Koubot.Shared.Models;

namespace KouFunctionPlugin
{
    public class LotteryRoom : KouSessionRoom
    {

        public override bool OwnerSay(string line, out string result)
        {
            if (line == "开始")
            {

            }
            throw new System.NotImplementedException();
        }

        public override bool Say(PlatformUser speaker, string line)
        {
            throw new System.NotImplementedException();
        }
    }
}