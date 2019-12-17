using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used for trees when collision happened to instantiate particles.
/// </summary>
public class InstantiateParticles : Singleton<InstantiateParticles>
{
    public GameObject leafParticleSystemObject;

    public void CreateLeafParticleSystem(Transform parent)
    {
        Instantiate(this.leafParticleSystemObject, parent);
    }
}
