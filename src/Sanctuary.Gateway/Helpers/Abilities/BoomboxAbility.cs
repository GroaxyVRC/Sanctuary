using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Sanctuary.Game.Entities;
using Sanctuary.Game.Helpers;
using Sanctuary.Game.Resources.Definitions;
using Sanctuary.Game.Zones;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;

namespace Sanctuary.Gateway.Helpers.Abilities;

public sealed class BoomboxAbility(AbilityServices services) : ConsumableAbility(services)
{
    public override bool Matches(ClientItemDefinition itemDefinition) =>
        _resourceManager.Consumables.Boomboxes.ContainsKey(itemDefinition.Id);

    public override bool HandleAbility(Player player, AbilityPacketClientRequestStartAbility abilityPacketClientRequestStartAbility, int slot, ClientItem clientItem, ClientItemDefinition itemDefinition)
    {
        if (player.IsItemOnCooldown(itemDefinition.Id))
            return SendFailure(player);

        if (!_resourceManager.Consumables.Boomboxes.TryGetValue(itemDefinition.Id, out var definition))
            return SendFailure(player);

        SpawnBoomboxNpc(player, itemDefinition, definition);

        player.StartItemCooldown(itemDefinition.Id, ClampCooldown(definition.DurationMs));
        player.StartActionBarCooldown(ActionBarId, slot, itemDefinition.Icon.Id, itemDefinition.NameId, clientItem.Count, ClampCooldown(definition.DurationMs));

        return true;
    }

    private void SpawnBoomboxNpc(Player player, ClientItemDefinition itemDefinition, BoomboxDefinition definition)
    {
        var modelId = definition.ModelId;
        var effectIds = definition.EffectIds;
        var effectId = effectIds.Length > 0 ? effectIds[System.Random.Shared.Next(effectIds.Length)] : 0;

        var danceSequence = definition.DanceSequence;
        var transformModelId = definition.TransformModelId;

        var leftDirection = Vector3.Transform(new Vector3(-1, 0, 0), player.Rotation);
        var spawnPosition = new Vector4(
            player.Position.X + leftDirection.X * definition.SpawnOffset,
            player.Position.Y + leftDirection.Y * definition.SpawnOffset,
            player.Position.Z + leftDirection.Z * definition.SpawnOffset,
            player.Position.W
        );

        var boomboxNpc = SpawnNpc(player, spawnPosition, npc =>
        {
            npc.NameId = 0;
            npc.ModelId = modelId;
            npc.Name = "Boombox";
            npc.TextureAlias = itemDefinition.TextureAlias ?? "";
            npc.TintAlias = itemDefinition.TintAlias ?? "";
            npc.Scale = 1.0f;
            npc.Animation = definition.SpawnAnimationId; // Bouncing animation
            npc.CompositeEffectId = effectId; // Owned by the entity, so the client stops it on RemovePlayer
            npc.HideNamePlate = true;
            npc.IsInteractable = false;
        });

        if (boomboxNpc is null)
            return;

        var poofRecipients = BroadcastSpawn(player, boomboxNpc, spawnPosition, definition.SpawnEffectId);

        // Tag-attached so it can be stopped cleanly on despawn.
        var songTagId = 0;

        if (effectId != 0)
        {
            songTagId = NextEffectTagId();

            var songEffect = new PlayerUpdatePacketAddEffectTagCompositeEffect
            {
                Guid = boomboxNpc.Guid,
                TagId = songTagId,
                CompositeEffectId = effectId,
                SourceGuid = boomboxNpc.Guid,
            };

            foreach (var recipient in poofRecipients)
                recipient.SendTunneled(songEffect);
        }

        StartDanceLoop(player.Zone, boomboxNpc, spawnPosition, definition, danceSequence, songTagId, effectId, transformModelId);
    }

