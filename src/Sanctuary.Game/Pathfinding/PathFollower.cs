using System;
using System.Collections.Generic;
using System.Numerics;

using Sanctuary.Core.Extensions;

namespace Sanctuary.Game.Pathfinding;


public static class PathFollower
{
    public readonly record struct AdvanceResult(bool Moved, Vector3 NewPosition, Quaternion? NewRotation, bool Arrived);

    public static AdvanceResult Advance(PathState path, Vector3 currentPosition, float speed, float tolerance, float deltaSeconds)
    {

        if (!path.TryGetCurrentTarget(out var targetPosition))
            return new AdvanceResult(false, currentPosition, null, true);

        var toTarget = targetPosition - currentPosition;
        var distance = toTarget.Length();


        // This logic here is pretty simple: if we're within a tolerance of the waypoint,
        // 'path.Advance' will pop the waypoint. 
        if (toTarget.X * toTarget.X + toTarget.Z * toTarget.Z <= tolerance * tolerance)
        {
            path.Advance();
            return new AdvanceResult(false, currentPosition, null, path.IsEmpty);
        }

        var direction = Vector3.Normalize(toTarget);
        var step = direction * speed * deltaSeconds;
        if (step.Length() > distance)
            step = toTarget;

        var newPosition = currentPosition + step;

        return new AdvanceResult(true, newPosition, direction.ToFlatRotation(), false);
    }
}