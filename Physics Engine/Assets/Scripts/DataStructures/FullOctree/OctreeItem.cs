using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeItem : MonoBehaviour
{
    public List<OctreeNode> my_ownerNodes = new List<OctreeNode>(); // owner node for current node
    private Vector3 prevPos;
    public ColliderBox colliderBox;  // TODO: add aabb to BaseCollider and overwrite in case of BoxCollider

    // Debug
    private TextMesh textDebugMesh;

    void Start()
    {
        Transform child = this.transform.GetChild(0);
        if (child != null)
        {
            this.textDebugMesh = child.GetComponent<TextMesh>();
        }
        
        prevPos = transform.position;
    }


    private void FixedUpdate()
    {
        if (transform.position != prevPos)
        {
            RefreshOwners();
            prevPos = transform.position;
        }
    }

    /// <summary>
    /// Check the references to the nodes. Where is this item contained?
    /// </summary>
    [ContextMenu("RefreshOwners")]
    public void RefreshOwners()
    {
        Debug.Log("OCTREE ITEM CREATE/MOVED: Refreshing owners");

        // Moved, so check for octree status
        OctreeNode.octreeRoot.ProcessItem(this);

        // List of nodes that still contain this item
        List<OctreeNode> survivedNodes = new List<OctreeNode>();

        // List of nodes that don't contain this item anymore
        List<OctreeNode> obsoleteNodes = new List<OctreeNode>();

        foreach (OctreeNode on in my_ownerNodes)
        {
            //if (!on.ContainsItemPos(transform.position))
            if (!on.ContainsItemColliderBox(this.colliderBox))
            {
                obsoleteNodes.Add(on);
            }
            else
            {
                survivedNodes.Add(on);
            }
        }

        // Assign new owners
        my_ownerNodes = survivedNodes;

        foreach (OctreeNode on in obsoleteNodes)
        {
            on.Attempt_ReduceSubdivisions(this);
        }

        // DEBUG
        UpdateDebugMeshText();
    }


    // ===================
    // DEBUG
    // ===================
    public void UpdateDebugMeshText()
    {
        if (this.textDebugMesh != null)
        {
            string d = "My Owner Nodes: \n";
            foreach(OctreeNode on in this.my_ownerNodes)
            {
                d += on.GetName();
                d += " \n";
            }
            this.textDebugMesh.text = d;
        }
    }


}
