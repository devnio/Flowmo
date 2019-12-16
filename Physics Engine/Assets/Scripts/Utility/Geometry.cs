using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public struct Point
{
    public Vector3 p;

    public Point(Vector3 p)
    {
        this.p = p;
    }
}

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

public struct CollisionManifold
{
    public bool colliding;
    public float depth; // penetration distance of the manifold

    public Vector3 normal; // collision normal between two colliding objects
    public Vector3 avg_contact; // middle point of the contact points on surface
    public Vector3 avg_depth; // middle point of depth points inside obb

    public List<Vector3> contacts; // set of contact points at which the two objects collide
    public List<Vector3> depths; // set of contact points at the depth inside second object

    //---------------------------------
    // Set the Default values for a Collision Manifold
    //---------------------------------
    public void Reset()
    {
        //if (result.Equals(default(CollisionManifold))) // check against default value of struct
        this.colliding = false;
        this.normal = new Vector3(0, 0, 1);
        this.avg_depth = Vector3.zero;
        this.avg_contact = Vector3.zero;
        this.depth = float.MaxValue;
        if (this.contacts != null) this.contacts.Clear();
        if (this.depths != null) this.depths.Clear();
    }
}

static class Geometry
{
    // Distance between two points p1 and p2
    public static float Distance(Point p1, Point p2)
    {
        return Vector3.Magnitude(p1.p - p2.p);
    }

    // Length of the line
    public static float Length(Line line)
    {
        return Vector3.Magnitude(line.start - line.end);
    }

    // Return result of the plane equation (helper function for planes) 
    public static float PlaneEquation(Point point, Plane plane)
    {
        return Vector3.Dot(point.p, plane.normal) - plane.distance;
    }

    static bool PointOnPlane(Point point, Plane plane)
    {
        float dot = Vector3.Dot(point.p, plane.normal);
        return Util.CMP(dot, plane.distance);
    }

    static Point ClosestPoint(Plane plane, Point point)
    {
        float dot = Vector3.Dot(plane.normal, point.p);
        float distance = dot - plane.distance;
        return new Point(point.p - (plane.normal * distance));
    }

    static bool PointOnLine(Point point, Line line)
    {
        Point closest = ClosestPoint(line, point);
        float magntde = Vector3.Magnitude(closest.p - point.p);
        float distanceSq = magntde * magntde;
        return Util.CMP(distanceSq, 0.0f);
    }

    static Point ClosestPoint(Line line, Point point)
    {
        Vector3 lVec = line.end - line.start; // line vector
        float t = Vector3.Dot(point.p - line.start, lVec) / Vector3.Dot(lVec, lVec);
        t = Mathf.Max(t, 0.0f);
        t = Mathf.Min(t, 1.0f);
        return new Point(line.start + lVec * t);
    }

    public static Interval GetInterval(ColliderBox obb, Vector3 axis)
    {
        Point[] vertex = GetVertices(obb);

        Interval result;

        result.min = Vector3.Dot(axis, vertex[0].p);
        result.max = Vector3.Dot(axis, vertex[0].p);

        for (int i = 1; i < 8; ++i) // project all 8 vertices onto the axis and build interval
        {
            float projection = Vector3.Dot(axis, vertex[i].p);
            result.min = (projection < result.min) ? projection : result.min;
            result.max = (projection > result.max) ? projection : result.max;
        }

        return result;
    }

    public static Point[] GetVertices(ColliderBox obb) // find vertices of OBB
    {
        Point[] vert_array = new Point[8]; // OBB always has 8 vertices
        Cube obb_cube = obb.getOBBVertices();

        for (int i = 0; i < obb_cube.vertices.Length; i++)
        {
            vert_array[i].p = obb_cube.vertices[i];
        }

        return vert_array;
    }

    public static Cube GetOBBVertices(ColliderBox obb) // find vertices of OBB as Cube information
    {
        return obb.getOBBVertices();
    }


    public static bool PointInOBB(Point point, ColliderBox obb)
    {
        // move point relative to obb by subtracting the obb center from the point
        Vector3 dir = point.p - obb._center;

        bool Build(Vector3 direction, Vector3 axis, float coord)
        {
            float distance = Vector3.Dot(direction, axis);
            coord /= 2; // convert to half-extent

            if (distance > coord + 0.0001f) // for example obb._xyzLength.x value, but half-extent value!
            {
                return false;
            }
            if (distance < -coord - 0.0001f) // check in other direction
            {
                return false;
            }
            return true;
        }

        Point[] vertices = GetVertices(obb);

        Vector3 x_axis = (vertices[(int)CubeIdx.D].p - vertices[(int)CubeIdx.A].p).normalized;
        Vector3 y_axis = (vertices[(int)CubeIdx.E].p - vertices[(int)CubeIdx.A].p).normalized;
        Vector3 z_axis = (vertices[(int)CubeIdx.B].p - vertices[(int)CubeIdx.A].p).normalized;

        if (!Build(dir, x_axis, obb._xyzLength.x)) return false;
        if (!Build(dir, y_axis, obb._xyzLength.y)) return false;
        if (!Build(dir, z_axis, obb._xyzLength.z)) return false;


        return true;
    }

