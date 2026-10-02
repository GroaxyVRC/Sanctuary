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
    public bool OpensAtlas { get; set; }
    public bool AutoSelectSingleInteraction { get; set; }
    public int InteractRange { get; set; } = 100;
    public bool IsInteractable { get; set; } = true;
    public InteractionList? InteractionList { get; set; }
    public bool InteractionUnknown { get; set; }
    public NotificationInfo? Notification { get; set; }
    public List<NotificationInfo> Notifications { get; set; } = [];
    public string[]? Scripts { get; set; }
}
