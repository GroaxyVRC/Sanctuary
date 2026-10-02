using System.Collections.Generic;

namespace Sanctuary.Game.Resources.Definitions;

public class BoomboxDefinition
{
    public int ItemId { get; set; }
    public int ModelId { get; set; }
    public int[] EffectIds { get; set; } = [];
    public int[] DanceSequence { get; set; } = [];
    public bool SynchronizedDances { get; set; }
    // Overlap the client's native emote ease-out/ease-in window between clips.
    public int DanceBlendMs { get; set; }
    // For styles without a native group: clip durations, by player model, in sequence order.
    public Dictionary<int, int[]> DanceDurationsMs { get; set; } = [];
    public int TransformModelId { get; set; }
}
