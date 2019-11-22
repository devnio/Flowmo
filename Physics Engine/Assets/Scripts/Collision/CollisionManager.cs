using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionManager : Singleton<CollisionManager>
{
    //---------------------------------
    // Debugging
    //---------------------------------
    public bool debug;
    public GameObject SATDebugCubePrefab;
    private GameObject SATDebugCube;

    //---------------------------------
    // Collision Fields
    //---------------------------------
    public List<BaseCollider> Colliders;
    public Dictionary<int, Vector3> CachedSeparatingAxis;
    
    // Collision resolution values of current check
    // Be careful to always set these values before using them.
    // Obb - Obb
    private Vector3 currentMinPenetrationAxis;
    private float currentMinPenetrationDistance;

    // Obb - Sphere
    private Vector3 currentClosestPointOnObb;

    private void Start()
    {
        // Initialize stuff
        Logger.Instance.DebugInfo("CollisionManager: Started");
        CachedSeparatingAxis = new Dictionary<int, Vector3>();

        if (debug)
        {
            SATDebugCube = Instantiate(SATDebugCubePrefab);
            SATDebugCube.SetActive(false);
        }
    }

    public void AddCollider(BaseCollider Collider)
    {
        if (Colliders == null)
        {
            Colliders = new List<BaseCollider>();
        }
        Colliders.Add(Collider);

        Logger.Instance.DebugInfo("Added Collider " + Collider.Id + " to CollisionManager!");
    }

    public void DetectCollisions()
    {
        // TODO: Spatial structure.
        int count = Colliders.Count;
        for (int i = 0; i < count; i++)
        {
            for (int j = i + 1; j < count; j++)
            {
                // OBB vs OBB
                if (Colliders[i] as ColliderBox != null && Colliders[j] as ColliderBox != null)
                {
                    if (AreOBBsColliding((ColliderBox)Colliders[i], (ColliderBox)Colliders[j]))
                    {
                        Logger.Instance.DebugInfo("Collision happened [OBB vs OBB]: " + 
                                                    Colliders[i].Id + " - " + 
                                                    Colliders[j].Id +" !", 
                                                    "COLLISION_MANAGER");
                    }
                }

                // SPHERE vs SPHERE
                else if (Colliders[i] as SphereCollider != null && Colliders[j] as SphereCollider != null)
                {
                    if (AreSpheresColliding((SphereCollider)Colliders[i], (SphereCollider)Colliders[j]))
                    {
                        Logger.Instance.DebugInfo("Collision happened [Sphere vs Sphere]: " +
                                                    Colliders[i].Id + " - " +
                                                    Colliders[j].Id + " !",
                                                    "COLLISION_MANAGER");
                    }
                }

                // OBB vs SPHERE
                else 
                {
                    ColliderBox b = Colliders[i] as ColliderBox;
                    SphereCollider s = Colliders[j] as SphereCollider;

                    if (b == null)
                    {
                        b = Colliders[j] as ColliderBox;
                        s = Colliders[i] as SphereCollider;
                    }

                    if (b != null && s != null)
                    {
                        if (AreSphereOBBColliding(b, s))
                        {
                            Logger.Instance.DebugInfo("Collision happened [OBB vs Sphere]: " +
                                                        Colliders[i].Id + " - " +
                                                        Colliders[j].Id + " !",
                                                        "COLLISION_MANAGER");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Get unique ID form the colliders is used in the CachedSeparatingAxis 
    /// dictionary to retrieve the separating axis.
    /// </summary>
    /// <param name="id1"></param>
    /// <param name="id2"></param>
    /// <returns></returns>
    public int GetCachedSeparatingAxisID(int id1, int id2)
    {
        // get sorted values
        int min = Mathf.Min(id1, id2);
        int max = Mathf.Max(id1, id2);

        // iterate over 
        int step = 0;
        for(int i = 1; i <= min; i++)
        {
            step += BaseCollider.GlobalIdCounter - i;
        }
        return step - min + max - 1;
    }


    /// <summary>
    /// Check if the previously detected SA is still a valid SA.
    /// </summary>
    /// <param name="b1"></param>
    /// <param name="b2"></param>
    /// <param name="ax"></param>
    /// <returns></returns>
    public bool CheckPreviousSeparatingAxis(ColliderBox b1, ColliderBox b2)
    {
        int cacheId = this.GetCachedSeparatingAxisID(b1.Id, b2.Id);

        // Check if this tuple is already cached
        if (this.CachedSeparatingAxis.ContainsKey(cacheId))
        {
            // Check if the cached axis is still separating the two colliders
            if (SeparatingAxisCheck(b1, b2, this.CachedSeparatingAxis[cacheId], false))
            {
                return true;
            }
            else
            {
                this.CachedSeparatingAxis.Remove(cacheId);
                return false;
            }
        }

        // No previous cached value
        return false;
    }


    //---------------------------------
    // Collision Methods
    //---------------------------------
    /// <summary>
    /// Separate the 2 objects from the information got from the collision.
    /// If ob2 is null only ob1 is moved out. Be careful to pass the right currPoint and projPoint.
    /// </summary>
    private void SeparateParticleObjects(ParticleObject ob1, Vector3 currPoint, Vector3 projPoint, ParticleObject ob2 = null)
    {
        // Update particles to move out
        float[] c1 = this.ComputeParticlesCoefficients(ob1, currPoint);
        float lambda1 = this.ComputeLambda(c1);
        if (ob2 == null)
        {
            Logger.Instance.DebugInfo("1 Dynamic Sphere", "COLLISION");
            // Update the dynamic object
            for (int i = 0; i < 4; i++)
            {
                ob1.particles[i].position = ob1.particles[i].position + lambda1 * c1[i] * (projPoint - currPoint);
            }
            Logger.Instance.DebugParticleCoefficients(currPoint, c1, "COEFFICIENTS");
        }
        else
        {
            // In case both are dynamic have to push them in opposite directions.
            Logger.Instance.DebugInfo("2 Dynamic Spheres", "COLLISION");
            float[] c2 = this.ComputeParticlesCoefficients(ob1, projPoint);
            float lambda2 = this.ComputeLambda(c2);

            // Update the dynamic object
            for (int i = 0; i < 4; i++)
            {
                ob1.particles[i].position = ob1.particles[i].position + lambda2 * c1[i] * (projPoint - currPoint) * 0.5f;
                ob2.particles[i].position = ob2.particles[i].position + lambda2 * c2[i] * (currPoint - projPoint) * 0.5f;
            }
            Logger.Instance.DebugParticleCoefficients(currPoint, c1, "COEFFICIENTS 1");
            Logger.Instance.DebugParticleCoefficients(projPoint, c2, "COEFFICIENTS 2");
        }
    }

    /// <summary>
    /// Compute coefficients to use for moving the particles (SEE JAKOBSEN paper).
    /// </summary>
    /// <param name="ob"></param>
    /// <param name="currPoint"></param>
    /// <returns></returns>
    private float[] ComputeParticlesCoefficients(ParticleObject ob, Vector3 currPoint)
    {
        float[] c = new float[ob.particles.Length];
        float sum = 0;

        foreach (Particle p in ob.particles)
        {
            sum += (p.position - currPoint).magnitude;
        }

        for (int i = 0; i < c.Length; i++)
        {
            c[i] = 1 - ((ob.particles[i].position - currPoint).magnitude / sum);
        }
        return c;
    }

    /// <summary>
    /// Computes the lambda value from the coefficients of the particles (SEE JAKOBSEN paper)
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    private float ComputeLambda(float[] c)
    {
        float sum = 0f;
        for (int i = 0; i < c.Length; i++)
        {
            sum += c[i] * c[i];
        }

        return 1f / sum;
    }


    //---------------------------------
    // OBB vs Sphere
    //---------------------------------
    public bool AreSphereOBBColliding(ColliderBox b, SphereCollider s)
    {
        Tuple<Vector3, bool> infoClosestPoint = Util.FindClosestPoint(b, s);
        this.currentClosestPointOnObb = infoClosestPoint.Item1;
        bool sphereInsideObb = infoClosestPoint.Item2;

        float distSphereObbPoint = (this.currentClosestPointOnObb - s._center).sqrMagnitude;

        bool collision = distSphereObbPoint < s.SqrRadius;
        if (collision || sphereInsideObb) this.CollisionResolutionObbSphere(b, s, sphereInsideObb);

        return collision || sphereInsideObb;
    }

    public void CollisionResolutionObbSphere(ColliderBox b, SphereCollider s, bool sphereInsideObb)
    {
        Vector3 temp = this.currentClosestPointOnObb - s._center;
        //float mtd = s.Radius - temp.magnitude;
        float dirMult = sphereInsideObb ? -1 : 1; 
        Vector3 dir = temp.normalized * dirMult;

        Vector3 currPoint = s._center + dir * s.Radius;
        Vector3 projPoint = this.currentClosestPointOnObb;

        this.SeparateParticleObjects(b.GetParticleObject(), projPoint, currPoint);
        //this.SeparateParticleObjects(s.GetParticleObject(), currPoint, projPoint);
    }

    // TODO: Add function for moving particle objects between obb and sphere

    //---------------------------------
    // Sphere vs Sphere
    //---------------------------------
    private bool AreSpheresColliding(SphereCollider sp1, SphereCollider sp2)
    {
        // Compute distance vector
        Vector3 sp1_sp2 = sp1._center - sp2._center;
        float longSpan = sp1_sp2.magnitude;
        float overlapSpan = sp1.Radius + sp2.Radius;

        bool colliding = longSpan < overlapSpan;

        // Resolve Collision
        if (colliding)
        {
            this.CollisionResolutionSphere(sp1, sp2, overlapSpan - longSpan);
        }

        return colliding;
    }

    private void CollisionResolutionSphere(SphereCollider sp1, SphereCollider sp2, float overlapDistance)
    {
        // Assuming sp1 is dynamic.
        Vector3 dir = sp1._center - sp2._center;
        dir.Normalize();

        Vector3 currPoint = sp1._center + dir * sp1.Radius;
        Vector3 displace = dir * overlapDistance;
        Vector3 projPoint = currPoint + displace;

        if (!sp1.IsStatic() && !sp2.IsStatic())
        {
            this.SeparateParticleObjects(sp1.GetParticleObject(), currPoint, projPoint, sp2.GetParticleObject());
        }
        else
        {
            if (!sp1.IsStatic()) this.SeparateParticleObjects(sp1.GetParticleObject(), currPoint, projPoint);
            else if (!sp2.IsStatic()) this.SeparateParticleObjects(sp2.GetParticleObject(), projPoint, currPoint);
        }
    }

 
    //---------------------------------
    // OBB vs OBB
    //---------------------------------
    /// <summary>
    /// Checks if there is a separating axis between the two colliders.
    /// Returns true if there is a separating axis (no collision).
    /// </summary>
    /// <param name="cacheAxis"> If the given axis is separating cache this in the system (faster for next round).
    /// This is False if we need to convalidate the previous stored one (don't add 2 times).</param>
    /// <param name="cacheColResolution"> When active we store the MTV and MTD. </param>
    /// <param name="ax"></param>
    /// <returns></returns>
    private bool SeparatingAxisCheck(ColliderBox b1, ColliderBox b2, Vector3 ax, bool cacheAxis=true, bool cacheColResolution = false)
    {
        if (ax == Vector3.zero) return false;

        ax.Normalize();

        Cube c1 = b1.cube;
        Cube c2 = b2.cube;

        float c1Max = float.MinValue;
        float c1Min = float.MaxValue;
        float c2Max = float.MinValue;
        float c2Min = float.MaxValue;

        // Project points from cubes to ax. Find extreme points in the axis.
        for (int i = 0; i < 8; i++)
        {
            // Project point to axis;
            float c1Proj = Vector3.Dot(c1.vertices[i], ax);
            float c2Proj = Vector3.Dot(c2.vertices[i], ax);

            c1Max = Mathf.Max(c1Max, c1Proj);
            c1Min = Mathf.Min(c1Min, c1Proj);
            c2Max = Mathf.Max(c2Max, c2Proj);
            c2Min = Mathf.Min(c2Min, c2Proj);
        }

        var max = Mathf.Max(c1Max, c2Max);
        var min = Mathf.Min(c1Min, c2Min);

        float longSpan = max - min;
        float overlapSpan = c1Max - c1Min + c2Max - c2Min;

        bool noCollision = longSpan > overlapSpan;
        // if no collision happened (found a separating axis) cache the separating axis
        if (noCollision && cacheAxis && !cacheColResolution) CachedSeparatingAxis.Add(GetCachedSeparatingAxisID(b1.Id, b2.Id), ax); 

        // we already know a collision happened and try to find min penetration axis for projection
        if (cacheColResolution && !noCollision)
        {
            float val = overlapSpan - longSpan;
            if (val >= 0 && val < this.currentMinPenetrationDistance && ax != Vector3.zero)
            {
                this.currentMinPenetrationDistance = val;
                this.currentMinPenetrationAxis = ax;
                Debug.Log("FOUND MIN AXIS");
                Debug.Log(val);
                Debug.Log(ax);
            }
        }

        return noCollision;
    }
    
    /// <summary>
    /// Return true if the colliderboxes are collliding
    /// </summary>
    /// <param name="b1"></param>
    /// <param name="b2"></param>
    /// <returns></returns>
    public bool AreOBBsColliding(ColliderBox b1, ColliderBox b2, bool cacheColResponse=false)
    {
        // Return no collision if the previous SA is still valid.
        if (CheckPreviousSeparatingAxis(b1, b2)) return false;
        //Init
        this.currentMinPenetrationDistance = float.MaxValue;
        //this.currentMinPenetrationAxis = Vector3.zero;

        Cube c1 = b1.cube;
        Cube c2 = b2.cube;

        // Get axis from first cube
        Vector3 c1axis1 = c1.vertices[(int)CubeIdx.B] - c1.vertices[(int)CubeIdx.A];
        Vector3 c1axis2 = c1.vertices[(int)CubeIdx.D] - c1.vertices[(int)CubeIdx.A];
        Vector3 c1axis3 = c1.vertices[(int)CubeIdx.E] - c1.vertices[(int)CubeIdx.A];

        // Get axis from second cube
        Vector3 c2axis1 = c2.vertices[(int)CubeIdx.B] - c2.vertices[(int)CubeIdx.A];
        Vector3 c2axis2 = c2.vertices[(int)CubeIdx.D] - c2.vertices[(int)CubeIdx.A];
        Vector3 c2axis3 = c2.vertices[(int)CubeIdx.E] - c2.vertices[(int)CubeIdx.A];

        Vector3[] axis = { c1axis1, c1axis2, c1axis3, c2axis1, c2axis2, c2axis3 };

        // Check 6 axis from 2 cubes
        foreach (Vector3 ax in axis)
        {
            if (SeparatingAxisCheck(b1, b2, ax, true, cacheColResponse)) return false;
        }

        // Check 9 axis given by cross product between 2 cubes
        for (int i = 0; i < 3; i++)
        {
            Vector3 ax1 = Vector3.Cross(axis[i], axis[3]);
            if (SeparatingAxisCheck(b1, b2, ax1, true, cacheColResponse)) return false;
            Vector3 ax2 = Vector3.Cross(axis[i], axis[4]);
            if (SeparatingAxisCheck(b1, b2, ax2, true, cacheColResponse)) return false;
            Vector3 ax3 = Vector3.Cross(axis[i], axis[5]);
            if (SeparatingAxisCheck(b1, b2, ax3, true, cacheColResponse)) return false;
        }

        // Collision Resolution
        if (!cacheColResponse) CollisionResolutionOBB(b1, b2);

        // No separating axis found -> collision happened.
        return true;
    }

    /// <summary>
    /// If collision happened project the ParticleObject back to prevCenterMass.
    /// </summary>
    /// <param name="b1"></param>
    /// <param name="b2"></param>
    public void CollisionResolutionOBB(ColliderBox b1, ColliderBox b2)
    {
        // TODO: for now only try solving between dynamic and static obj.
        ParticleObject dynamicObj = b1.GetComponent<ParticleObject>();
        ColliderBox dynamicB = b1;
        ColliderBox staticB = b2;
        if (b2.GetComponent<ParticleObject>().objectType == ObjectType.DYNAMIC) {
            dynamicObj = b2.GetComponent<ParticleObject>();
            staticB = b1;
            dynamicB = b2;
        }

        // Find min penetration axis
        AreOBBsColliding(b1, b2, true);
        ShowCurrentSeparatingPlane();
        // =====
        //VerletSimulation.Instance.StopSimulation();
        // =====

        Vector3 currPoint = dynamicObj.centerOfMass;
        Vector3 projPoint = dynamicObj.centerOfMass + this.currentMinPenetrationAxis.normalized * this.currentMinPenetrationDistance;

        // Update particles to move out
        float[] c = new float[4];
        float sum = 0;

        foreach(Particle p in dynamicObj.particles)
        {
            sum += (p.position - currPoint).magnitude;
        }

        for (int i = 0; i < 4; i++)
        {
            c[i] = (dynamicObj.particles[i].position - currPoint).magnitude / sum;
        }

        Logger.Instance.DebugInfo("C1: " + c[0] + ", C2: " + c[1] + ", C3: " + c[2] + ", C3: " + c[3], "C1-C4");
        Logger.Instance.DebugInfo((c[0] + c[1] + c[2] + c[3]).ToString(), "SUM C1-C4");
        Logger.Instance.DebugInfo(currPoint.ToString(), "CURRENTE POINT: P ");
        Vector3 linearComb = c[0] * dynamicObj.particles[0].position
            + c[1] * dynamicObj.particles[1].position
            + c[2] * dynamicObj.particles[2].position
            + c[3] * dynamicObj.particles[3].position;
        Logger.Instance.DebugInfo(linearComb.ToString(), "LINEAR COMB P=C1X1 + C2X2 + C3X3 + C4X4 = ");


        float lambda = 1f / (c[0] * c[0] + c[1] * c[1] + c[2] * c[2] + c[3] * c[3]);

        for (int i = 0; i < 4; i++)
        {
            dynamicObj.particles[i].position = dynamicObj.particles[i].position + lambda * c[i] * (projPoint - currPoint);
        }

        Logger.Instance.DebugInfo("Updated particles, min axis dist: " + this.currentMinPenetrationDistance + ", min axis: " + this.currentMinPenetrationAxis, "INFO COLLISION RESOLUTION");
        Debug.Log(this.currentMinPenetrationDistance);
        Debug.Log(this.currentMinPenetrationAxis);
    }

    //---------------------------------
    // Drawing & Debugging
    //---------------------------------
    private void ShowCurrentSeparatingPlane()
    {
        SATDebugCube.SetActive(true);
        SATDebugCube.transform.rotation = Quaternion.FromToRotation(Vector3.right, this.currentMinPenetrationAxis);
    }

    private void OnDrawGizmos()
    {
        Color col = Color.cyan;
        col.a = 0.7f;
        Gizmos.color = col;

        Gizmos.DrawSphere(this.currentClosestPointOnObb, 0.3f);
    }
}
