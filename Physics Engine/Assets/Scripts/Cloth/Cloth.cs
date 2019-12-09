using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Cloth : MonoBehaviour
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

    public bool ShowVelocityArrows;
    private GameObject[] VelocityArrows_DB;

    private MeshFilter meshFilter;
    private Mesh mesh;
    public DynamicGrid dynamicGrid;

    private void Start()
    {
        this.meshFilter = this.GetComponent<MeshFilter>();
        this.dynamicGrid = this.GetComponent<DynamicGrid>();

        this.dynamicGrid.Generate();

        this.mesh = this.meshFilter.mesh;

        this.CreateParticlesAndConstraints();

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
        E_pointsTransformedInLocalSpace = true;

        // Add Tetrahederon and Bounding constraints 
        constraints = new List<Constraint>();
        constraints.Add(new DistanceConstraint(particles, distTuples));
        //constraints.Add(new BoundConstraint(particles, new Vector3(10, 5, 10)));

        // Add object to simulation
        VerletSimulation.Instance.AddCloth(this);

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
        // Integration
        foreach (Particle p in particles)
        {
            Vector3 temp = p.position;
            p.position += p.position - p.prevPosition + (acceleration * dt * dt) * p.invMass;
            p.prevPosition = temp;
        }
        UpdateSoftBodyMesh();
    }

    public void SatisfyConstraints()
    {
        foreach (Constraint c in constraints)
        {
            c.ConstraintUpdate();
        }
    }

    public void UpdateSoftBodyMesh()
    {
        Vector3[] newVertices = new Vector3[this.particles.Length];
        for (int i = 0; i < this.particles.Length; i++)
        {
            newVertices[i] = this.particles[i].position;
        }

        this.mesh.vertices = newVertices;
    }

    //------------------------
    // SoftBody
    //------------------------
    private void CreateParticlesAndConstraints()
    {
        this.CreateParticlesBasedOnMesh();
        this.CreateZigZagDistanceConstraints();
        //this.CreateDistanceConstraintsBasedOnMesh();
    }
    private void CreateParticlesBasedOnMesh()
    {
        this.particles = new Particle[this.mesh.vertexCount];
        for (int i = 0; i < this.mesh.vertexCount; i++)
        {
            this.particles[i] = new Particle(this.mesh.vertices[i], 1f);
            if (i == (this.dynamicGrid.xSize+1) * this.dynamicGrid.ySize || i == ((this.dynamicGrid.xSize + 1) * (this.dynamicGrid.ySize + 1)-1))
            {
                particles[i].invMass = 0;
            }
        }
    }

    private void CreateZigZagDistanceConstraints()
    {
        //for structural springs add: +((this.dynamicGrid.ySize) * (this.dynamicGrid.xSize + 1) * 2)
        int tot_dist_const = (this.dynamicGrid.xSize) * (this.dynamicGrid.ySize) * 2 +
            ((this.dynamicGrid.ySize) * (this.dynamicGrid.xSize + 1) * 2) +
            ((this.dynamicGrid.ySize-1) * (this.dynamicGrid.xSize + 1) * 2); // should be (x-1)(y-1)*2 but see how I generated vertices

        this.distTuples = new DistTuple[tot_dist_const];
        int i = 0;
        for (int y = 0; y <= this.dynamicGrid.ySize - 1; y++)
        {
            for (int x = 0; x <= this.dynamicGrid.xSize; x++)
            {
                if (x == 0)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), (y + 1) * (this.dynamicGrid.xSize + 1) + (x + 1), -1);
                    i++;
                }
                else if (x == this.dynamicGrid.xSize)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), (y + 1) * (this.dynamicGrid.xSize + 1) + (x - 1), -1);
                    i++;
                }
                else
                {
                    Debug.Log("INISDE");
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), (y + 1) * (this.dynamicGrid.xSize + 1) + (x + 1), -1);
                    i++;
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), (y + 1) * (this.dynamicGrid.xSize + 1) + (x - 1), -1);
                    i++;
                }
            }
        }

        // Add also structural springs
        float springW = 0.1f;
        for (int y = 0; y <= this.dynamicGrid.ySize; y++)
        {
            for (int x = 0; x <= this.dynamicGrid.xSize; x++)
            {
                if (x < this.dynamicGrid.xSize)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), x + 1 + y * (this.dynamicGrid.xSize + 1), -1, springW);
                    i++;
                }

                if (y < this.dynamicGrid.xSize)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), x + (y + 1) * (this.dynamicGrid.xSize + 1), -1, springW);
                    i++;
                }

            }
        }

        springW = 0.0075f;
        // Add bending springs
        for (int y = 0; y <= this.dynamicGrid.ySize; y++)
        {
            for (int x = 0; x <= this.dynamicGrid.xSize; x++)
            {
                if (x < this.dynamicGrid.xSize-1)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), x + 2 + y * (this.dynamicGrid.xSize + 1), -1, springW);
                    i++;
                }

                if (y < this.dynamicGrid.ySize-1)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), x + (y + 2) * (this.dynamicGrid.xSize + 1), -1, springW);
                    i++;
                }

            }
        }
    }

    private void CreateDistanceConstraintsBasedOnMesh()
    {
        int totElements = this.mesh.vertexCount * (this.mesh.vertexCount - 1) / 2;
        this.distTuples = new DistTuple[totElements];
        int sum = 0;

        for (int i = 0; i < this.mesh.vertexCount; i++)
        {
            for (int j = i + 1; j < this.mesh.vertexCount; j++)
            {
                //Debug.Log("i: " + i + ", j: " + j + ", sum : " + sum + ", idx: "  + (sum + j - 1));
                this.distTuples[sum + j - 1] = new DistTuple(i, j, -1);
            }
            sum += this.mesh.vertexCount - i - 2;
        }
        Debug.Log("FINISH CREATING " + totElements + " CONSTRAINTS");
    }

    //private void CreateDistanceConstraintsBasedOnMesh()
    //{
    //    this.distTuples = new DistTuple[this.mesh.vertexCount];

    //    for (int i = 0; i < this.mesh.vertexCount; i++)
    //    {
    //        if (i > 0)
    //            this.distTuples[i - 1] = new DistTuple(i, i-1, -1);
    //        if (i < this.mesh.vertexCount - 1)
    //            this.distTuples[i + 1] = new DistTuple(i, i+1, -1);
    //    }
    //    Debug.Log("FINISH CREATING " + this.mesh.vertexCount + " CONSTRAINTS");
    //}



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
