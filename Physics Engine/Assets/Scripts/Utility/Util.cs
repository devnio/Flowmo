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
    /// <summary>
    /// Finds the closest point on the OBB given another point.
    /// </summary>
    /// <param name="b"></param>
    /// <param name="s"></param>
    /// <returns></returns>
    public static Tuple<Vector3, bool> FindClosestPoint(ColliderBox b, SphereCollider s)
    {
        Cube c = b.cube;

        Vector3 cAxis1 = c.vertices[(int)CubeIdx.D] - c.vertices[(int)CubeIdx.A];  // local x
        Vector3 cAxis2 = c.vertices[(int)CubeIdx.E] - c.vertices[(int)CubeIdx.A];  // local y
        Vector3 cAxis3 = c.vertices[(int)CubeIdx.B] - c.vertices[(int)CubeIdx.A];  // local z

        Vector3[] axes = { cAxis1.normalized, cAxis2.normalized, cAxis3.normalized };
        Vector3 halfLenghts = b._xyzLength / 2f;

        // Represent sphere position from obbs origin
        bool centerIsInsideObb = true;
        Vector3 closestPoint = b._center;
        Vector3 point = s._center - b._center;
        for (int i = 0; i < axes.Length; i++)
        {
            // Project the point and
            float projValue = Vector3.Dot(point, axes[i]);

            // See if point is bigger than half extents of obb
            float halfLength = halfLenghts[i];

            Logger.Instance.DebugInfo("Proj Value " + i + ": " + projValue + ", HalfLen: " + halfLength, "SPHERE-OBB CHECK");

            // Manual clamping
            if (projValue < -halfLength)
            {
                centerIsInsideObb = false;
                projValue = -halfLength;
            }
            else if (projValue > halfLength)
            {
                centerIsInsideObb = false;
                projValue = halfLength;
            }

            closestPoint += axes[i] * projValue;            
        }

        // If center is inside Obb we have to find the nearest face and project the point there (need for collision resolution)
        if (centerIsInsideObb)
        {
            float min = float.MaxValue;
            int idx = 0;
            float faceDir = 1;
            for (int i = 0; i < axes.Length; i++)
            {
                float projValue = Vector3.Dot(point, axes[i]);
                float halfLength = halfLenghts[i];
                float diff = Mathf.Min(min, halfLength - Mathf.Abs(projValue));
                if (min > diff)
                {
                    faceDir = Mathf.Sign(projValue);
                    min = diff;
                    idx = i;
                }
            }
            closestPoint += axes[idx] * faceDir * min;
        }
            

        return new Tuple<Vector3, bool>(closestPoint, centerIsInsideObb);
    }

    /// <summary>
    /// Compare floats.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="epsilon"></param>
    /// <returns></returns>
    public static bool CMP(float a, float b, float epsilon = float.Epsilon)
    {
        const float MinNormal = 1.175494E-38f;
        float absA = Mathf.Abs(a);
        float absB = Mathf.Abs(b);
        float diff = Mathf.Abs(a - b);

        if (a.Equals(b))
        {
            // shortcut, handles infinities
            return true;
        }
        else if (a == 0 || b == 0 || absA + absB < MinNormal)
        {
            // a or b is zero or both are extremely close to it
            // relative error is less meaningful here
            return diff < (epsilon * MinNormal);
        }
        else
        {
            // use relative error
            return diff / (absA + absB) < epsilon;
        }
    }

    // ============================
    // EXTENSIONS
    // ============================
    public static Vector3 ToVector3(this Vector2 v2)
    {
        return new Vector3(v2[0], v2[1], 0);
    }
}


