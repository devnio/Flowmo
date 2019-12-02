using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// POINT.
/// </summary>
public struct Point
{
    public Vector3 p;

    public Point(Vector3 p)
    {
        this.p = p;
    }
}

/// <summary>
/// LINE: Defined by start and end point.
/// </summary>
public struct Line
{
    public Vector3 start;
    public Vector3 end;

    public Line(Vector3 start, Vector3 end)
    {
        this.start = start;
        this.end = end;
    }
}

/// <summary>
/// INTERVAL: defined by min and max float.
/// </summary>
public struct Interval
{
    public float min;
    public float max;

    public Interval(float min, float max)
    {
        this.min = min;
        this.max = max;
    }
}

/// <summary>
/// PLANE: defined by normal and distance form origin.
/// </summary>
public struct Plane
{
    public Vector3 normal; // direction normal of the plane, always normalized
    public float distance; // distance from origin

    public Plane(Vector3 normal, float distance)
    {
        this.normal = normal;
        this.distance = distance;
    }
}


/// <summary>
/// SPHERE: defined by poistion and radius.
/// </summary>
public struct Sphere
{
    public Point position; 
    public float radius; 

    public Sphere(Point position, float radius)
    {
        this.position = position;
        this.radius = radius;
    }
}

/// <summary>
/// COLLISION MANIFOLD: in case of a collision it wraps all the necessary information formed by the collision.
/// </summary>
public struct CollisionManifold
{
    public bool colliding;

    // collision normal between two colliding objects
    public Vector3 normal;

    // penetration distance of the manifold
    public float depth;

    // set of contact points at which the two objects collide
    public List<Vector3> contacts; 

    //---------------------------------
    // Set the Default values for a Collision Manifold
    //---------------------------------
    public static void Reset(CollisionManifold result)
    {
        //if (result.Equals(default(CollisionManifold))) // check against default value of struct
        result.colliding = false;
        result.normal = new Vector3(0, 0, 1);
        result.depth = float.MaxValue;
        if (result.contacts != null) result.contacts.Clear();
    }
}

// ===================
// GEOMETRY
// ===================
static class Geometry
{
    /// <summary>
    /// Returns cube that contains vertices of obb.
    /// </summary>
    public static Cube GetOBBVertices(ColliderBox obb)
    {
        return obb.cube;
    }

    /// <summary>
    /// Distance between two points p1 and p2.
    /// </summary>
    public static float Distance(Point p1, Point p2)
    {
        return Vector3.Magnitude(p1.p - p2.p);
    }

    /// <summary>
    /// Length of the line.
    /// </summary>
    public static float Length(Line line)
    {
        return Vector3.Magnitude(line.start - line.end);
    }

    /// <summary>
    /// Return result of the plane equation (helper function for planes).
    /// Is a point on one side or the other.
    /// </summary>
    public static float PlaneEquation(Point point, Plane plane)
    {
        return Vector3.Dot(point.p, plane.normal) - plane.distance;
    }

    /// <summary>
    /// Returns true if a point is on the plane.
    /// </summary>
    static bool PointOnPlane(Point point, Plane plane)
    {
        float dot = Vector3.Dot(point.p, plane.normal);
        return Util.CMP(dot, plane.distance);
    }

    /// <summary>
    /// Return closest point on plane given a point in space.
    /// </summary>
    static Point ClosestPoint(Plane plane, Point point)
    {
        float distance = Geometry.PlaneEquation(point, plane);
        return new Point(point.p - (plane.normal * distance));
    }

    /// <summary>
    /// Returns the closest point to a line from a point in space.
    /// </summary>
    static Point ClosestPoint(Line line, Point point)
    {
        Vector3 lVec = line.end - line.start; // line vector
        float t = Vector3.Dot(point.p - line.start, lVec) / Vector3.Dot(lVec, lVec);
        t = Mathf.Clamp01(t);
        return new Point(line.start + lVec * t);
    }

    /// <summary>
    /// Returns true if the point is on the line.
    /// </summary>
    static bool PointOnLine(Point point, Line line)
    {
        Point closest = ClosestPoint(line, point);
        float distSqr = Vector3.SqrMagnitude(closest.p - point.p);
        return Util.CMP(distSqr, 0.0f);
    }

    /// <summary>
    /// Return vertices of the obb.
    /// </summary>
    public static Point[] GetVertices(ColliderBox obb) // find vertices of OBB
    {
        Point[] vert_array = new Point[8]; 
        Cube obb_cube = obb.cube;

        for (int i = 0; i < obb_cube.vertices.Length; i++)
        {
            vert_array[i].p = obb_cube.vertices[i];
        }

        return vert_array;
    }

