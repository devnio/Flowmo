using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Particle
{

    public Vector3 position;
    [HideInInspector]
    public Vector3 prevPosition;
    //[HideInInspector]
    public Vector3 velocity;

    public float invMass = 1f;
    public float mass = 1f;

    public Particle(Vector3 position, float mass, float invMass = 1f)
    {
        this.position = position;
        this.prevPosition = position;
        this.mass = mass;
        this.invMass = invMass;

        this.velocity = Vector3.zero;
    }
}
