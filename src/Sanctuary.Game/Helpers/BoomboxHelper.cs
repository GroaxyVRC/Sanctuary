using System.Collections.Generic;

using Sanctuary.Game.Entities;
using Sanctuary.Packet;

namespace Sanctuary.Game.Helpers;

public static class BoomboxHelper
{
    public static void SyncDance(List<Player> targets, int animationId)
    {
        if (targets.Count == 0)
            return;

        var playerUpdatePacketSetSynchronizedAnimations = new PlayerUpdatePacketSetSynchronizedAnimations();

        foreach (var player in targets)
            playerUpdatePacketSetSynchronizedAnimations.Animations.Add(new PlayerUpdatePacketSetSynchronizedAnimations.Animation { Guid = player.Guid, AnimationId = animationId });

        var recipients = new HashSet<Player>(targets);

        foreach (var player in targets)
            foreach (var visiblePlayer in player.VisiblePlayers.Values)
                recipients.Add(visiblePlayer);

        var data = Player.SerializeTunneled(playerUpdatePacketSetSynchronizedAnimations);

        foreach (var recipient in recipients)
            recipient.SendSerialized(data);
    }
}
