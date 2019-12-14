using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Constraint 
{
    public Particle[] particlesUnderConstraint;

    abstract public void ConstraintUpdate();
}

public class DistanceConstraint : Constraint
{
    public DistTuple[] distanceConstraints;
    public DistanceConstraint(Particle[] particles, DistTuple[] distanceConstraints)
    {
        this.particlesUnderConstraint = particles;
        this.distanceConstraints = distanceConstraints;
        this.InitializeConstraints();
        Logger.Instance.DebugTuples(this.distanceConstraints, "DistanceConstraints");
    }

    /// <summary>
    /// The constraints with distance -1 have to be resolved as the current distance.
    /// </summary>
    public void InitializeConstraints()
    {
        for (int i = 0; i < this.distanceConstraints.Length; i++)
        {
            var tuple = this.distanceConstraints[i];

            // If distance was set to -1, take the current distance
            if (System.Math.Abs(tuple.Item3 - -1f) < Mathf.Epsilon)
            {
                this.distanceConstraints[i].Item3 = (this.particlesUnderConstraint[tuple.Item1].position - this.particlesUnderConstraint[tuple.Item2].position).magnitude;
            }
        }
    }

    public override void ConstraintUpdate()
    {
        foreach(DistTuple tuple in this.distanceConstraints)
        {
            Particle p1 = this.particlesUnderConstraint[tuple.Item1];
            Particle p2 = this.particlesUnderConstraint[tuple.Item2];
            float distConst = tuple.Item3;

            Vector3 delta = p2.position - p1.position;
            float deltaLen = delta.magnitude;

            // SAFETY CHECK 
            if (Util.CMP(deltaLen, 0f) || float.IsNaN(deltaLen)) continue;

            float diff = (deltaLen - distConst) / (deltaLen * (p1.invMass + p2.invMass));
            float k = tuple.springW;
            float d = tuple.springD * 0.01f;  // should be multiplied by the velocity
            Vector3 m = delta * 0.5f * diff;
            p1.position += (m * k - d * p1.velocity) * p1.invMass;
            p2.position -= (m * k + d * p2.velocity) * p2.invMass;
        }
    }
}

public class PointConstraint : Constraint
{
    public PointTuple[] pointConstraints;

    public PointConstraint(Particle[] particles, PointTuple[] pointConstraints)
    {
        this.particlesUnderConstraint = particles;
        this.pointConstraints = pointConstraints;
    }

    public override void ConstraintUpdate()
    {
        foreach (PointTuple tuple in this.pointConstraints)
        {
            Particle p = this.particlesUnderConstraint[tuple.p];
            Vector3 pos = tuple.pos;
            float k = tuple.springW;
            float d = tuple.springD * 0.01f;  // should be multiplied by the velocity

            Vector3 delta = p.position - pos;
            float deltaLen = delta.magnitude;

            // SAFETY CHECK 
            if (Util.CMP(deltaLen, 0f) || float.IsNaN(deltaLen)) continue;

            float diff = deltaLen / (deltaLen * (p.invMass));
            
            Vector3 m = delta * diff;
            p.position -= (m * k - d * p.velocity) * p.invMass;
        }
    }
}

public class BoundConstraint : Constraint
{
    public BoundConstraint(Particle[] particles, Vector3 boxBound)
    {
        this.BoxBound = boxBound;
        this.particlesUnderConstraint = particles;
    }

    public Vector3 BoxBound;

    override public void ConstraintUpdate()
    {
        foreach(Particle p in particlesUnderConstraint)
        {
            Vector3 pos = p.position;
            pos.x = Mathf.Clamp(pos.x, -BoxBound.x, BoxBound.x);
            pos.y = Mathf.Clamp(pos.y, -BoxBound.y, BoxBound.y);
            pos.z = Mathf.Clamp(pos.z, -BoxBound.z, BoxBound.z);
            p.position = pos;
        }
    }
}
