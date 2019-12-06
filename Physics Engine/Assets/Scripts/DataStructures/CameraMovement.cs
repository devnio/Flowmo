using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float speed;

    // Update is called once per frame
    void Update()
    {
        this.transform.Translate((Vector3.right * Input.GetAxisRaw("Horizontal") + Vector3.forward * Input.GetAxisRaw("Vertical")) * this.speed, Space.Self);
        this.transform.rotation = Quaternion.Euler(-(Input.mousePosition.y - Screen.height / 2) * this.speed, (Input.mousePosition.x - Screen.width / 2) * this.speed, 0);
    }

}
