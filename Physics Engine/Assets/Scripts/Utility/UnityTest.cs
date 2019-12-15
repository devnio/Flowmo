using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityTest : MonoBehaviour
{
    public Rigidbody rigidBody;
    public Vector3 velocity;
    void Start()
    {
        rigidBody.velocity = velocity;
    }

}
