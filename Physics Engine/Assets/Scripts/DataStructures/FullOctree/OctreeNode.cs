using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Representation of one cube (cell)
public class OctreeNode
{
    // ===============
    // FIELDS
    // ===============
    public static int maxObjectLimit = 1;  // max amount of objects in this node (otherwise split)
    private static int maxDepth = 3;

    private int nodeDepth;
    private string name;

    private ColliderBox colliderBoxNode;
    private TextMesh textDebugMesh;

    static public List<OctreeNode> NodesToCheckForCollision;  // every time you add an item check if more than 2 items contain it and take items

    static OctreeNode _octreeRoot;  // only one root between all nodes (singleton)
    static public OctreeNode octreeRoot
    {
        get
        {
            if (_octreeRoot == null)
            {
                _octreeRoot = new OctreeNode(null, Vector3.zero, 15f, new List<OctreeItem>());
            }
            return _octreeRoot;
        }
    }

    GameObject octantGO; // object in charge to display boundaries
    LineRenderer octantLineRenderer;

    public float halfDimensionLength;  // half extent of cell
    private Vector3 pos;  // center of

    public OctreeNode parent;  
    public List<OctreeItem> containedItems = new List<OctreeItem>();

    OctreeNode[] _childrenNodes = new OctreeNode[8];
    public OctreeNode[] childrenNodes
    {
        get { return _childrenNodes; }
    }

    //TODO: add the init inside the collision manager
    //[RuntimeInitializeOnLoadMethod]
    public static bool Init()
    {
        NodesToCheckForCollision = new List<OctreeNode>();
        return octreeRoot == null;  // the first time the getter creates the object.
    }

    // ===============
    // CONSTRUCTOR
    // ===============
    public OctreeNode(OctreeNode parent, Vector3 thisChild_pos, float thisChild_halfLength, List<OctreeItem> potential_items, string name = "ROOT")
    {
        this.parent = parent;
        this.halfDimensionLength = thisChild_halfLength;
        this.pos = thisChild_pos;
        this.name = name;

        // Depth
        if (parent != null)
        {
            this.nodeDepth = this.parent.nodeDepth + 1;
        }
        else
        {
            this.nodeDepth = 0;
        }

        // Octant Object and ColliderBox attachement (+ debug stuff)
        octantGO = new GameObject();
        octantGO.name = this.name; 
        
        this.colliderBoxNode = octantGO.AddComponent<ColliderBox>();
        this.colliderBoxNode.AddToCollisionManager = false;
        this.colliderBoxNode.xyzLength = Vector3.one;

        octantGO.transform.position = this.pos;
        octantGO.transform.localScale = Vector3.one * this.halfDimensionLength * 2;
        this.colliderBoxNode.UpdateColliderPose(Vector3.zero);

        // Debug text
        GameObject octantGOChild = new GameObject("DebugText_" + this.name);
        octantGOChild.transform.parent = octantGO.transform;
        octantGOChild.transform.localPosition = Vector3.zero;

        this.textDebugMesh = octantGOChild.AddComponent<TextMesh>();
        this.textDebugMesh.fontSize = 50;
        this.textDebugMesh.characterSize = 0.1f;

        // Vizualization
        //octantLineRenderer = octantGO.AddComponent<LineRenderer>();
        //FillCube_VisualizeCoords(); // fill coords of line renderer

        foreach (OctreeItem item in potential_items)
        {
            // Check if the item really belongs to this particular node
            ProcessItem(item);
        }
    }

    // ===============
    // PROCESSIING
    // ===============
    public void EraseChildrenNodes()
    {
        _childrenNodes = new OctreeNode[8];
    }

