using System.Collections.Generic;

using Sanctuary.Packet.Common;

namespace Sanctuary.Game.Resources.Definitions;

public class NpcDefinition
{
    public int Id { get; set; }

    public int NameId { get; set; }
    public string? Name { get; set; }

    public int ModelId { get; set; }
    public string ModelFileName { get; set; } = null!;

    public string? TextureAlias { get; set; }
    public string? TintAlias { get; set; }
    public int TintId { get; set; }
    public float? Scale { get; set; }
    public bool HideNamePlate { get; set; }
    public int Disposition { get; set; } = 1;
    public int Animation { get; set; } = 1;
    public int CompositeEffectId { get; set; }
    public float VerticalOffset { get; set; }
    public List<CharacterAttachmentData> Attachments { get; set; } = [];
    public NotificationInfo? Notification { get; set; }
    public List<NotificationInfo> Notifications { get; set; } = [];
    public string[]? Scripts { get; set; }
}
