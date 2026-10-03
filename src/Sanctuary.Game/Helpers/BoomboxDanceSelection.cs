using System.Linq;
using System.Collections.Generic;

namespace Sanctuary.Game.Helpers;

// Called under the zone dance lock. Regular boxes receive one complete clip per turn.
public sealed class BoomboxDanceSelection
{
    private sealed class Turn
    {
        public ulong Owner;
        public long EndsAt;
        public readonly Dictionary<ulong, int> Plays = new();
    }
    private readonly Dictionary<ulong, Turn> _turns = new();

    public ulong Select(ulong player, long now, IEnumerable<(ulong Guid, int Priority)> sources)
    {
        var available = sources.ToArray();
        var priority = available.Select(source => source.Priority).DefaultIfEmpty().Max();
        var candidates = available.Where(source => source.Priority == priority)
            .Select(source => source.Guid).Distinct().Order().ToArray();
        if (candidates.Length == 0)
        {
            _turns.Remove(player);
            return 0;
        }
        if (!_turns.TryGetValue(player, out var turn))
            _turns[player] = turn = new Turn();
        foreach (var guid in turn.Plays.Keys.Where(guid => !available.Any(source => source.Guid == guid)).ToArray())
            turn.Plays.Remove(guid);

        if (candidates.Contains(turn.Owner) && now < turn.EndsAt)
            return turn.Owner;
        var minimum = candidates.Min(id => turn.Plays.GetValueOrDefault(id));
        // Round-robin among equally served boxes, independent of callback execution order.
        var leastServed = candidates.Where(id => turn.Plays.GetValueOrDefault(id) == minimum).ToArray();
        var owner = leastServed.FirstOrDefault(id => id > turn.Owner);
        if (owner == 0) owner = leastServed[0];
        turn.Owner = owner;
        turn.EndsAt = long.MaxValue; // Reserved until this box actually sends its clip.
        return owner;
    }

    public void Remove(ulong player) => _turns.Remove(player);

    public bool Owns(ulong player, ulong owner) =>
        _turns.TryGetValue(player, out var turn) && turn.Owner == owner;

    public void Started(ulong player, ulong owner, long endsAt)
    {
        if (_turns.TryGetValue(player, out var turn) && turn.Owner == owner)
        {
            turn.EndsAt = endsAt;
            turn.Plays[owner] = turn.Plays.GetValueOrDefault(owner) + 1;
        }
    }

}
