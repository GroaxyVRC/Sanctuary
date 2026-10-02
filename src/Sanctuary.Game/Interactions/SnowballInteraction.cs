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
    private const int ActionBarId = 1;
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
        var pickup = new AbilityPacketLaunchAndLand
        {
            Guid = player.Guid,
            Unknown10 = definition.PickupEffectId,
            Unknown15 = EffectTagIdGenerator.Next(),
            TargetLocation = new Vector4(0, 0, 0, 1)
        };
        pickup.ActionBar.Slot = -1;
        pickup.ProjectileParameters.Unknown5 = 1;
        pickup.ProjectileParameters.Unknown8 = new Vector4(0, 0, 0, 1);
        pickup.ProjectileParameters.Unknown12 = new Vector4(0, 1, 0, 0);
        pickup.ProjectileParameters.Unknown15 = 1;
        pickup.ProjectileParameters.Unknown16 = 1;
        pickup.ProjectileParameters.Unknown18 = definition.PickupAnimationId;
        pickup.ProjectileParameters.Unknown27 = 100;
        pickup.Targets.Add(Target.CreateTarget(unchecked((long)player.Guid)));
        player.SendTunneledToVisible(pickup, true);

        player.SendTunneled(new AbilityPacketAbilityDefinition { Definition = definition.Definition });
        var set = new AbilityPacketSetDefinition { ProfileId = player.NpcAbilityProfileId };
        set.AbilitySet.Abilities[definition.Slot] = definition.Ability;
        player.SendTunneled(set);
    }

    public static void Update(Player player)
    {
        if (player.NpcAbility is null)
            return;
        if (DateTimeOffset.UtcNow < player.NpcAbilityExpiresAt && player.ActiveProfileId == player.NpcAbilityProfileId)
            return;

        var set = new AbilityPacketSetDefinition { ProfileId = player.NpcAbilityProfileId };
        player.NpcAbility = null;
        player.RemoveEffect(player.NpcAbilityEffectId);
        player.NpcAbilityEffectId = 0;
        player.SendTunneled(set);
        player.SendToolbar();
    }

    public static bool HandleAbility(Player player, AbilityPacketClientRequestStartAbility request)
    {
        Update(player);
        var definition = player.NpcAbility;
        if (definition is null || request.Data.Id != ActionBarId || request.Data.Slot != definition.Slot)
            return false;

        var now = DateTimeOffset.UtcNow;
        if (now < player.NpcAbilityNextCastAt)
        {
            player.SendTunneled(new AbilityPacketFailed { StringId = 3079 });
            return true;
        }

        var target = player.FindTarget(request.Target == 1 ? 0 : request.Guid, definition.Ability.Range);
        var targetGuid = target?.Guid ?? 0;
        var targetPosition = target?.Position ?? player.Position + new Vector4(player.GetFacingDirection() * definition.Ability.Range, 0);
        var distance = Vector3.DistanceSquared(new Vector3(player.Position.X, player.Position.Y, player.Position.Z),
            new Vector3(targetPosition.X, targetPosition.Y, targetPosition.Z));
        if (!float.IsFinite(distance) || (target is not null && distance > definition.Ability.Range * definition.Ability.Range))
        {
            player.SendTunneled(new AbilityPacketFailed { StringId = 3079 });
            return true;
        }

        player.NpcAbilityNextCastAt = now.AddMilliseconds(definition.Definition.Unknown20);
        player.SendTunneledToVisible(new AbilityPacketStartCasting
        {
            CasterGuid = player.Guid,
            Unused = player.Guid,
            AbilityId = definition.Definition.Id
        }, true);
        var packet = new AbilityPacketLaunchAndLand
        {
            Guid = player.Guid,
            Unknown2 = definition.Definition.Unknown19,
            Unknown4 = definition.Definition.Unknown12,
            Unknown6 = definition.Definition.Unknown20,
            Unknown9 = definition.Definition.Unknown13,
            Unknown10 = definition.Definition.CompositeEffect,
            Unknown15 = EffectTagIdGenerator.Next(),
            TargetLocation = targetGuid == 0 ? targetPosition : new Vector4(0, 0, 0, 1),
            ActionBar = request.Data,
            ProjectileParameters = definition.ProjectileParameters
        };
        if (targetGuid == 0)
            packet.Targets.Add(Target.CreateTargetLocation(targetPosition, targetPosition));
        else
            packet.Targets.Add(Target.CreateTarget(unchecked((long)targetGuid)));

        if (targetGuid != 0)
            player.Projectiles.AddProjectile(player, targetGuid, definition.HitAnimationId, definition.RecoveryAnimationId,
                definition.RecoveryDelayMs, definition.HitReportTimeoutMs,
                (int)(MathF.Sqrt(distance) / BitConverter.Int32BitsToSingle(definition.ProjectileParameters.Unknown3) * 1000));

        player.SendTunneledToVisible(packet, true);
        return true;
    }
}
