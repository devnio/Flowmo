using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    Material recentCubeMaterial;
    Transform recentCubeTransform;
    public float speed;

    // For naming cubes
    private int counter;

    private void Start()
    {
        //Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        this.transform.Translate(Input.GetAxisRaw("Horizontal") * Time.deltaTime * this.speed, 0f,  Input.GetAxisRaw("Vertical") * Time.deltaTime * this.speed, Space.Self);
        transform.Rotate(0f, Input.GetAxis("Mouse X") * Time.deltaTime * this.speed * 2f, 0f, Space.Self);
        transform.Rotate(-Input.GetAxis("Mouse Y") * Time.deltaTime * this.speed * 2f, 0f, 0f, Space.Self);

        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            GameObject newCube = (GameObject)GameObject.Instantiate(Resources.Load("OctreeItem"));
            newCube.name = "ITEM_" + counter;
            counter++;
            newCube.transform.position = this.transform.position + transform.forward * 6f;
            newCube.GetComponent<OctreeItem>().RefreshOwners();
        }

        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.forward, out hit, 100f))
        {
            if (hit.collider.tag == "OctCube")
            {
                if (recentCubeMaterial != null)
                {
                    recentCubeMaterial.color = Color.white;
                }

                GameObject caught = hit.collider.gameObject;
                //Rigidbody caughtRigid = caught.GetComponent<Rigidbody>();
                recentCubeMaterial = caught.GetComponent<Renderer>().material;

                recentCubeMaterial.color = Color.red;

                // Parent
                if (Input.GetKeyDown(KeyCode.Mouse1))
                {
                    //caughtRigid.isKinematic = true;
                    recentCubeTransform = caught.transform;
                    recentCubeTransform.parent = this.transform;
                }
                if (Input.GetKeyUp(KeyCode.Mouse1))
                {
                    //caughtRigid.isKinematic = false;
                    if (recentCubeTransform != null)
                    {
                        recentCubeTransform.parent = null;
                    }
                }

                // Remove
                if (Input.GetKey(KeyCode.E))
                {
                    GameObject.Destroy(caught); // TODO: destroy from octree.
                }

                // Push
                if (Input.GetKeyDown(KeyCode.R))
                {
                    //caughtRigid.AddForce(transform.forward * 150f);
                }

            }
        }
        else
        {
            if (recentCubeMaterial != null)
            {
                recentCubeMaterial.color = Color.white;
            }
        }

        // Close Program
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

    }

}