    /// <summary>
    /// Killing a node means killing the whole set of siblings (8 nodes). We never remove single nodes.
    /// So we have to make sure that all the items get the reference to the siblings and this node lost.
    /// </summary>
    private void KillNode (OctreeNode[] obsoleteSiblingNodes)
    {
        // delete the reference of all the items in this node for their ownership list
        foreach (OctreeItem oi in containedItems)
        {
            oi.my_ownerNodes = oi.my_ownerNodes.Except(obsoleteSiblingNodes).ToList();

            
            foreach (OctreeNode octNode in obsoleteSiblingNodes)
            {
                if (NodesToCheckForCollision.Contains(octNode))
                {
                    NodesToCheckForCollision.Remove(octNode);
                }
            }

            oi.my_ownerNodes.Remove(this);

            oi.my_ownerNodes.Add(this.parent);
            parent.containedItems.Add(oi);

            AddContainedItemsToItemCollisionCheckList(this.parent);
        }

        // remove the references to objects (octantGO)
        foreach (OctreeNode sibling in parent.childrenNodes)
        {
            // maybe need to check for octantgo
            GameObject.Destroy(sibling.octantGO);
        }
        GameObject.Destroy(this.octantGO);
    }

    private void AddContainedItemsToItemCollisionCheckList(OctreeNode octNodeToAdd)
    {
        if (octNodeToAdd.containedItems.Count > 1 && !NodesToCheckForCollision.Contains(octNodeToAdd))
        {
            //ItemsToCheckForCollision.AddRange(this.containedItems.Except(ItemsToCheckForCollision));
            NodesToCheckForCollision.Add(octNodeToAdd);
        }
    }

    /// <summary>
    /// Add an item to the contained items of the node. Check also if needed to split.
    /// </summary>
    private void PushItem(OctreeItem item)
    {
        // Add item to the contained item.
        if (!containedItems.Contains(item))
        {
            containedItems.Add(item);
            item.my_ownerNodes.Add(this);
            AddContainedItemsToItemCollisionCheckList(this);
            Logger.Instance.DebugInfo("PushItem: adding item to the current node (no need to split).", "OCTREE NODE");
        }

        // Check if needed to split
        if (containedItems.Count > maxObjectLimit && nodeDepth < maxDepth)
        {
            Logger.Instance.DebugInfo("PushItem: splitting node -> too many items in current node and can go deeper.", "OCTREE NODE");
            Split();
        }
    }

    /// <summary>
    /// Splits this node into 8 subnodes stored inside childrenNodes.
    /// </summary>
    private void Split()
    {
        // Since this node is not a leaf node anymore
        // First: remove items reference to this node
        foreach(OctreeItem oi in containedItems)
        {
            oi.my_ownerNodes.Remove(this);
        }

        Vector3 positionVector = new Vector3(halfDimensionLength / 2, halfDimensionLength / 2, halfDimensionLength / 2);
        for (int i = 0; i < 4; i++)
        {
            _childrenNodes[i] = new OctreeNode(this, pos + positionVector, halfDimensionLength / 2, containedItems, this.name + "->" + i);
            positionVector = Quaternion.Euler(0f, -90f, 0f) * positionVector;
        }

        positionVector = new Vector3(halfDimensionLength / 2, -halfDimensionLength / 2, halfDimensionLength / 2);
        for (int i = 4; i < 8; i++)
        {
            _childrenNodes[i] = new OctreeNode(this, pos + positionVector, halfDimensionLength / 2, containedItems, this.name + "->" + i);
            positionVector = Quaternion.Euler(0f, -90f, 0f) * positionVector;
        }

        // TODO: ADDED [CHECK PERFORMANCE] Update all the contained items for the new tree structure before clearing this node
        foreach (OctreeItem oi in containedItems)
        {
            foreach (OctreeNode childNode in childrenNodes)
            {
                // TODO: after fix, try swapping this comment/uncomment group
                if (childNode.ProcessItem(oi))
                {
                    return;
                }
                //childNode.ProcessItem(oi);
                Logger.Instance.DebugInfo("Split(): created 8 childs -> processing item " + oi.name + " in the node " + childNode.GetName(), "OCTREE NODE");

            }
        }

        containedItems.Clear();
    }

