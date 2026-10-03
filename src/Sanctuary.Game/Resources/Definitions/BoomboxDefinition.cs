using System.Collections.Generic;

namespace Sanctuary.Game.Resources.Definitions;

public class BoomboxDefinition
{
    public int ItemId { get; set; }
    public int ModelId { get; set; }
    public int[] EffectIds { get; set; } = [];
    public int[] DanceSequence { get; set; } = [];
    // Native group repeated by the client's standing-animation controller.
    public int StandingDanceAnimationId { get; set; }
    public bool SynchronizedDances { get; set; }
    // Overlap the client's native emote ease-out/ease-in window between clips.
    public int DanceBlendMs { get; set; }
    // For styles without a native group: clip durations, by player model, in sequence order.
    public Dictionary<int, int[]> DanceDurationsMs { get; set; } = [];
    // Concrete clips for independent random playback, indexed by model then animation ID.
    public Dictionary<int, Dictionary<int, int>> IndependentDanceDurationsMs { get; set; } = [];
    public int Priority { get; set; }
    public int TransformReapplyDelayMs { get; set; }
    public int TransformModelId { get; set; }
}
