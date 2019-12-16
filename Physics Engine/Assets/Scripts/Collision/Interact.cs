using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour
{
    public SoftStructure softStructure; // the object we are interacting with
    private SphereCollider dragSphereColl;

    private Vector3 mousePos_StartDragMode;
    private Vector3 particlePos_StartDragMode;
    private float moveStrength = 3f;

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (!this.softStructure.IsInDragMode())
            {
                Vector3 mousePos_Screen = Input.mousePosition;
                Ray shootRay = Camera.main.ScreenPointToRay(mousePos_Screen);
                Debug.DrawLine(shootRay.origin, shootRay.origin + shootRay.direction * 100000f, Color.red);

                // Check intersection with sphere
                foreach (SphereCollider sp in this.softStructure.sphereColliders)
                {
                    if (SphereRayIntersection(shootRay, sp))
                    {
                        Logger.Instance.DebugInfo("Ray intersected sphere.");
                        this.softStructure.ActivateDragMode(true);
                        this.mousePos_StartDragMode = Camera.main.ScreenToViewportPoint(mousePos_Screen);
                        this.particlePos_StartDragMode = sp.GetSingleParticlePosition();
                        this.dragSphereColl = sp;
                        break;
                    }
                }
            }
            else
            {
                Vector3 disp = Camera.main.ScreenToViewportPoint(Input.mousePosition) - this.mousePos_StartDragMode;
                Debug.Log(disp);
                this.dragSphereColl.ChangeSingleParticlePosition(this.particlePos_StartDragMode + disp * this.moveStrength);
            }
        }
        else
        {
            if (this.softStructure.IsInDragMode())
            {
                this.softStructure.ActivateDragMode(false);
            }
        }
    }

    // =======================
    // Sphere Ray Intersection
    // =======================
    public bool SphereRayIntersection(Ray ray, SphereCollider sc)
    {
        Vector3 sphereRay = sc._center - ray.origin;
        float t_ray_sphere = Vector3.Dot(ray.direction, sphereRay);
        if (t_ray_sphere < 0) return false;

        float d_sq = sphereRay.sqrMagnitude - t_ray_sphere * t_ray_sphere;
        if (d_sq > sc.Radius * sc.Radius) return false;

        return true;
    }
}
