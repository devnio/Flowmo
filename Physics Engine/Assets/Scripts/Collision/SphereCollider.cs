using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphereCollider : BaseCollider
{
    // Some variables used for the editor
    private bool isRunning;
    private float _initialMaxScale;
    private float _assignedRadius;

    public bool isSingleParticle;
    private Particle singleParticle;

    public float Radius;
    [HideInInspector]
    public float SqrRadius;

    private ColliderBox colliderBoxForOctree;

    private void Start()
    {
        // Assign unique Id
        this.AssignUniqueId();

        // Check if this game object also has a colliderbox, if that's the case it means that its used for the octree
        colliderBoxForOctree = this.GetComponent<ColliderBox>();
        if (colliderBoxForOctree != null)
        {
            // make sure this is false (make it in the inspector to be sure)
            this.colliderBoxForOctree.AddToCollisionManager = false;
        }

        // Add to collision manager
        CollisionManager.Instance.AddCollider(this, this.isSingleParticle);

        // Keep current scale, so if in game changes happen to scale, the collider will be scaled accordingly
        this._initialMaxScale = Mathf.Max(this.transform.localScale.x, this.transform.localScale.y, this.transform.localScale.z);
        this._assignedRadius = this.Radius;
        this.isRunning = true;
        this.SqrRadius = this.Radius * this.Radius;

        Logger.Instance.DebugInfo("Radius: " + this.Radius.ToString() + "SqrRadius: " + this.SqrRadius.ToString(), "SPHERE COLLIDER");
    }

    public override void UpdateColliderPose(Vector3 displace)
    {
        // displace used for finding min penetration distance for collision
        if (!isSingleParticle)
        {
            this._center = this.transform.TransformPoint(this.displaceCenter + displace);
        }
        else
        {
            if (this.singleParticle != null)
            {
                this._center = this.transform.TransformPoint(this.singleParticle.position);
            }
        }
        //if (this.colliderBoxForOctree != null)
        //{
        //    this.colliderBoxForOctree.UpdateColliderPose(Vector3.zero);
        //}

        // relative to localScale (UNCOMMENT IF SCALE IS CHANGING DURING RUNTIME)
        //if (this.isRunning)
        //{
        //    var maxScale = Mathf.Max(this.transform.localScale.x, this.transform.localScale.y, this.transform.localScale.z);
        //    this.Radius = this._assignedRadius * (maxScale / this._initialMaxScale);
        //}

    }

    public void AssignSingleParticle(Particle p)
    {
        this.singleParticle = p;
    }

    public void ChangeSingleParticlePosition(Vector3 pos)
    {
        this.singleParticle.position = pos;
    }

    public Vector3 GetSingleParticlePosition()
    {
        return this.singleParticle.position;
    }

    public float GetSingleParticleInvMass()
    {
        return this.singleParticle.invMass;
    }

    //=========================
    // UPDATE VARIABLES
    //=========================

    //=========================
    // DRAW
    //=========================
    private void OnDrawGizmos()
    {
        Color col = Color.green;
        col.a = 0.5f;
        Gizmos.color = col;

        if (this.transform.hasChanged)
        {
            this.UpdateColliderPose(Vector3.zero);
            this.transform.hasChanged = false;
        }

        Gizmos.DrawWireSphere(this._center, this.Radius);
    }

}