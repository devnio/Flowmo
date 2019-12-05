using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OctreeManager : Singleton<OctreeManager>
{
    // PRIVATE
    private Octree<BaseCollider> Octree;

    // PUBLIC
    [Header("Tweakable Parameters.")]
    public Vector3 OctreePosition;
    public float OctreeSize;
    public int OctreeDepth;
    public Color OctreeColor;

    void Start()
    {
        this.Octree = new Octree<BaseCollider>(this.OctreePosition, this.OctreeSize, this.OctreeDepth);
    }


    //===========================
    // REFERENCE
    //===========================


    //===========================
    // DRAW
    //===========================
    private void DrawOctree(Octree<BaseCollider>.OctreeNode<BaseCollider> node, float colorAlpha = 1f)
    {
        Color c = this.OctreeColor;
        c.a = colorAlpha;
        Gizmos.color = c;
        Gizmos.DrawWireCube(node.Position, node.Size * Vector3.one);

        if (!node.IsLeaf())
        {
            foreach (var sNode in node.Nodes)
            {
                DrawOctree(sNode, colorAlpha - (1f / (this.OctreeDepth + 2)));
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (this.Octree != null)
        {
            this.DrawOctree(this.Octree.GetRoot());
        }
    }

}
