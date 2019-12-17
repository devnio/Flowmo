using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoftStructure : MonoBehaviour
{
    [HideInInspector]
    public Vector3 prevCenterOfMass; // used for the collision point q (the one to shift towards)
    [HideInInspector]
    public Vector3 centerOfMass;
    [HideInInspector]
    public BaseCollider Collider;

    public List<Constraint> constraints;

    private Vector3 gravityAcceleraiton;
    public bool UseGravity = true;
    public Vector3 acceleration;
    public Vector3 velocity;

    public Particle[] particles; // relative to center of mass
    public DistTuple[] distTuples;
    public PointTuple[] pointTuples;

    public bool ShowVelocityArrows;
    private GameObject[] VelocityArrows_DB;

    // Colliders of each particle
    [HideInInspector]
    public SphereCollider[] sphereColliders;
    public float sphereCollRadius = 0.3f;

    private bool dragMode; // interaction
    public Transform[] bones;
    private Quaternion[] initialBoneOrientations;

    private void Start()
    {
        // Add object to simulation
        VerletSimulation.Instance.AddSoftStructure(this);

        // Initialize acceleration
        gravityAcceleraiton = new Vector3(0, -9.81f, 0);
        if (this.UseGravity) acceleration += gravityAcceleraiton;

        this.InitializeConstraints();
    }


    /// <summary>
    /// After cloth has been generated the constraints have to be initialized from the distance tuples.
    /// </summary>
    public void InitializeConstraints()
    {
        // Instantiate debugging arrows
        if (this.ShowVelocityArrows)
        {
            this.VelocityArrows_DB = new GameObject[particles.Length];
            for (int i = 0; i < particles.Length; i++)
            {
                this.VelocityArrows_DB[i] = (GameObject)Instantiate(Resources.Load("VelocityArrow"));
                this.VelocityArrows_DB[i].SetActive(false);
            }
        }

        // For visualizer
        //E_pointsTransformedInLocalSpace = true;

        // Prev pos
        foreach (Particle p in particles)
        {
            //p.position = this.transform.TransformPoint(p.position);
            p.prevPosition = p.position;
            p.velocity = Vector3.zero;
        }

        // Transform to world point constraints
        //foreach (PointTuple pTup in this.pointTuples)
        //{
        //    pTup.pos = this.transform.TransformPoint(pTup.pos);
        //}

        // Init sphere colliders
        this.CreateSphereColliders();

        // Add Tetrahederon and Bounding constraints 
        constraints = new List<Constraint>();
        constraints.Add(new DistanceConstraint(particles, distTuples));
        constraints.Add(new PointConstraint(particles, pointTuples));

        this.initialBoneOrientations = new Quaternion[this.bones.Length];
        for (int i = 0; i < this.bones.Length; i++)
        {
            this.initialBoneOrientations[i] = this.bones[i].rotation;
        }
    }

    public void UpdateStep(float dt)
    {
        if (!this.dragMode)
        {
            // Integration
            foreach (Particle p in particles)
            {
                Vector3 temp = p.position;
                p.position += p.position - p.prevPosition + (acceleration * dt * dt) * p.invMass;
                p.prevPosition = temp;

                // Update velocity used (only for damping now)
                p.velocity = p.position - p.prevPosition;
            }
        }
        this.UpdateSphereColliderPos();
        this.UpdateSoftStructureBones();
    }

    public void SatisfyConstraints()
    {
        if (!this.dragMode)
        {
            // Distance constraints
            this.constraints[0].ConstraintUpdate();
            // Point constraints
            this.constraints[1].ConstraintUpdate();
        }
    }

    // Updates the position of the colliders based on the associated particle.
    public void UpdateSphereColliderPos()
    {
        for (int i = 0; i < this.particles.Length; i++)
        {
            this.sphereColliders[i].UpdateColliderPose(Vector3.zero);
        }
    }

    // Update the orientation of each bone.
    public void UpdateSoftStructureBones()
    {
        // Particles
        Particle p0 = this.particles[0];
        Particle p1 = this.particles[1];
        Particle p2 = this.particles[2];
        Particle p3 = this.particles[3];
        Particle p4 = this.particles[4];
        Particle p5 = this.particles[5];
        Particle p6 = this.particles[6];

        // Constant constraint points
        Vector3 point0 = this.pointTuples[0].pos;
        Vector3 point1 = this.pointTuples[1].pos;
        Vector3 point2 = this.pointTuples[2].pos;
        Vector3 point3 = this.pointTuples[3].pos;
        Vector3 point4 = this.pointTuples[4].pos;
        Vector3 point5 = this.pointTuples[5].pos;

        // The initial constraint vectors
        Vector3 constVect0 = point0 - p0.position;  // p0.position assumed to be fixed
        Vector3 constVect1 = point1 - point0;
        Vector3 constVect2 = point2 - point1;
        Vector3 constVect3 = point3 - point2;
        Vector3 constVect4 = point4 - point1;
        Vector3 constVect5 = point5 - point4;

        // The current vectors (during simulation they move)
        Vector3 currVect0 = p1.position - p0.position;
        Vector3 currVect1 = p2.position - p1.position;
        Vector3 currVect2 = p3.position - p2.position;
        Vector3 currVect3 = p4.position - p3.position;
        Vector3 currVect4 = p5.position - p2.position;
        Vector3 currVect5 = p6.position - p5.position;

        this.bones[0].rotation = Quaternion.FromToRotation(constVect0, currVect0) * this.initialBoneOrientations[0];

        this.bones[1].rotation = Quaternion.FromToRotation(constVect4, currVect4) * this.initialBoneOrientations[1];
        this.bones[2].rotation = Quaternion.FromToRotation(constVect5, currVect5) * this.initialBoneOrientations[2];

        this.bones[3].rotation = Quaternion.FromToRotation(constVect2, currVect2) * this.initialBoneOrientations[3];
        this.bones[4].rotation = Quaternion.FromToRotation(constVect3, currVect3) * this.initialBoneOrientations[4];

    }

    //public void UpdateSoftStructureBones()
    //{
    //    // TODO
    //    // Particles
    //    Particle p0 = this.particles[0];
    //    Particle p1 = this.particles[1];
    //    Particle p2 = this.particles[2];

    //    Vector3 point0 = this.pointTuples[0].pos;
    //    Vector3 point1 = this.pointTuples[1].pos;

    //    Vector3 constVect0 = point0 - p0.position;
    //    Vector3 constVect1 = point1 - point0;

    //    Vector3 currVect0 = p1.position - p0.position;
    //    Vector3 currVect1 = p2.position - p1.position;

    //    this.bone1.rotation = Quaternion.FromToRotation(constVect0, currVect0);
    //    this.bone2.rotation = Quaternion.FromToRotation(constVect1, currVect1);
    //}

    // Interaction mode
    public void ActivateDragMode(bool state)
    {
        this.dragMode = state;
    }

    public bool IsInDragMode() { return this.dragMode; }

    // Create a sphere collider for each particle
    private void CreateSphereColliders()
    {
        int len = this.particles.Length;
        this.sphereColliders = new SphereCollider[len];

        for (int i = 0; i < len; i++)
        {
            SphereCollider sp = this.gameObject.AddComponent<SphereCollider>();

            sp.isSingleParticle = true;
            sp.AssignSingleParticle(this.particles[i]);
            sp.Radius = this.sphereCollRadius;

            this.sphereColliders[i] = sp;
        }
        UpdateSphereColliderPos();
    }

    //------------------------
    // Drawing
    //------------------------
    private bool E_pointsTransformedInLocalSpace = false;
    private void OnDrawGizmos()
    {
        if (particles != null)
        {
            float percent = 0f;
            float step = 1f / particles.Length;
            float maxVelocityParticle_val = float.MinValue;
            int maxVelocityParticle_idx = 0;
            // Draw Particles and Constraints

            Color color = Color.red;
            for (int i = 0; i < this.particles.Length; i++)
            {
                Particle p = this.particles[i];

                Vector3 pLocal = p.position;
                if (!E_pointsTransformedInLocalSpace)
                {
                    pLocal = this.transform.TransformPoint(p.position);
                }

                color = Color.Lerp(Color.magenta, Color.yellow, percent);
                percent += step;
                color.a = 0.75f;
                Gizmos.color = color;

                Gizmos.DrawSphere(pLocal, 0.2f);

                color = Color.blue;
                color.a = 0.2f;
                Gizmos.color = color;

                if (E_pointsTransformedInLocalSpace) Gizmos.DrawSphere(p.prevPosition, 0.1f);

                // Find biggest velocity for normalizing arrows
                if (this.ShowVelocityArrows)
                {
                    float sqrDist = (p.position - p.prevPosition).sqrMagnitude;
                    if (sqrDist > maxVelocityParticle_val)
                    {
                        maxVelocityParticle_val = sqrDist;
                        maxVelocityParticle_idx = i;
                    }
                }
            }

            // Draw Distance Constraints
            Gizmos.color = Color.magenta;
            if (this.distTuples != null)
            {
                foreach (DistTuple dConst in this.distTuples)
                {
                    Vector3 p1Local = this.particles[dConst.Item1].position;
                    Vector3 p2Local = this.particles[dConst.Item2].position;
                    if (!E_pointsTransformedInLocalSpace)
                    {
                        p1Local = this.transform.TransformPoint(p1Local);
                        p2Local = this.transform.TransformPoint(p2Local);
                    }
                    Gizmos.DrawLine(p1Local, p2Local);
                }
            }

            if (this.pointTuples != null)
            {
                foreach (PointTuple pTup in this.pointTuples)
                {
                    Gizmos.color = Color.cyan;
                    Vector3 pos = pTup.pos;
                    Vector3 partPoint = this.particles[pTup.p].position;

                    if (!E_pointsTransformedInLocalSpace)
                    {
                        pos = this.transform.TransformPoint(pos);
                        partPoint = this.transform.TransformPoint(partPoint);
                    }

                    Gizmos.DrawWireSphere(pos, 0.15f);
                    Gizmos.DrawLine(pos, partPoint);
                }
            }

            // Draw velocity arrows
            if (this.E_pointsTransformedInLocalSpace && this.ShowVelocityArrows)
            {
                float maxVel = (this.particles[maxVelocityParticle_idx].position - this.particles[maxVelocityParticle_idx].prevPosition).magnitude;

                for (int i = 0; i < this.particles.Length; i++)
                {
                    Particle p = this.particles[i];
                    Vector3 dir = p.position - p.prevPosition;
                    float vel = dir.magnitude;

                    if (float.IsNaN(vel) || (maxVel + float.Epsilon >= 0 && maxVel - float.Epsilon <= 0))
                    {
                        this.VelocityArrows_DB[i].SetActive(false);
                        continue;
                    }
                    this.VelocityArrows_DB[i].SetActive(true);

                    this.VelocityArrows_DB[i].transform.position = p.position;
                    this.VelocityArrows_DB[i].transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                    this.VelocityArrows_DB[i].transform.localScale = Vector3.one * (vel / maxVel * 0.3f);
                }
            }

        }
    }
}
