using System.Collections.Generic;

using Sanctuary.Game.Entities;
using Sanctuary.Packet;

namespace Sanctuary.Game.Helpers;

public static class BoomboxHelper
{
    private const int IdleAnimationId = 1;

    public static void PauseDance(Player player)
    {
        if (player.BoomboxDanceAnimation == 0)
            return;

        player.BoomboxDanceAnimation = 0;
        player.BoomboxDanceIsStanding = false;
        player.SendTunneledToVisible(new PlayerUpdatePacketSetAnimation
        {
            Guid = player.Guid,
            AnimationId = IdleAnimationId,
            Flags = 1
        }, true);
        player.SendTunneledToVisible(new PlayerUpdatePacketSetAnimation
        {
            Guid = player.Guid,
            AnimationId = IdleAnimationId,
            Unknown = 1
        }, true);
    }

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