    public static Line[] GetEdges(ColliderBox obb) // find edges of OBB
    {
        Line[] edges = new Line[12]; // OBB always has 12 edges

        //List<Line> edges = new List<Line>();

        Point[] vertices = GetVertices(obb);

        int i = 0;

        void Build(CubeIdx From, CubeIdx To, int j)
        {
            int fromIndex = (int)From;
            int toIndex = (int)To;
            edges[j] = new Line(vertices[fromIndex].p, vertices[toIndex].p);
        }

        Build(CubeIdx.A, CubeIdx.B, i++); // A -> B
        Build(CubeIdx.A, CubeIdx.D, i++); // A -> D
        Build(CubeIdx.A, CubeIdx.E, i++); // A -> E

        Build(CubeIdx.G, CubeIdx.F, i++); // G -> F
        Build(CubeIdx.G, CubeIdx.C, i++); // G -> C
        Build(CubeIdx.G, CubeIdx.H, i++); // G -> H

        Build(CubeIdx.E, CubeIdx.F, i++); // E -> F
        Build(CubeIdx.E, CubeIdx.H, i++); // E -> H

        Build(CubeIdx.C, CubeIdx.B, i++); // C -> B
        Build(CubeIdx.C, CubeIdx.D, i++); // C -> D

        Build(CubeIdx.B, CubeIdx.F, i++); // B -> F
        Build(CubeIdx.D, CubeIdx.H, i++); // D -> H

        return edges;
    }

