using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Bottom square: A,B,C,D, Top Square: E,F,G,H (clockwise) (following this https://i.stack.imgur.com/DXXYP.png convention)
/// </summary>
public struct Cube
{
    public Vector3[] vertices; //A, B, C, D, E, F, G, H
}

public enum CubeIdx
{
    A = 0,
    B = 1,
    C = 2,
    D = 3,
    E = 4,
    F = 5,
    G = 6,
    H = 7,
}

/*

    F--------G   
 .' |     .' |   
E---+-- H'   |   
|   |   |    |   
|  ,B --+--- C   
|.'     |  .'    
A ------ D'

*/

public class ColliderBox : BaseCollider
{
    // Previous cube
    public Cube prevCube;
    public Cube cube;

    [HideInInspector]
	public Vector3 _xyzLength;
	public Vector3 xyzLength;

	[HideInInspector]
	// Contains min and max which are computed from the displaceCenter and xyzLength
	public AABB aabb;

	private void Start()
    {
        // Assign unique Id
        this.AssignUniqueId();

        // Add to collision manager
        CollisionManager.Instance.AddCollider(this);
    }

    public override void UpdateColliderPose(Vector3 displace)
    {
        this.SavePrevCubeState();
        this.UpdateOBB(displace);
    }

    /// <summary>
	/// Called before computing the AABB bounds.
	/// </summary>
	private void UpdateXYZLengthScaleInvariant()
	{
		_xyzLength.x = xyzLength.x * this.transform.localScale.x;
		_xyzLength.y = xyzLength.y * this.transform.localScale.y;
		_xyzLength.z = xyzLength.z * this.transform.localScale.z;
	}

	/// <summary>
	/// Compute the bounds of the AABB.
	/// </summary>
	/// <param name="min"></param>
	/// <returns></returns>
	private Vector3 ComputeMinMax(bool min)
	{
        this.UpdateXYZLengthScaleInvariant();
        float x, z, y;
		if (min)
		{
			x = _center.x - _xyzLength.x / 2;
			y = _center.y - _xyzLength.y / 2;
			z = _center.z - _xyzLength.z / 2;
		}
		else
		{
			x = _center.x + _xyzLength.x / 2;
			y = _center.y + _xyzLength.y / 2;
			z = _center.z + _xyzLength.z / 2;
		}

		return new Vector3(x, y, z);
	}

	/// <summary>
	/// Compute AABB positions (hence without considering orientation) based on current position of game object.
	/// </summary>
	public void UpdateAABBPosition(Vector3 displace)
	{
        // displace used for finding min penetration distance for collision
        this._center = this.transform.TransformPoint(this.displaceCenter + displace);
		aabb._min = ComputeMinMax(true);
		aabb._max = ComputeMinMax(false);
	}

    /// <summary>
	/// Returns axis aligned cube containing the vertex positions.
	/// </summary>
	/// <returns></returns>
    public Cube GetAABBVertices()
    {
        Vector3 right = _xyzLength.x * Vector3.right;
        Vector3 up = _xyzLength.y * Vector3.up;
        Vector3 forward = _xyzLength.z * Vector3.forward;

        Cube res = new Cube
        {
            vertices = new Vector3[8]
        };

        res.vertices[0] = this.aabb._min;
        res.vertices[1] = this.aabb._min + forward;
        res.vertices[2] = res.vertices[1] + right;
        res.vertices[3] = this.aabb._min + right;

        res.vertices[4] = this.aabb._min + up;
        res.vertices[5] = res.vertices[4] + forward;
        res.vertices[6] = res.vertices[5] + right;
        res.vertices[7] = res.vertices[4] + right;

        return res;
    }

    /// <summary>
    /// Apply local orientation to AABB cube.
    /// </summary>
	public void UpdateCubeOrientation()
	{
		for (int i = 0; i < this.cube.vertices.Length; i++)
		{
			this.cube.vertices[i] = Util.RotatePointAroundPivot(_center, this.cube.vertices[i], this.transform.rotation.eulerAngles);
		}
	}

    private void SavePrevCubeState()
    {
        this.prevCube = this.cube;
    }

	/// <summary>
	/// Use the created AABB and change the orientation to match the one from the game object.
	/// </summary>
	private void UpdateOBB(Vector3 displace)
	{
		this.UpdateAABBPosition(displace);
		this.cube = this.GetAABBVertices();
		this.UpdateCubeOrientation();
	}

	//=========================
	// UPDATE VARIABLES
	//=========================
	/// <summary>
	/// Called when changing center or xyzLength in the inspector.
	/// </summary>
	private void OnValidate()
    {
        UpdateOBB(Vector3.zero);
    }

	//=========================
	// DRAW
	//=========================
	private void OnDrawGizmos()
	{
		Color col = Color.green;
		col.a = 0.5f;
		Gizmos.color = col;

        if (this.transform.hasChanged)
        {
            this.UpdateColliderPose(Vector3.zero);
            this.transform.hasChanged = false;
        }

        DrawRectangleAABB();
	}

	private void DrawRectangleAABB()
	{
		void Draw(CubeIdx From, CubeIdx To)
		{
			int fromIndex = (int)From;
			int toIndex = (int)To;
			Gizmos.DrawLine(this.cube.vertices[fromIndex], this.cube.vertices[toIndex]);
		}

		Draw(CubeIdx.A, CubeIdx.B); // A -> B
		Draw(CubeIdx.A, CubeIdx.D); // A -> D
		Draw(CubeIdx.A, CubeIdx.E); // A -> E

		Draw(CubeIdx.G, CubeIdx.F); // G -> F
		Draw(CubeIdx.G, CubeIdx.C); // G -> C
		Draw(CubeIdx.G, CubeIdx.H); // G -> H

		Draw(CubeIdx.E, CubeIdx.F); // E -> F
		Draw(CubeIdx.E, CubeIdx.H); // E -> H

		Draw(CubeIdx.C, CubeIdx.B); // C -> B
		Draw(CubeIdx.C, CubeIdx.D); // C -> D

		Draw(CubeIdx.B, CubeIdx.F); // B -> F
		Draw(CubeIdx.D, CubeIdx.H); // D -> H

        foreach(Vector3 v in this.cube.vertices)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(v, 0.05f);
        }
	}
}
