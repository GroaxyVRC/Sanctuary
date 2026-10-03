using System;
using System.Linq;
using System.Collections.Generic;

using Sanctuary.Game.Resources.Definitions;

namespace Sanctuary.Game.Helpers;

// Advances measured clips for an individual actor or a shared synchronized routine.
public sealed class BoomboxDancePlayback(int[] sequence, int[] durationsMs, int blendMs = 0)
{
    private int _index;
    private long _nextClipAt;

    public long NextClipAt => _nextClipAt;
    public void Resume() => _nextClipAt = 0;

    public static BoomboxDancePlayback? CreateSynchronized(BoomboxDefinition definition) =>
        definition.SynchronizedDances && definition.DanceDurationsMs.Count > 0
            ? new BoomboxDancePlayback(definition.DanceSequence,
                Enumerable.Range(0, definition.DanceSequence.Length)
                    .Select(i => definition.DanceDurationsMs.Values.Max(d => d[i])).ToArray(), definition.DanceBlendMs)
            : null;

    public int Advance(long nowMs)
    {
        if (nowMs < _nextClipAt || sequence.Length == 0 || durationsMs.Length != sequence.Length)
            return 0;

        var animation = sequence[_index];
        var duration = durationsMs[_index];
        if (duration <= 0)
            return 0; // Unknown clip duration must never become another guessed refresh interval.

        _index = (_index + 1) % sequence.Length;
        // Start from actual send time; never catch up by skipping/truncating clips after a stalled tick.
        // The next emote fades in while the outgoing clip finishes its native fade-out.
        // Waiting until its control ends makes the client blend through standing idle.
        _nextClipAt = nowMs + duration - Math.Clamp(blendMs, 0, duration - 1);
        return animation;
    }
}
