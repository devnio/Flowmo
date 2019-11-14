using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISimulation
{
    float GetDT();
    void UpdateStepPhysics(float dt);
}
