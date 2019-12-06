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
            if (System.Math.Abs(tuple.Item3 - -1f) < Mathf.Epsilon)
            {
                this.distanceConstraints[i].Item3 = (this.particlesUnderConstraint[tuple.Item1].position - this.particlesUnderConstraint[tuple.Item2].position).magnitude;
            }
        }
    }

    public override void ConstraintUpdate()
    {
        foreach(Tuple<int, int, float> tuple in this.distanceConstraints)
        {
            Particle p1 = this.particlesUnderConstraint[tuple.Item1];
            Particle p2 = this.particlesUnderConstraint[tuple.Item2];
            float distConst = tuple.Item3;

            Vector3 delta = p2.position - p1.position;
            float deltaLen = delta.magnitude;

            // SAFETY CHECK 
            if (Util.CMP(deltaLen, 0f) || float.IsNaN(deltaLen)) continue;

            float diff = (deltaLen - distConst) / (deltaLen * (p1.invMass + p2.invMass));
            float w = 0.01f;
            p1.position += delta * 0.5f * diff * w;
            p2.position -= delta * 0.5f * diff * w;
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
