using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VerletSimulation : Singleton<VerletSimulation>, ISimulation
{
    // ===========
    // PRIVATE
    // ===========
    [SerializeField]
    private float _dt;
    [SerializeField]
    private float _timeScale = 1;

    private float passedTime;

    // ===========
    // PUBLIC
    // ===========
    public int ConstraintIterations = 4;
    public List<ParticleObject> ParticleObjects;
    public List<SoftBody> SoftBodyObjects;
    public List<Cloth> ClothObjects;


    private void Update()
	{
		if ((Time.time >= passedTime + _dt) && !_stopSimulation)
		{
            // Update passed time for _dt
            passedTime = Time.time;

			// Update One Step Physics
			UpdateStepPhysics(_dt * _timeScale);
		}

        if (moveOneFrame) { moveOneFrame = false; _stopSimulation = true; }
	}

	public void UpdateStepPhysics(float dt)
    {
        // Update RigidBodies
        UpdateParticles(dt);

        // Detect Collisions
        //CollisionManager.Instance.DetectCollisions();

        // Satisfy Constraints
        SatisfyConstraints(this.ConstraintIterations);
    }

    public void UpdateParticles(float dt)
    {
        // PARTICLE OBJ
        foreach (ParticleObject pb in ParticleObjects)
        {
            pb.UpdateStep(dt);
        }

        // SOFT BODY OBJ
        foreach (SoftBody sb in SoftBodyObjects)
        {
            sb.UpdateStep(dt);
        }

        // CLOTH OBJ
        foreach (Cloth cl in ClothObjects)
        {
            cl.UpdateStep(dt);
        }
    }

    public void SatisfyConstraints(int iterations)
    {
        // PARTICLE OBJ
        foreach (ParticleObject pb in ParticleObjects)
        {
            for (int i = 0; i < iterations; i++)
            {
                pb.SatisfyConstraints();
            }
            pb.UpdateGameObjectPose();
        }

        // SOFT BODY OBJ
        foreach(SoftBody sb in SoftBodyObjects)
        {
            for (int i = 0; i < iterations; i++)
            {
                sb.SatisfyConstraints();
            }
        }

        // CLOTH OBJ
        foreach (Cloth cl in ClothObjects)
        {
            for (int i = 0; i < iterations; i++)
            {
                cl.SatisfyConstraints();
            }
        }
    }

    /// <summary>
    /// Add a new particle object to the list of objects that get updated in the simulation.
    /// </summary>
    /// <param name="rigidBody"></param>
    /// =================
    /// Particle Object
    /// =================
    public void AddParticleObject(ParticleObject pObject)
    {
        if (ParticleObjects == null)
        {
            ParticleObjects = new List<ParticleObject>();
        }
        ParticleObjects.Add(pObject);

        Logger.Instance.DebugInfo("Added a particle object!");
    }

    /// =================
    /// Soft Body Object
    /// =================
    public void AddSoftBody(SoftBody sbObject)
    {
        if (SoftBodyObjects == null)
        {
            SoftBodyObjects = new List<SoftBody>();
        }
        SoftBodyObjects.Add(sbObject);

        Logger.Instance.DebugInfo("Added a soft body object!");
    }

    /// =================
    /// Cloth Object
    /// =================
    public void AddCloth(Cloth clothObject)
    {
        if (ClothObjects == null)
        {
            ClothObjects = new List<Cloth>();
        }
        ClothObjects.Add(clothObject);

        Logger.Instance.DebugInfo("Added a cloth object!");
    }

    public bool _stopSimulation = false;
    public void StopSimulation(bool stop = true)
    {
        Logger.Instance.DebugInfo("STOP SIMULATION");
        _stopSimulation = stop;
    }

    private bool moveOneFrame = false;
    public void NextFrame()
    {
        _stopSimulation = false;
        moveOneFrame = true;
    }

    public float GetDT()
    {
        return _dt;
    }
}