    /// <summary>
    /// Returns (min, max) interval of the obb on the given axis.
    /// TODO: be careful when using: currently returns in obb space.
    /// </summary>
    /// <param name="obb"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public static Interval GetInterval(ColliderBox obb, Vector3 axis)
    {
        Vector3 C = obb._center;
        Vector3 E = obb.xyzLength;

        Point[] vertex = GetVertices(obb);

        Interval result;

        result.min = result.max = Vector3.Dot(axis, vertex[0].p);

        // project all 8 vertices onto the axis and build inerval
        for (int i = 1; i < 8; ++i) 
        {
            float projection = Vector3.Dot(axis, vertex[i].p);
            result.min = (projection < result.min) ? projection : result.min;
            result.max = (projection > result.max) ? projection : result.max;
        }

        return result;
    }


    /// <summary>
    /// Returns if a point is in obb.
    /// </summary>
    public static bool PointInOBB(Point point, ColliderBox obb)
    {
        // move point relative to obb by subtracting the obb center from the point
        Vector3 rel_point = point.p - obb._center;

        bool Inside(Vector3 rel_p, Vector3 axis, float extent)
        {
            float distance = Vector3.Dot(rel_p, axis);
            extent /= 2; // convert to half-extent

            if (distance > extent || distance < -extent) // for example obb._xyzLength.x value, but half-extent value!
            {
                return false;
            }
            return true;
        }

        Point[] vertices = GetVertices(obb);
        Vector3 x_axis = (vertices[(int)CubeIdx.D].p - vertices[(int)CubeIdx.A].p).normalized;
        Vector3 y_axis = (vertices[(int)CubeIdx.E].p - vertices[(int)CubeIdx.A].p).normalized;
        Vector3 z_axis = (vertices[(int)CubeIdx.B].p - vertices[(int)CubeIdx.A].p).normalized;

        if (!Inside(rel_point, x_axis, obb._xyzLength.x)) return false;
        if (!Inside(rel_point, y_axis, obb._xyzLength.y)) return false;
        if (!Inside(rel_point, z_axis, obb._xyzLength.z)) return false;
        return true;
    }

    /// <summary>
    /// Returns the edges of the obb as lines.
    /// </summary>
    public static Line[] GetEdges(ColliderBox obb) // find edges of OBB
    {
        // OBB always has 12 edges
        Line[] edges = new Line[12]; 
        Point[] vertices = GetVertices(obb);

        int i = 0;

        void BuildLine(CubeIdx From, CubeIdx To, int j)
        {
            int fromIndex = (int)From;
            int toIndex = (int)To;
            edges[j] = new Line(vertices[fromIndex].p, vertices[toIndex].p);
        }

        BuildLine(CubeIdx.A, CubeIdx.B, i++); // A -> B
        BuildLine(CubeIdx.A, CubeIdx.D, i++); // A -> D
        BuildLine(CubeIdx.A, CubeIdx.E, i++); // A -> E

        BuildLine(CubeIdx.G, CubeIdx.F, i++); // G -> F
        BuildLine(CubeIdx.G, CubeIdx.C, i++); // G -> C
        BuildLine(CubeIdx.G, CubeIdx.H, i++); // G -> H

        BuildLine(CubeIdx.E, CubeIdx.F, i++); // E -> F
        BuildLine(CubeIdx.E, CubeIdx.H, i++); // E -> H

        BuildLine(CubeIdx.C, CubeIdx.B, i++); // C -> B
        BuildLine(CubeIdx.C, CubeIdx.D, i++); // C -> D

        BuildLine(CubeIdx.B, CubeIdx.F, i++); // B -> F
        BuildLine(CubeIdx.D, CubeIdx.H, i++); // D -> H

        return edges;
    }

    /// <summary>
    /// Returns the planes of the obb.
    /// </summary>
    public static List<Plane> GetPlanes(ColliderBox obb) 
    {
        Vector3 c = obb._center; // center
        Vector3 l = obb._xyzLength / 2; // half-extents
        Point[] vertices = GetVertices(obb);

        // compute directions on all axis
        Vector3 z_dir = (vertices[(int)CubeIdx.B].p - vertices[(int)CubeIdx.A].p).normalized;
        Vector3 x_dir = (vertices[(int)CubeIdx.D].p - vertices[(int)CubeIdx.A].p).normalized;
        Vector3 y_dir = (vertices[(int)CubeIdx.E].p - vertices[(int)CubeIdx.A].p).normalized;

        List<Plane> result = new List<Plane>();

        // build all the planes on the obb TODO: check if right (shouldn't distance be the magnitude of (c + x_dir * l.x?)
        result.Add(new Plane(x_dir, Vector3.Dot(x_dir, (c + x_dir * l.x))));
        result.Add(new Plane(-x_dir, Vector3.Dot(-x_dir, (c - x_dir * l.x))));
        result.Add(new Plane(y_dir, Vector3.Dot(y_dir, (c + y_dir * l.y))));
        result.Add(new Plane(-y_dir, Vector3.Dot(-y_dir, (c - y_dir * l.y))));
        result.Add(new Plane(z_dir, Vector3.Dot(z_dir, (c + z_dir * l.z))));
        result.Add(new Plane(-z_dir, Vector3.Dot(-z_dir, (c - z_dir * l.z))));

        return result;
    }

