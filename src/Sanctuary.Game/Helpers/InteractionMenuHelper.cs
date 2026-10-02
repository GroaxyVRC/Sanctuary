using System.Collections.Generic;

using Sanctuary.Game.Entities;
using Sanctuary.Packet;

namespace Sanctuary.Game.Helpers;

public static class InteractionMenuHelper
{
    public static PlayerUpdatePacketNpcRelevance GetNpcRelevancePacket(IEnumerable<Npc> npcs)
    {
        var playerUpdatePacketNpcRelevance = new PlayerUpdatePacketNpcRelevance();
        foreach (var npc in npcs)
        {
            if (npc is Mount || (npc.HasCursor is null && npc.CursorId == 0))
                continue;

            playerUpdatePacketNpcRelevance.Entries.Add(new PlayerUpdatePacketNpcRelevance.Entry
            {
                Guid = npc.Guid,
                HasCursor = npc.HasCursor ?? npc.CursorId != 0,
                CursorId = npc.CursorId,
                Unknown2 = npc.RelevanceUnknown2
            });
        }
        return playerUpdatePacketNpcRelevance;
    }
}