    private static void StartDanceLoop(IZone zone, Npc boomboxNpc, Vector4 spawnPosition, BoomboxDefinition definition, int[] danceSequence, int songTagId, int effectId, int transformModelId)
    {
        const int SwitchMs = 4000;

        var danceCenter = new Vector3(spawnPosition.X, spawnPosition.Y, spawnPosition.Z);

        var dancing = new HashSet<ulong>();
        var elapsedMs = 0;
        var sinceSwitch = SwitchMs; // so a dance starts on the first tick
        var sequenceIndex = 0;
        var previousAnim = -1;
        var currentAnim = 0;

        boomboxNpc.UpdateEverySecondAction = () =>
        {
            if (elapsedMs >= definition.DurationMs)
            {
                foreach (var player in zone.Players.Where(p => dancing.Contains(p.Guid)))
                    StopDancing(player, transformModelId);

                if (songTagId != 0)
                {
                    var stopSong = new PlayerUpdatePacketRemoveEffectTagCompositeEffect
                    {
                        Guid = boomboxNpc.Guid,
                        TagId = songTagId,
                    };

                    foreach (var player in zone.Players)
                        player.SendTunneled(stopSong);
                }

                DespawnNpc(boomboxNpc, definition.SpawnEffectId);
                return;
            }

            // Only flag a change when the id differs, so multi-dance boomboxes don't restart
            // the crowd every rotation.
            var animChanged = false;

            if (sinceSwitch >= SwitchMs)
            {
                var selected = danceSequence.Length > 0 ? danceSequence[sequenceIndex % danceSequence.Length] : 3501;
                sequenceIndex++;
                sinceSwitch = 0;

                // A single-clip sequence (Totem, Realms Roll) never changes id, and the client
                // stops after one play-through unless it's re-triggered every rotation.
                if (selected != previousAnim || danceSequence.Length <= 1)
                {
                    currentAnim = selected;
                    previousAnim = selected;
                    animChanged = true;
                }
            }

            var players = zone.Players.ToList();
            var inRange = players.Where(p =>
                Vector3.Distance(new Vector3(p.Position.X, p.Position.Y, p.Position.Z), danceCenter) <= definition.Range)
                .ToList();
            var inRangeGuids = inRange.Select(p => p.Guid).ToHashSet();

            foreach (var player in players.Where(p => dancing.Contains(p.Guid) && !inRangeGuids.Contains(p.Guid)))
                StopDancing(player, transformModelId);

            var newcomers = inRange.Where(p => !dancing.Contains(p.Guid)).ToList();
            dancing = inRangeGuids;

            if (transformModelId != 0)
            {
                foreach (var player in newcomers.Where(p => p.TemporaryAppearance == 0))
                    player.ApplyTemporaryAppearance(transformModelId, 0);
            }

            // Re-sync everyone on a rotation to stay phase-locked, otherwise only start late
            // arrivals so the rest don't hitch.
            if (animChanged)
                BoomboxHelper.SyncDance(inRange, currentAnim);
            else if (newcomers.Count > 0)
                BoomboxHelper.SyncDance(newcomers, currentAnim);

            // This targets the boombox's guid, not the player's, so a newcomer whose tile
            // visibility hasn't caught up drops it as an unknown entity and never hears the song.
            // Make sure they have AddNpc first.
            if (songTagId != 0 && newcomers.Count > 0)
            {
                var songEffect = new PlayerUpdatePacketAddEffectTagCompositeEffect
                {
                    Guid = boomboxNpc.Guid,
                    TagId = songTagId,
                    CompositeEffectId = effectId,
                    SourceGuid = boomboxNpc.Guid,
                };

                foreach (var player in newcomers)
                {
                    if (!boomboxNpc.VisiblePlayers.ContainsKey(player.Guid))
                        player.SendTunneled(boomboxNpc.GetAddNpcPacket());

                    player.SendTunneled(songEffect);
                }
            }

            elapsedMs += 1000;
            sinceSwitch += 1000;
        };
    }

    private static void StopDancing(Player player, int transformModelId)
    {
        if (transformModelId != 0 && player.TemporaryAppearance == transformModelId)
            player.RemoveTemporaryAppearance();

        player.SendTunneledToVisible(new PlayerUpdatePacketSetAnimation
        {
            Guid = player.Guid,
            AnimationId = IdleAnimationId,
            Flags = 1
        }, true);
    }
}