    /// <summary>
    /// This function checks if a line intersects a plane and if it does, the line is clipped to the plane
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="line"></param>
    /// <param name="outPoint"></param>
    /// <returns></returns>
    public static Tuple<Point, bool> ClipToPlane(Plane plane, Line line) //, Point outPoint) // clip line to plane
    {
        /// TODO: check if parameter 'Point outPoint' is really needed!
        Tuple<Point, bool> info = new Tuple<Point, bool>(new Point(), false);

        // ensure that the line and plane intersect
        Vector3 ab = line.end - line.start;
        float nA = Vector3.Dot(plane.normal, line.start);
        float nAB = Vector3.Dot(plane.normal, ab);

        //if (Util.CMP(nAB, 0.0f)) return info;

        // Find the parameter t along the line at which it intersects the plane
        float t = (plane.distance - nA) / nAB;

        Logger.Instance.DebugInfo("plane normal: " + plane.normal);
        Logger.Instance.DebugInfo("plane distance: " + plane.distance);
        Logger.Instance.DebugInfo("line start: " + line.start.y);
        Logger.Instance.DebugInfo("line end: " + line.end.y);
        Logger.Instance.DebugInfo("nA: " + nA);
        Logger.Instance.DebugInfo("nAB: " + nAB);
        Logger.Instance.DebugInfo("plane.distance - nA: " + (plane.distance - nA));
        Logger.Instance.DebugInfo("t: " + t);


        // If the intersection parameter t was valid, return the point at which the line and plane intersect
        if (t >= 0.0f && t <= 1.0f)
        {
            info.Item1.p = line.start + ab * t;
            info.Item2 = true;
            return info;
        }

        // return false if parameter t was not within the range, thus
        // the plane and line segment do not intersect
        return info;
    }

    /// <summary>
    /// This function takes an array of edges that represent an oriented bounding box and another 
    /// oriented bounding box. The edges provided are clipped against the planes of the 
    /// provided bounding box.
    /// </summary>
    public static List<Point> ClipEdgesToOBB(Line[] edges, ColliderBox obb)
    {

        List<Point> result = new List<Point>();
        List<Plane> planes = GetPlanes(obb);

        for (int i = 0; i < planes.Count; ++i)
        {
            for (int j = 0; j < edges.Length; ++j)
            {
                Tuple<Point, bool> info = ClipToPlane(planes[i], edges[j]);
                if (info.Item2)
                {
                    Point intersection = info.Item1;
                    // TODO: why do we need to check again if the point is in OBB?
                    // If we passed previous test it means we intersected the plane of the obb.
                    if (PointInOBB(intersection, obb))
                    {
                        result.Add(intersection);
                    }
                }
            }
        }

        return result;
    }


    /// <summary>
    /// This function uses similar logic to testing if objects separate on a single axis in the SAT test
    /// </summary>
    /// <param name="obb1"></param>
    /// <param name="obb2"></param>
    /// <param name="axis"></param>
    /// <param name="outShouldFlip"></param>
    /// <returns></returns>
    public static Tuple<float, bool> PenetrationDepth(ColliderBox obb1, ColliderBox obb2, Vector3 axis, bool outShouldFlip) 
    {
        Tuple<float, bool> info = new Tuple<float, bool>(0.0f, outShouldFlip);

        Interval i1 = GetInterval(obb1, axis.normalized);
        Interval i2 = GetInterval(obb2, axis.normalized);

        if (!((i2.min <= i1.max) && (i1.min <= i2.max)))
        {
            return info; // No penerattion
        }

        float len1 = i1.max - i1.min;
        float len2 = i2.max - i2.min;
        float min = Mathf.Min(i1.min, i2.min);
        float max = Mathf.Max(i1.max, i2.max);
        float length = max - min;

        if (info.Item2 != false)
        {
            info.Item2 = (i2.min < i1.min);
        }

        info.Item1 =  (len1 + len2) - length;

        return info;
    }


