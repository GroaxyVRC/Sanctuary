using System;
using System.Linq;
using System.Numerics;

using Sanctuary.Game.Entities;
using Sanctuary.Game.Helpers;
using Sanctuary.Packet;
using Sanctuary.Packet.Common;
using Sanctuary.Packet.Common.Targets;

namespace Sanctuary.Game.Interactions;

public class SnowballInteraction : IInteraction
{
    public const int Type = 33;
    public static readonly InteractionData Data = new() { Id = IInteraction.UniqueId++, Type = Type };
    public int Id => Data.Id;

    public void OnInteract(Player player, IEntity other)
    {
        if (other is not Npc npc || npc.Ability is null ||
            npc.InteractionList?.Interactions.Any(x => x.Type == Type) != true ||
            !InteractionMenuHelper.CanInteract(npc, player))
            return;

        var definition = npc.Ability;
        if (definition.Slot < 0 || definition.Slot >= 8 || definition.DurationMs <= 0)
            return;

        if (player.NpcAbilityEffectId != 0)
            player.RemoveEffect(player.NpcAbilityEffectId);

        player.NpcAbility = definition;
        player.NpcAbilityProfileId = player.ActiveProfileId;
        player.NpcAbilityExpiresAt = DateTimeOffset.UtcNow.AddMilliseconds(definition.DurationMs);
        player.NpcAbilityNextCastAt = DateTimeOffset.MinValue;
        player.NpcAbilityEffectId = player.AddEffect(new PlayerEffect
        {
            ExpiresAt = player.NpcAbilityExpiresAt,
            BuffIconId = definition.BuffIconId,
            BuffNameId = definition.BuffNameId,
            OnRemoved = () => player.NpcAbilityExpiresAt = DateTimeOffset.MinValue
        });
        player.SendTunneledToVisible(new AbilityPacketStartCasting
        {
            CasterGuid = player.Guid,
            Unused = player.Guid,
            AbilityId = definition.PickupAbilityId
        }, true);
        var abilityPacketLaunchAndLand = new AbilityPacketLaunchAndLand
        {
            Guid = player.Guid,
            Unknown10 = definition.PickupEffectId,
            Unknown15 = EffectTagIdGenerator.Next(),
            TargetLocation = new Vector4(0, 0, 0, 1)
        };
        abilityPacketLaunchAndLand.ActionBar.Slot = -1;
        abilityPacketLaunchAndLand.ProjectileParameters.Unknown5 = 1;
        abilityPacketLaunchAndLand.ProjectileParameters.Unknown8 = new Vector4(0, 0, 0, 1);
        abilityPacketLaunchAndLand.ProjectileParameters.Unknown12 = new Vector4(0, 1, 0, 0);
        abilityPacketLaunchAndLand.ProjectileParameters.Unknown15 = 1;
        abilityPacketLaunchAndLand.ProjectileParameters.Unknown16 = 1;
        abilityPacketLaunchAndLand.ProjectileParameters.Unknown18 = definition.PickupAnimationId;
        abilityPacketLaunchAndLand.ProjectileParameters.Unknown27 = 100;
        abilityPacketLaunchAndLand.Targets.Add(Target.CreateTarget(unchecked((long)player.Guid)));
        player.SendTunneledToVisible(abilityPacketLaunchAndLand, true);

        player.SendTunneled(new AbilityPacketAbilityDefinition { Definition = definition.Definition });
        var abilityPacketSetDefinition = new AbilityPacketSetDefinition { ProfileId = player.NpcAbilityProfileId };
        abilityPacketSetDefinition.AbilitySet.Abilities[definition.Slot] = definition.Ability;
        player.SendTunneled(abilityPacketSetDefinition);
    }

    public static void Update(Player player)
    {
        if (player.NpcAbility is null)
            return;
        if (DateTimeOffset.UtcNow < player.NpcAbilityExpiresAt && player.ActiveProfileId == player.NpcAbilityProfileId)
            return;

        var abilityPacketSetDefinition = new AbilityPacketSetDefinition { ProfileId = player.NpcAbilityProfileId };
        player.NpcAbility = null;
        player.RemoveEffect(player.NpcAbilityEffectId);
        player.NpcAbilityEffectId = 0;
        player.SendTunneled(abilityPacketSetDefinition);
        player.SendToolbar();
    }
}
