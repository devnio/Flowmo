using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Particle
{

    public Vector3 position;
    [HideInInspector]
    public Vector3 prevPosition;
    [HideInInspector]
    public float invMass;

    public float mass = 1f;

    public Particle(Vector3 position, float mass)
    {
        this.position = position;
        this.prevPosition = position;
        this.invMass = 1f / mass;
    }
}
