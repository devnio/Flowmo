using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RigidBody : MonoBehaviour
{
    // Public
    public BaseCollider Collider;
    [Header("Is this rigidbody part of the simulation.")]
    public bool IsPartOfSimulation = true;
    [Header("Apply gravity to the rigidbody.")]
    public bool applyGravity = true;

    // Note for position we use: transform.position (for efficiency)
    public float mass;
    public Vector3 force;
    public Vector3 linearVelocity;

    private void Start()
    {
        // Add rigidbody to simulation
        if (IsPartOfSimulation) RBSimulation.Instance.AddRigidBody(this);

        // Update acceleration based on gravity
        if (applyGravity) force += Const.Gravity * mass;
    }

    /// <summary>
    /// Performs one physics step on the rigidbody.
    /// </summary>
    /// <param name="dt"></param>
    public void UpdateStep(float dt)
    {
        // Apply force (using symplectic euler)
        linearVelocity += dt * (force/mass);
        transform.position = transform.position + dt * linearVelocity;

        // Physics Positions
        UpdateAABBCollider();
    }

    public void UpdateAABBCollider()
    {
        if (Collider != null) Collider.UpdateColliderPose(Vector3.zero);
    }

}
