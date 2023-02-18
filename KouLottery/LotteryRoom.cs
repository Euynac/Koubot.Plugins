using Koubot.SDK.System.Session;
using Koubot.Shared.Models;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;

namespace KouFunctionPlugin
{
    public class LotteryRoom : KouSessionRoom
    {
        protected override bool AutoJoinRoom => false;

        public override RoomReaction OwnerSay(string line)
        {
            if (line == "抽取")
            {
                return JoinedUsers.RandomGetOne().Name;
            }
            return base.OwnerSay(line);
        }

        public override RoomReaction Say(PlatformUser speaker, string line)
        {
            if (line == "1")
            {
                if (JoinedUsers.Contains(speaker))
                {
                    return $"{speaker.Name}已经在抽奖列表里了";
                }

                return $"{speaker.Name}已加入";
            }
            return false;
        }

        public LotteryRoom(string roomName, PlatformUser ownerUser, PlatformGroup? roomGroup) : base(roomName, ownerUser, roomGroup)
        {
        }
    }
}