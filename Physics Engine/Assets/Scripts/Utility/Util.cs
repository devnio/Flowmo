using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    public static bool AreAABBsColliding(AABB a, AABB b)
    {
        return (a._min.x <= b._max.x && a._max.x >= b._min.x) &&
               (a._min.y <= b._max.y && a._max.y >= b._min.y) &&
               (a._min.z <= b._max.z && a._max.z >= b._min.z);
    }

    public static Vector3 ComputeCentroid(Vector3[] input)
    {
        var res = Vector3.zero;
        foreach(Vector3 v in input)
        {
            res += v;
        }
        
        return res / input.Length;
    }

    public static Vector3 RotatePointAroundPivot(Vector3 pivot, Vector3 point, Vector3 angles)
    {
        var dir = point - pivot;  // get vector relative to pivot
        dir = Quaternion.Euler(angles) * dir;  // rotate vector
        return pivot + dir;
    }



    // ============================
    // COLLISION
    // ============================
    

    // ============================
    // EXTENSIONS
    // ============================
    public static Vector3 ToVector3(this Vector2 v2)
    {
        return new Vector3(v2[0], v2[1], 0);
    }
}


