using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eRoadQueryResult
{
	SUCCESS,
	SUCCESS_AND_STOP,
	FAIL,
}

public class RoadModule
{
	public int mPriority;
	public Vector3 mPosition;
	public float mLength;
	public float mDirection;
	public bool mBadRoad = false;

	public Vector3 End
	{
		get
		{
			return mPosition + new Vector3(Mathf.Cos(mDirection * Mathf.Deg2Rad), 0.0f, Mathf.Sin(mDirection * Mathf.Deg2Rad)) * mLength;
		}
	}

	public RoadModule() { }

	public RoadModule(int aPriority, Vector3 aPosition, float aLength, float aDirection)
	{
		mPriority = aPriority;
		mPosition = aPosition;
		mLength = aLength;
		mDirection = Mathf.Repeat(aDirection, 360.0f);
	}
}

public class RoadNode
{
	public Vector3 mPosition;
	public List<RoadNode> mConnections = new List<RoadNode>();
	public bool mIsCursor = false;
	public float mF = 0.0f;
	public float mG = 0.0f;
	public float mH = 0.0f;
	public RoadNode mParent = null;
	public bool mIsVisited = false;
	public bool mIsPath = false;
	public bool mBadNode = false;

	public void AddConnection(RoadNode aNode)
	{
		if (mConnections.Contains(aNode) == false && aNode.mConnections.Contains(this) == false)
		{
			mConnections.Add(aNode);
			aNode.mConnections.Add(this);
		}
	}
}

public class Parcel
{
	public Color mColor;
	public List<Vector3> mPoints = new List<Vector3>();
}