    /// <summary>
    /// Checking if an item is inside the node (or subnodes of the node).
    /// If inside of node: PushItem() (reference from item and node are updated and also split if necessary).
    /// If there are childs go down the chain.
    /// </summary>
    public bool ProcessItem(OctreeItem item)
    {
        //if (ContainsItemPos(item.transform.position))
        if (ContainsItemColliderBox(item.colliderBox))
        {
            Logger.Instance.DebugInfo("ProcessItem: item is colliding with node", "OCTREE NODE");
            // If the item is inside this node return true
            if (ReferenceEquals(childrenNodes[0], null))
            {
                PushItem(item);

                // TODO: DEBUG (comment later)
                //this.UpdateDebugMeshText();
                //item.UpdateDebugMeshText();

                return true;
            }
            else
            {
                bool proc = false;
                // If this node has childs then process all the subnodes to see who containts this item
                foreach(OctreeNode childNode in childrenNodes)
                {
                    // TODO: after fix, try swapping this comment/uncomment group
                    if (childNode.ProcessItem(item))
                    {
                        //return true;
                        proc = true;
                    }
                    childNode.ProcessItem(item);
                }

                // TODO: DEBUG (comment later)
                //this.UpdateDebugMeshText();
                //item.UpdateDebugMeshText();

                return proc;
            }
        }

        // TODO: DEBUG (comment later)
        //this.UpdateDebugMeshText();
        //item.UpdateDebugMeshText();

        return false;
    }

    /// <summary>
    /// If an item moved check if the previous nodes can be reduces in size.
    /// Can remove childs in case of:
    /// (1) If the node has empty leaf nodes.
    /// (2) If the node doesn't exceed max amount of childs.
    /// </summary>
    public void Attempt_ReduceSubdivisions(OctreeItem escapedItem)
    {
        if (!ReferenceEquals(this, octreeRoot) && !Siblings_ChildrenNodesPresent_too_manyItems())
        {
            // delte node and siblings
            foreach (OctreeNode on in parent.childrenNodes)
            {
                // TODO: passing only this node should be enough or not? -> because we are iterating over all sibling nodes
                on.KillNode(parent.childrenNodes.Where(i => !ReferenceEquals(i, this)).ToArray());
            }
            parent.EraseChildrenNodes();
        }
        else
        {
            // remove the item from the contained items of this particular node (because item is no longer inside this node)
            containedItems.Remove(escapedItem);
            escapedItem.my_ownerNodes.Remove(this);
        }

        // TODO: DEBUG (comment later)
        //this.UpdateDebugMeshText();
    }

    /// <summary>
    /// True if the children nodes are present in the siblings of this PARTICULAR OBSOLETE NODE,
    /// or if their total number of items is way too much for the parent to accept.
    /// </summary>
    /// <returns></returns>
    private bool Siblings_ChildrenNodesPresent_too_manyItems()
    {
        List<OctreeItem> legacy_items = new List<OctreeItem>();

        // iterate through siblings and see if they have any children
        foreach(OctreeNode sibling in parent.childrenNodes)
        {
            // if the sibling node contains childrens return true (if not leaf node, don't delete)
            if (!ReferenceEquals(sibling.childrenNodes[0], null))
            {
                Logger.Instance.DebugInfo("SiblingTooManyItems(): siblings have children, WON'T destroy children.", "OCTREE NODE");
                return true;
            }

            // add all the items from the currently inspected sibling
            // add only the items that are not already contained
            legacy_items.AddRange(sibling.containedItems.Where(i => !legacy_items.Contains(i)));
        }

        // too many items for the parent to hold (don't get rid of siblings and this particulare obsolete node)
        if (legacy_items.Count > maxObjectLimit)
        {
            //Debug.Log("Too many items  " + legacy_items.Count);
            Logger.Instance.DebugInfo("SiblingTooManyItems(): too many items " + legacy_items.Count + ", WON'T destroy children.", "OCTREE NODE");

            return true;
        }

        Logger.Instance.DebugInfo("SiblingTooManyItems(): returning false (means that nodes are destroyed.)", "OCTREE NODE");
        // none of the siblings contain child nodes and their items coudl be hold by the parent so we can get rid of this particular node and his siblings
        return false;
    }

