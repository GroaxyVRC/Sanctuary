using Sanctuary.Packet.Common;

namespace Sanctuary.Game.Helpers;

public static class InteractionMenuHelper
{
    public static NotificationInfo GetNotification(ulong guid, NotificationInfo definition) => new()
    {
        Guid = guid,
        IsCompact = definition.IsCompact,
        NotificationType = definition.NotificationType,
        IconId = definition.IconId,
        IconState = definition.IconState,
        Unknown = definition.Unknown,
        NameId = definition.NameId,
        ReferenceId = definition.ReferenceId,
        Unknown2 = definition.Unknown2,
        Unknown3 = definition.Unknown3,
        Enabled = definition.Enabled
    };
}
