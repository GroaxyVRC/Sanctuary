using System.Collections.Generic;
using System.Linq;

using Sanctuary.Packet;
using Sanctuary.Packet.Common;

namespace Sanctuary.Game.Helpers;

public static class WelcomeScreenHelper
{
    // Match the client's default marketplace banner, with a real announcement row
    // so its carousel initializes the count and selected index.
    public static AnnouncementDataSendPacket GetAnnouncementsPacket(IEnumerable<AnnouncementInfo>? announcements = null) => new()
    {
        Announcements = announcements?.ToList() ??
        [
            new()
            {
                Id = 1,
                Priority = 1,
                IconId = 38443,
                TitleStringId = 414,
                BodyStringId = 415,
                ButtonStringId = 436323,
                LuaCall = "Marketplace",
                Param2 = 31415
            }
        ]
    };
}