    public static List<Plane> GetPlanes(ColliderBox obb) // find planes of OBB
    {
        Vector3 c = obb._center; // center
        Vector3 l = obb._xyzLength / 2; // half-extents
        Point[] vertices = GetVertices(obb);

        // compute directions on all axis
        Vector3 z_dir = (vertices[(int)CubeIdx.B].p - vertices[(int)CubeIdx.A].p).normalized;
        Vector3 x_dir = (vertices[(int)CubeIdx.D].p - vertices[(int)CubeIdx.A].p).normalized;
        Vector3 y_dir = (vertices[(int)CubeIdx.E].p - vertices[(int)CubeIdx.A].p).normalized;

        List<Plane> result = new List<Plane>();

        // build all the planes on the obb
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
    public static Tuple<Point, bool> ClipToPlane(Plane plane, Line line)//, Point outPoint) // clip line to plane
    {
        // TODO: Check if outPoint is needed or not, still not sure

        //Tuple<Point, bool> info = new Tuple<Point, bool>(new Point(), false);
        Tuple<Point, bool> info = new Tuple<Point, bool>(new Point(Vector3.zero), false);

        // ensure that the line and plane intersect
        Vector3 ab = line.end - line.start;
        float nA = Vector3.Dot(plane.normal, line.start);
        float nAB = Vector3.Dot(plane.normal, ab);

        //info.Item1 = new Point();
        //info.Item2 = false;

        if (Util.CMP(nAB, 0.0f)) return info;

        // Find the parameter t along the line at which it intersects the plane
        float t = (plane.distance - nA) / nAB;

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
    /// <param name="edges"></param>
    /// <param name="obb"></param>
    /// <returns></returns>
    public static List<Point> ClipEdgesToOBB(Line[] edges, ColliderBox obb)
    {

        List<Point> result = new List<Point>();

        Point intersection = new Point(Vector3.zero);

        List<Plane> planes = GetPlanes(obb);

        for (int i = 0; i < planes.Count; ++i)
        {
            //Debug.Log("Planes Count:  " + planes.Count);
            for (int j = 0; j < edges.Length; ++j)
            {
                //Debug.Log("Edges Count:  " + edges.Length);
                Tuple<Point, bool> info = ClipToPlane(planes[i], edges[j]);
                intersection = info.Item1;
                //Debug.Log("Point is inside: " + info.Item2 + " Point intersection: " + info.Item1.p);
                if (info.Item2)
                {
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
            return info; // No penetration
        }

        float len1 = i1.max - i1.min;
        float len2 = i2.max - i2.min;
        float min = Mathf.Min(i1.min, i2.min);
        float max = Mathf.Max(i1.max, i2.max);
        float length = max - min;

        info.Item2 = (i2.min < i1.min);
        info.Item1 = (len1 + len2) - length;

        return info;
    }


    public static CollisionManifold FindCollisionFeatures(ColliderBox obb1, ColliderBox obb2)
    {
        CollisionManifold result = new CollisionManifold();
        //CollisionManifold.Reset(result);
        result.Reset();

        // First, make a quick collision test on spheres within the OBBs, if they don't collide => OBBs dont collide
        Sphere s1 = new Sphere(new Point(obb1._center), Vector3.Magnitude(obb1._xyzLength / 2) + 0.1f);
        Sphere s2 = new Sphere(new Point(obb2._center), Vector3.Magnitude(obb2._xyzLength / 2) + 0.1f);

        if (!SphereSphere(s1, s2)) return result;

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

        for (int i = 0; i < test.Length; ++i) // axis.Count = 15
        {
            if (axis[i].x < 0.000001f) test[i].x = 0.0f;
            if (test[i].y < 0.000001f) test[i].y = 0.0f;
            if (test[i].z < 0.000001f) test[i].z = 0.0f;
            if (Vector3.Magnitude(test[i]) * Vector3.Magnitude(test[i]) < 0.001f) continue;

            Tuple<float, bool> pntrtion_info = PenetrationDepth(obb1, obb2, test[i], shouldFlip);
            float depth = pntrtion_info.Item1;
            shouldFlip = pntrtion_info.Item2;
            //Debug.Log("shouldFlip" + shouldFlip);
            //if (depth < 0.0f) return result; // if penetration depth < 0.0f 
            if (depth <= 0.001f)
            {
                Debug.Log("DEPTH: " + depth);
                return result;
            }


            else if (depth < result.depth)
            {
                if (shouldFlip) test[i] = test[i] * -1.0f;

                result.depth = depth;
                hitNormal = test[i];
                //Debug.Log("hitNormal" + hitNormal);
            }

        }

        //Debug.Log("PenetrationDepth: " + result.depth);
        //Debug.Log("shouldFlip: " + shouldFlip);
        //Debug.Log("COLLIDING2: " + result.colliding);

        //if (Util.CMP(hitNormal.magnitude, 0.0f))// return result;        //if (hitNormal.magnitude <= 0.1f)
        //{
        //    Debug.Log("RETURN hitNormal.magnitude: " + hitNormal.magnitude);
        //    return result;
        //}

        Vector3 axs = hitNormal.normalized;

        List<Point> list1 = ClipEdgesToOBB(GetEdges(obb2), obb1);
        List<Point> list2 = ClipEdgesToOBB(GetEdges(obb1), obb2);

        result.contacts = new List<Vector3>();
        result.depths = new List<Vector3>();

        foreach (Point point in list1)
        {
            result.contacts.Add(point.p);
            result.depths.Add(point.p);
        }
        foreach (Point point in list2)
        {
            result.contacts.Add(point.p);
            result.depths.Add(point.p);
        }


        Interval interval = GetInterval(obb1, axs);
        float depth_distance_points = (interval.max - interval.min) * 0.5f; // depth points after intersection inside obb
        float contact_points = (interval.max - interval.min) * 0.5f - result.depth; // points of intersection at the surface

        Vector3 pointOnPlane1 = obb1._center + axs * depth_distance_points;
        Vector3 pointOnPlane2 = obb1._center + axs * contact_points;

        for (int i = result.contacts.Count - 1; i >= 0; --i)
        {
            Vector3 contact = result.contacts[i];
            result.contacts[i] = contact + (axs * Vector3.Dot(axs, pointOnPlane2 - contact));
            result.depths[i] = contact + (axs * Vector3.Dot(axs, pointOnPlane1 - contact));

            //result.depths.Add(contact + (axs * Vector3.Dot(axs, pointOnPlane1 - contact)));

            // This bit is in the "There is more" section of the book
            for (int j = result.contacts.Count - 1; j > i; --j)
            {
                float magnitude = Vector3.Magnitude(result.contacts[j] - result.contacts[i]);
                if (magnitude * magnitude < 0.0001f)
                {
                    result.contacts.RemoveAt(j);
                    result.depths.RemoveAt(j);
                    break;
                }
            }

        }

        foreach (Vector3 cp in result.contacts)
        {
            result.avg_contact += cp;
        }
        result.avg_contact /= result.contacts.Count;

        //result.avg_depth = result.avg_contact + result.depth * result.normal;

        foreach (Vector3 dp in result.depths)
        {
            result.avg_depth += dp;
        }
        result.avg_depth /= result.depths.Count;

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
        //Debug.Log("longSpan: " + longSpan);
        //Debug.Log("overlapSpan: " + overlapSpan);

        bool colliding = longSpan < overlapSpan;

        return colliding;
    }


}