    public static CollisionManifold FindCollisionFeatures(ColliderBox obb1, ColliderBox obb2)
    {
        Debug.Log("Start");
        CollisionManifold result = new CollisionManifold();
        CollisionManifold.Reset(result);

        // First, make a quick collision test on spheres within the OBBs, if they don't collide => OBBs dont collide
        Sphere s1 = new Sphere(new Point(obb1._center), obb1.xyzLength.magnitude / 2);
        Sphere s2 = new Sphere(new Point(obb2._center), obb2.xyzLength.magnitude / 2);

        if (!SphereSphere(s1, s2))
        {
            Debug.Log("Return from SphereSphere Test.");
            return result;
        }
        Debug.Log("SphereSphere Test Done.");

        Cube c1 = obb1.cube;
        Cube c2 = obb2.cube;

        List<Vector3> axis = new List<Vector3>();

        // Get axis from first cube
        Vector3 c1axis1 = (c1.vertices[(int)CubeIdx.B] - c1.vertices[(int)CubeIdx.A]).normalized;
        Vector3 c1axis2 = (c1.vertices[(int)CubeIdx.D] - c1.vertices[(int)CubeIdx.A]).normalized;
        Vector3 c1axis3 = (c1.vertices[(int)CubeIdx.E] - c1.vertices[(int)CubeIdx.A]).normalized;

        axis.Add(c1axis1);
        axis.Add(c1axis2);
        axis.Add(c1axis3);

        // Get axis from second cube
        Vector3 c2axis1 = (c2.vertices[(int)CubeIdx.B] - c2.vertices[(int)CubeIdx.A]).normalized;
        Vector3 c2axis2 = (c2.vertices[(int)CubeIdx.D] - c2.vertices[(int)CubeIdx.A]).normalized;
        Vector3 c2axis3 = (c2.vertices[(int)CubeIdx.E] - c2.vertices[(int)CubeIdx.A]).normalized;

        axis.Add(c2axis1);
        axis.Add(c2axis2);
        axis.Add(c2axis3);

        // Check 9 axis given by cross product between 2 cubes
        for (int i = 0; i < 3; ++i)
        {
            axis.Add(Vector3.Cross(axis[i], axis[3]));
            axis.Add(Vector3.Cross(axis[i], axis[4]));
            axis.Add(Vector3.Cross(axis[i], axis[5]));
        }

        Vector3[] test = axis.ToArray();

        Vector3 hitNormal = new Vector3(0.0f, 0.0f, 0.0f);
        bool shouldFlip = false;

        for (int i = 0; i < axis.Count; ++i) // axis.Count = 15
        {
            if (test[i].x < 0.000001f) test[i].x = 0.0f;
            if (test[i].y < 0.000001f) test[i].y = 0.0f;
            if (test[i].z < 0.000001f) test[i].z = 0.0f;
            if (Vector3.Magnitude(test[i]) < 0.001f) continue;

            Debug.Log("Penetration Depth.");
            Tuple<float, bool> pntrtion_info = PenetrationDepth(obb1, obb2, test[i], shouldFlip);
            float depth = pntrtion_info.Item1;
            shouldFlip = pntrtion_info.Item2;

            if (depth < 0.0f) return result; // if penetration depth < 0.0f 
            else if (depth < result.depth)
            {
                if (shouldFlip) test[i] = test[i] * -1.0f;

                result.depth = depth;
                hitNormal = test[i];
            }

        }

        Debug.Log("Penetration Depth Done.");

        if (hitNormal.magnitude == 0.0f) return result;

        Vector3 axs = hitNormal.normalized;

        List<Point> list1 = ClipEdgesToOBB(GetEdges(obb2), obb1);
        List<Point> list2 = ClipEdgesToOBB(GetEdges(obb1), obb2);

        result.contacts = new List<Vector3>();

        foreach (Point point in list1) result.contacts.Add(point.p);
        foreach (Point point in list2) result.contacts.Add(point.p);

        Interval interval = GetInterval(obb1, axs);
        float distance = (interval.max - interval.min) * 0.5f - result.depth * 0.5f;
        Vector3 pointOnPlane = obb1._center + axs * distance;

        for (int i = result.contacts.Count - 1; i >= 0; --i)
        {
            Vector3 contact = result.contacts[i];
            result.contacts[i] = contact + (axs * Vector3.Dot(axs, pointOnPlane - contact));

            // This bit is in the "There is more" section of the book
            for (int j = result.contacts.Count - 1; j > i; --j)
            {
                float magnitude = Vector3.Magnitude(result.contacts[j] - result.contacts[i]);
                if (magnitude * magnitude < 0.0001f)
                {
                    //result.contacts.erase(result.contacts.begin() + j);
                    result.contacts.RemoveAt(j);
                    break;
                }
            }
        }

        result.colliding = true;
        result.normal = axs;

        return result;
    }

    private static bool SphereSphere(Sphere sp1, Sphere sp2)
    {
        // Compute distance vector
        Vector3 sp1_sp2 = sp1.position.p - sp2.position.p;
        float longSpan = sp1_sp2.magnitude;
        float overlapSpan = sp1.radius + sp2.radius;
        Debug.Log("longSpan: " + longSpan);
        Debug.Log("overlapSpan: " + overlapSpan);

        bool colliding = longSpan < overlapSpan;

        return colliding;
    }


}
 