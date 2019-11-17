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
    private Vector3 orientationAxis;
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

    private Vector3 frictionVector;

    private void Start()
    {
        // Check if there is a collider
        if (this.gameObject.GetComponent<BaseCollider>() != null)
        {
            this.Collider = this.gameObject.GetComponent<BaseCollider>();
            this.Collider.AssignParticleObject(this);
        }

        // Add Tetrahederon and Bounding constraints 
        constraints = new List<Constraint>();
        constraints.Add(new DistanceConstraint(particles, distTuples));
        constraints.Add(new BoundConstraint(particles, new Vector3(10, 5, 10)));

        // Initialize prev pos as current one and invmass
        foreach (Particle p in particles) {
            p.position = this.transform.TransformPoint(p.position);
            p.invMass = 1f / p.mass;
            p.prevPosition = p.position - velocity / 10f;

            // For visualizer
            E_pointsTransformedInLocalSpace = true;
        }

        // Initialize center position
        this.centerOfMass = this.ComputeCenterOfMass();
        this.SetOrientationAxis();

        // Add object to simulation
        VerletSimulation.Instance.AddParticleObject(this);

        // Initialize acceleration
        gravityAcceleraiton = new Vector3(0, -9.81f, 0);
        if (this.UseGravity) acceleration += gravityAcceleraiton;
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
                    p.position += p.position - p.prevPosition + acceleration * dt * dt;

                    p.prevPosition = temp - this.frictionVector;
                    this.SetFrictionVector(Vector3.zero);

                    // TODO: do for general surfaces
                    //if (p.position.y < -4.8)
                    //{
                    //    p.prevPosition = temp + (p.position - temp).normalized * 0.005f;
                    //}
                    //else
                    //{
                    //    p.prevPosition = temp;
                    //}
                    
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

        this.UpdateGameObjectOrientation();

        this.Collider.UpdateColliderPose(Vector3.zero);
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
        Vector3 dir = this.GetOrientationAxis();
        this.transform.rotation = Quaternion.FromToRotation(this.orientationAxis, dir);
    }

    /// <summary>
    /// Saves the initial direction of a tetrahedron axis in order to have a reference for orienting the box.
    /// </summary>
    private void SetOrientationAxis()
    {
        this.orientationAxis = this.GetOrientationAxis();
        //this.initialOrientation = this.transform.rotation;
    }

    private Vector3 GetOrientationAxis()
    {
        Vector3 A = this.particles[0].position;
        Vector3 D = this.particles[1].position;
        Vector3 dir = D - A; 
        return dir;
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
            Color color = Color.red;
            foreach (Particle p in this.particles)
            {
                Vector3 pLocal = p.position;
                if (!E_pointsTransformedInLocalSpace)
                {
                    pLocal = this.transform.TransformPoint(p.position);
                    this.centerOfMass = this.ComputeCenterOfMass(true);
                }

                color = Color.red;
                color.a = 0.5f;
                Gizmos.color = color;

                Gizmos.DrawSphere(pLocal, 0.2f);

                color = Color.blue;
                color.a = 0.25f;
                Gizmos.color = color;

                if (E_pointsTransformedInLocalSpace) Gizmos.DrawSphere(p.prevPosition, 0.2f);
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
