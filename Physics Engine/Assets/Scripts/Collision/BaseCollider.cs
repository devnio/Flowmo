using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AABB
{
    public Vector3 _min;
    public Vector3 _max;
}


public class BaseCollider : MonoBehaviour
{
    [HideInInspector]
    public static int GlobalIdCounter { get; private set; }
    public int Id { get; private set; }
    [HideInInspector]
    public Vector3 _center;
    public Vector3 displaceCenter;

    private ParticleObject particleObject;

    /// <summary>
    /// Called during initialization of particle object.
    /// </summary>
    /// <param name="particleObject"></param>
    public void AssignParticleObject(ParticleObject particleObject)
    {
        this.particleObject = particleObject;
    }

    /// <summary>
    /// When a collision happens we don't want the object to be affected by forces in next frame, because we want to resolve collision.
    /// </summary>
    public void DontIntegrateParticleObjectThisFrame()
    {
        this.particleObject.integrateThisFrame = false;
    }

    public bool IsStatic()
    {
        return this.particleObject == null || this.particleObject.objectType == ObjectType.KINEMATIC;
    }

    public ParticleObject GetParticleObject()
    {
        return this.particleObject;
    }

    /// <summary>
    /// Update center of the collider based on the displaceCenter variable.
    /// </summary>
    public void UpdateCenterPosition()
	{
		_center = this.transform.TransformPoint(displaceCenter);
	}

    // The child has to override this method.
    public virtual void UpdateColliderPose(Vector3 displace) { }

    /// <summary>
    /// Assign the unique ID.
    /// </summary>
    public void AssignUniqueId()
    {
        Id = GlobalIdCounter;
        GlobalIdCounter++;
    }
}