    /// <summary>
    /// Is a point inside this node?
    /// </summary>
    //public bool ContainsItemPos(Vector3 itemPos)
    //{
    //    if (itemPos.x > pos.x + halfDimensionLength || itemPos.x < pos.x - halfDimensionLength)
    //        return false;
    //    if (itemPos.y > pos.y + halfDimensionLength || itemPos.y < pos.y - halfDimensionLength)
    //        return false;
    //    if (itemPos.z > pos.z + halfDimensionLength || itemPos.z < pos.z - halfDimensionLength)
    //        return false;

    //    return true;
    //}

    public bool ContainsItemColliderBox(ColliderBox b1)
    {
        return CollisionManager.Instance.AreOBBsColliding(this.colliderBoxNode, b1);
    }

    // ===============
    // VISUALIZATION
    // ===============
    public string GetName()
    {
        return name;
    }

    public void UpdateDebugMeshText()
    {
        if (this.textDebugMesh != null && false)
        {
            string d = "Contained ITEMS: \n";
            // ITEMS
            foreach (OctreeItem oi in this.containedItems)
            {
                d += oi.gameObject.name;
                d += " \n";
            }

            d += " \n";
            d += "My Child NODES: \n";
            // NODES
            foreach (OctreeNode on in this.childrenNodes)
            {
                if (!ReferenceEquals(on, null))
                {
                    d += on.GetName();
                    d += " \n";
                }
            }

            this.textDebugMesh.text = d;
        }
    }

    void FillCube_VisualizeCoords()
    {
        Vector3[] cubeCoords = new Vector3[8];
        Vector3 corner = Vector3.one * this.halfDimensionLength; // Top Front Right
        for (int x = 0; x < 4; x++)
        {
            cubeCoords[x] = this.pos + corner;
            corner = Quaternion.Euler(0f, 90f, 0f) * corner;
        }

        corner = new Vector3(this.halfDimensionLength, -this.halfDimensionLength, this.halfDimensionLength); // Bottom Front Right
        for (int x = 4; x < 8; x++)
        {
            cubeCoords[x] = this.pos + corner;
            corner = Quaternion.Euler(0f, 90f, 0f) * corner;
        }

        octantLineRenderer.useWorldSpace = true;
        octantLineRenderer.SetVertexCount(16);
        octantLineRenderer.SetWidth(0.03f, 0.03f);

        octantLineRenderer.SetPosition(0, cubeCoords[0]);
        octantLineRenderer.SetPosition(1, cubeCoords[1]);
        octantLineRenderer.SetPosition(2, cubeCoords[2]);
        octantLineRenderer.SetPosition(3, cubeCoords[3]);
        octantLineRenderer.SetPosition(4, cubeCoords[0]);
        octantLineRenderer.SetPosition(5, cubeCoords[4]);
        octantLineRenderer.SetPosition(6, cubeCoords[5]);
        octantLineRenderer.SetPosition(7, cubeCoords[1]);

        octantLineRenderer.SetPosition(8, cubeCoords[5]);
        octantLineRenderer.SetPosition(9, cubeCoords[6]);
        octantLineRenderer.SetPosition(10, cubeCoords[2]);
        octantLineRenderer.SetPosition(11, cubeCoords[6]);
        octantLineRenderer.SetPosition(12, cubeCoords[7]);
        octantLineRenderer.SetPosition(13, cubeCoords[3]);
        octantLineRenderer.SetPosition(14, cubeCoords[7]);
        octantLineRenderer.SetPosition(15, cubeCoords[4]);
    }
}
