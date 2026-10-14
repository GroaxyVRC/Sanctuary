using System.Numerics;

namespace Sanctuary.Core.Extensions;

public static class Vector3Extensions
{
    public static float DistanceSquaredXZ(this Vector3 value, Vector3 other)
    {
        var deltaX = value.X - other.X;
        var deltaZ = value.Z - other.Z;

        return deltaX * deltaX + deltaZ * deltaZ;
    }
}
