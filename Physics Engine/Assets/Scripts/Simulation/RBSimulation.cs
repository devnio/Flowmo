using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ===========
// DEPRECATED
// ===========

// REMEMBER TO SET EXECUTION ORDER
public class RBSimulation : Singleton<RBSimulation>, ISimulation
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
    public List<RigidBody> RigidBodies;
    
    private void Start()
    {
        // Initialize stuff
        Logger.Instance.DebugInfo("RB Simulation: Started");
        passedTime = Time.time;
    }

    private void Update()
    {
        if (Time.time >= passedTime + _dt)
        {
            // Update passed time for _dt
            passedTime = Time.time;

            // Update One Step Physics
            UpdateStepPhysics(_dt * _timeScale);
        }
    }

    // ===========
    // Physics Methods
    // ===========
    /// <summary>
    /// Update one step the physics.
    /// </summary>
    /// <param name="dt"></param>
    public void UpdateStepPhysics(float dt)
    {
        // TODO: Check Physics
        CollisionManager.Instance.DetectAndResolveCollisions();

        // Update RigidBodies
        UpdateRigidBodies(dt);
    }

    /// <summary>
    /// Update one step the rigidbodies.
    /// </summary>
    /// <param name="dt"></param>
    public void UpdateRigidBodies(float dt)
    {
        foreach(RigidBody rb in RigidBodies)
        {
            rb.UpdateStep(dt);
        }
    }

    /// <summary>
    /// Add a new rigidbody to the bodies that get updated in the simulation.
    /// </summary>
    /// <param name="rigidBody"></param>
    public void AddRigidBody(RigidBody rigidBody)
    {
        if (RigidBodies == null)
        {
            RigidBodies = new List<RigidBody>();
        }
        RigidBodies.Add(rigidBody);

        Logger.Instance.DebugInfo("Added a rigidbody!");
    }

    public float GetDT()
    {
        return _dt;
    }

}
