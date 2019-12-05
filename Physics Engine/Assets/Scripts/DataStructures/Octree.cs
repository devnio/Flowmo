using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: See if this is necessary.

public enum OctreeIndex
{
    BottomLeftFront = 0, //000,
    BottomRightFront = 2, //010,
    BottomRightBack = 3, //011,
    BottomLeftBack = 1, //001,
    TopLeftFront = 4, //100,
    TopRightFront = 6, //110,
    TopRightBack = 7, //111,
    TopLeftBack = 5, //101,
}

public class Octree<TType>
{
    private OctreeNode<TType> node;  // root node
    private int depth;

    public Octree(Vector3 position, float size, int depth)
    {
        node = new OctreeNode<TType>(position, size);
        this.depth = depth;
        node.Subdivide(this.depth);
    }

    public class OctreeNode<TType>
    {
        Vector3 position;
        float size;
        OctreeNode<TType>[] subNodes;
        IList<TType> value;

        public OctreeNode(Vector3 pos, float size)
        {
            this.position = pos;
            this.size = size;
        }

        /// <summary>
        /// 8 subnodes
        /// </summary>
        public IEnumerable<OctreeNode<TType>> Nodes
        {
            get { return subNodes; }
        }

        public Vector3 Position
        {
            get { return position; }
        }

        public float Size
        {
            get { return size; }
        }

        public void Subdivide(int depth = 0)
        {
            subNodes = new OctreeNode<TType>[8];
            for (int i = 0; i < subNodes.Length; ++i)
            {
                Vector3 newPos = position;
                if ((i & 4) == 4)
                {
                    newPos.y += size * 0.25f;
                }
                else
                {
                    newPos.y -= size * 0.25f;
                }

                if ((i & 2) == 2)
                {
                    newPos.x += size * 0.25f;
                }
                else
                {
                    newPos.x -= size * 0.25f;
                }

                if ((i & 1) == 1)
                {
                    newPos.z += size * 0.25f;
                }
                else
                {
                    newPos.z -= size * 0.25f;
                }

                subNodes[i] = new OctreeNode<TType>(newPos, size * 0.5f);
                if (depth > 0)
                {
                    subNodes[i].Subdivide(depth - 1);
                }
            }
        }

        public bool IsLeaf()
        {
            return subNodes == null;
        }
    }


    private int GetIndexOfPosition(Vector3 lookupPosition, Vector3 nodePosition)
    {
        int index = 0;

        index |= lookupPosition.y > nodePosition.y ? 4 : 0;  // set the flag for y
        index |= lookupPosition.x > nodePosition.x ? 2 : 0;  // set the flag for x
        index |= lookupPosition.z > nodePosition.z ? 1 : 0;  // set the flag for z

        return index;
    }

    
    /// <summary>
    /// Check where this vertex should be allocated.
    /// </summary>
    public void UpdateNodePosition(Vector3 vertexPosition, BaseCollider baseCollider)
    {
        //node.
        //node.s
        //node.node  

        // (1) Get node where it should be stored
        OctreeNode<TType> itNode = GetRoot();

        for(int i = 0; i <= depth; i++)
        {
            int occupiedNodeIdx = GetIndexOfPosition(vertexPosition, itNode.Position);
            //itNode = itNode.Nodes[occupiedNodeIdx];
        }

        // (2) Once the Node is found
        // (2.1) Check where this basecollider was stored previously 
        // (2.2) if it isn't stored in the current node add the BaseCollider to its IList

        // PROBLEM: an object (cube/sphere) can be inside multiple nodes
        //          checking only the extents isn't enough, because it might extend
        //          through multiple nodes (should use ray coverage?)
    }

    public OctreeNode<TType> GetRoot()
    {
        return node;
    }

    public int GetDepth()
    {
        return depth;
    }
}