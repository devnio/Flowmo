using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public struct ClothParameters
{
    public int clothSize;

    public float structuralStiffness;
    public float bendStiffness;
    public float shearStiffness;

    public float structuralDamping;
    public float bendDamping;
    public float shearDamping;
}

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

    private SphereCollider[] sphereColliders;

    private MeshFilter meshFilter;
    private Mesh mesh;
    public DynamicGrid dynamicGrid;

    public ClothParameters clothParams;

    public float clothTileSizeMult;
    public float clothSphereColliderSize;

    private void Start()
    {
        // Pass reference to UIClothManager
        UIClothManager.Instance.cloth = this;

        this.meshFilter = this.GetComponent<MeshFilter>();
        this.dynamicGrid = this.GetComponent<DynamicGrid>();

        // Add object to simulation
        VerletSimulation.Instance.AddCloth(this);

        // Initialize acceleration
        gravityAcceleraiton = new Vector3(0, -9.81f, 0);
        if (this.UseGravity) acceleration += gravityAcceleraiton;

    }

    // Used in the UI
    public void GenerateCloth()
    {
        this.dynamicGrid.Generate(this.clothParams.clothSize, this.clothTileSizeMult);
        this.mesh = this.meshFilter.mesh;
        this.CreateParticlesAndConstraints();
        this.InitializeConstraints();
    }

    // Used in the UI
    public void ResetCloth()
    {
        this.meshFilter.mesh = null;
        this.particles = null;
        this.constraints = null;
        foreach (SphereCollider sp in this.sphereColliders)
        {
            Destroy(sp);
        }
        this.sphereColliders = null;
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
        E_pointsTransformedInLocalSpace = true;

        // Add Tetrahederon and Bounding constraints 
        constraints = new List<Constraint>();
        constraints.Add(new DistanceConstraint(particles, distTuples));
    }

    /// <summary>
    /// Resets implicit velocity by changing prevPos. Not used now.
    /// </summary>
    public void ResetVelocity()
    {
        foreach (Particle p in particles)
        {
            p.prevPosition = p.position;
            p.velocity = Vector3.zero;
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

            // Update velocity used (only for damping now)
            p.velocity = p.position - p.prevPosition;
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
            sphereColliders[i].UpdateColliderPose(Vector3.zero);
        }

        this.mesh.vertices = newVertices;
    }

    //------------------------
    // SoftBody
    //------------------------
    private void CreateParticlesAndConstraints()
    {
        this.CreateParticlesBasedOnMesh();
        this.CreateDistanceConstraints();
        //this.CreateDistanceConstraintsBasedOnMesh();
    }
    private void CreateParticlesBasedOnMesh()
    {
        this.particles = new Particle[this.mesh.vertexCount];
        this.sphereColliders = new SphereCollider[this.mesh.vertexCount];
        for (int i = 0; i < this.mesh.vertexCount; i++)
        {
            this.particles[i] = new Particle(this.mesh.vertices[i], 1f);

            // Add sphere colliders
            SphereCollider sp = this.gameObject.AddComponent<SphereCollider>();
            sp.AssignSingleParticle(this.particles[i]);
            sp.isSingleParticle = true;
            sp.Radius = this.clothParams.clothSize / 10f * 1f;
            this.sphereColliders[i] = sp;

            // Clamp end particles
            if (i == (this.dynamicGrid.xSize+1) * this.dynamicGrid.ySize || i == ((this.dynamicGrid.xSize + 1) * (this.dynamicGrid.ySize + 1)-1))
            {
                particles[i].invMass = 0;
            }
        }
    }

    private void CreateDistanceConstraints()
    {
        //for structural springs add: +((this.dynamicGrid.ySize) * (this.dynamicGrid.xSize + 1) * 2)
        int tot_dist_const = (this.dynamicGrid.xSize) * (this.dynamicGrid.ySize) * 2 +
            ((this.dynamicGrid.ySize) * (this.dynamicGrid.xSize + 1) * 2) +
            ((this.dynamicGrid.ySize-1) * (this.dynamicGrid.xSize + 1) * 2); // should be (x-1)(y-1)*2 but see how I generated vertices

        this.distTuples = new DistTuple[tot_dist_const];
        int i = 0;

        float springW = 0.05f;
        float springD = 0.4f;

        // shear springs
        for (int y = 0; y <= this.dynamicGrid.ySize - 1; y++)
        {
            for (int x = 0; x <= this.dynamicGrid.xSize; x++)
            {
                if (x == 0)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), (y + 1) * (this.dynamicGrid.xSize + 1) + (x + 1), -1,
                        clothParams.shearStiffness, clothParams.shearDamping);
                    i++;
                }
                else if (x == this.dynamicGrid.xSize)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), (y + 1) * (this.dynamicGrid.xSize + 1) + (x - 1), -1,
                        clothParams.shearStiffness, clothParams.shearDamping);
                    i++;
                }
                else
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), (y + 1) * (this.dynamicGrid.xSize + 1) + (x + 1), -1,
                        clothParams.shearStiffness, clothParams.shearDamping);
                    i++;
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), (y + 1) * (this.dynamicGrid.xSize + 1) + (x - 1), -1,
                        clothParams.shearStiffness, clothParams.shearDamping);
                    i++;
                }
            }
        }

        // Add also structural springs
        for (int y = 0; y <= this.dynamicGrid.ySize; y++)
        {
            for (int x = 0; x <= this.dynamicGrid.xSize; x++)
            {
                if (x < this.dynamicGrid.xSize)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), x + 1 + y * (this.dynamicGrid.xSize + 1), -1,
                        clothParams.structuralStiffness, clothParams.structuralDamping);
                    i++;
                }

                if (y < this.dynamicGrid.xSize)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), x + (y + 1) * (this.dynamicGrid.xSize + 1), -1,
                        clothParams.structuralStiffness, clothParams.structuralDamping);
                    i++;
                }

            }
        }

        // Add bending springs
        for (int y = 0; y <= this.dynamicGrid.ySize; y++)
        {
            for (int x = 0; x <= this.dynamicGrid.xSize; x++)
            {
                if (x < this.dynamicGrid.xSize-1)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), x + 2 + y * (this.dynamicGrid.xSize + 1), -1,
                        clothParams.bendStiffness, clothParams.bendDamping);
                    i++;
                }

                if (y < this.dynamicGrid.ySize-1)
                {
                    this.distTuples[i] = new DistTuple(x + y * (this.dynamicGrid.xSize + 1), x + (y + 2) * (this.dynamicGrid.xSize + 1), -1,
                        clothParams.bendStiffness, clothParams.bendDamping);
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
