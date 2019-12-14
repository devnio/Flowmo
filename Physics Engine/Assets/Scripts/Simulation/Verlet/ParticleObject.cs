using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum ObjectType
{
    DYNAMIC,
    KINEMATIC
}

public class ParticleObject : MonoBehaviour
{
    // Take direction of 2 particles and use for orienting
    private Quaternion tetraCoordinateFrame;
    private Quaternion initialOrientation;

    [HideInInspector]
    public Vector3 prevCenterOfMass; // used for the collision point q (the one to shift towards)
    [HideInInspector]
    public Vector3 centerOfMass;
    [HideInInspector]
    public BaseCollider Collider;
    [HideInInspector]
    public bool integrateThisFrame = true;

    public List<Constraint> constraints;

    public ObjectType objectType;

    private Vector3 gravityAcceleraiton;
    public bool UseGravity = true;
    public Vector3 acceleration;
    public Vector3 velocity;

    public Particle[] particles; // relative to center of mass
    public DistTuple[] distTuples;
    public PointTuple[] pointTuples;

    private Vector3 frictionVector;
    public bool ShowVelocityArrows;
    private GameObject[] VelocityArrows_DB;

    private void Start()
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

        // Check if there is a collider
        if (this.gameObject.GetComponent<BaseCollider>() != null)
        {
            this.Collider = this.gameObject.GetComponent<BaseCollider>();
            this.Collider.AssignParticleObject(this);
        }

        // Initialize prev pos as current one and invmass
        foreach (Particle p in particles) {
            p.position = this.transform.TransformPoint(p.position);
            p.prevPosition = p.position - velocity / 10f;

            // For visualizer
            E_pointsTransformedInLocalSpace = true;
        }

        // Add Tetrahederon and Bounding constraints 
        constraints = new List<Constraint>();
        constraints.Add(new DistanceConstraint(particles, distTuples));
        constraints.Add(new PointConstraint(particles, pointTuples));
        //constraints.Add(new BoundConstraint(particles, new Vector3(10, 5, 10)));

        // Initialize center position
        this.centerOfMass = this.ComputeCenterOfMass();
        this.SetOrientationAxis();  //TODO: add back -> create GummyObject

        // Add object to simulation
        VerletSimulation.Instance.AddParticleObject(this);

        // Initialize acceleration
        gravityAcceleraiton = new Vector3(0, -9.81f, 0);
        if (this.UseGravity) acceleration += gravityAcceleraiton;

        //// Move Particle
        //this.particles[2].position += new Vector3(1,1,0) * 3f;
        //this.particles[1].position += new Vector3(1,1,0) * 1.5f;
    }

    /// <summary>
    /// Resets implicit velocity by changing prevPos. Not used now.
    /// </summary>
    public void ResetVelocity()
    {
        foreach (Particle p in particles)
        {
            p.prevPosition = p.position;
        }
    }

    public void UpdateStep(float dt)
    {
        if (this.objectType == ObjectType.DYNAMIC)
        {
            if (this.integrateThisFrame)
            {
                // Integration
                foreach (Particle p in particles)
                {
                    Vector3 temp = p.position;
                    p.position += p.position - p.prevPosition + (acceleration * dt * dt) * p.invMass;
                    p.prevPosition = temp;

                }
                this.UpdateGameObjectPose(); 
            }
            else integrateThisFrame = true;
        }
    }

    public void SatisfyConstraints(int iterations = 1)
    {
        foreach (Constraint c in constraints)
        {
            c.ConstraintUpdate();
        }
    }

    public void SetFrictionVector(Vector3 frictionVector)
    {
        this.frictionVector = frictionVector;
    }

    public void UpdateGameObjectPose()
    {
        this.prevCenterOfMass = this.centerOfMass;
        this.centerOfMass = ComputeCenterOfMass();
        this.transform.position = this.centerOfMass;

        // bring object to coordinate frame of tetrahedron
        this.UpdateGameObjectOrientation(); // TODO: when merging add this back -> make a new GummyObject

        // move collider on spot based on current transform
        if (this.Collider != null)
        {
            this.Collider.UpdateColliderPose(Vector3.zero);
        }
    }

    private Vector3 ComputeCenterOfMass(bool transformToLocalFirst = false)
    {
        Vector3 center = Vector3.zero;
        foreach (Particle p in this.particles)
        {
            Vector3 pos = p.position;
            if (transformToLocalFirst)
            {
                pos = this.transform.TransformPoint(pos);
            }
            center += pos;
        }
        return center / this.particles.Length;
    }

    private void UpdateGameObjectOrientation()
    {
        Quaternion quat = this.GetTetraFrameRotation();
        this.transform.rotation = quat * this.tetraCoordinateFrame * this.initialOrientation;
    }

    /// <summary>
    /// Saves the initial direction of a tetrahedron axis in order to have a reference for orienting the box.
    /// </summary>
    private void SetOrientationAxis()
    {
        this.tetraCoordinateFrame = Quaternion.Inverse(this.GetTetraFrameRotation());
        this.initialOrientation = this.transform.rotation;
    }

    private Quaternion GetTetraFrameRotation()
    {
        Vector3 A = this.particles[0].position;
        Vector3 B = this.particles[2].position;
        Vector3 C = this.particles[3].position;

        Vector3 dir1 = B - A;
        Vector3 dir2 = C - B;

        return Quaternion.LookRotation(dir1, dir2);
    }

    //------------------------
    // Drawing
    //------------------------
    private bool E_pointsTransformedInLocalSpace = false;
    private void OnDrawGizmos()
    {
        // Draw Particles and Constraints
        if (particles != null)
        {
            float percent = 0f;
            float step = 1f / particles.Length;
            float maxVelocityParticle_val = float.MinValue;
            int maxVelocityParticle_idx = 0;
        
            Color color = Color.red;
            for (int i = 0; i < this.particles.Length; i++)
            {
                Particle p = this.particles[i];

                Vector3 pLocal = p.position;
                if (!E_pointsTransformedInLocalSpace)
                {
                    pLocal = this.transform.TransformPoint(p.position);
                    this.centerOfMass = this.ComputeCenterOfMass(true);
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

        // Draw Center of Mass
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(this.centerOfMass, 0.2f);
    }

    private void OnValidate()
    {
        this.centerOfMass = this.ComputeCenterOfMass();
    }
}
