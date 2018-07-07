using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParcelGenerator : MonoBehaviour
{
	private List<Parcel> mParcels = new List<Parcel>();
	private List<RoadNode> mCurrentParcel = new List<RoadNode>();
	private List<RoadNode> mNodesForParcels = new List<RoadNode>();

	public void GenerateParcels(List<RoadNode> aNodes)
	{
		StopAllCoroutines();

		mParcels.Clear();
		mCurrentParcel.Clear();
		mNodesForParcels.Clear();

		StartCoroutine(Coroutine_GenerateParcels(aNodes));
	}

	public IEnumerator Coroutine_GenerateParcels(List<RoadNode> aNodes)
	{
		mNodesForParcels = aNodes.ToList();
		bool donedone = false;
		while (donedone == false)
		{
			ClearFilaments(ref mNodesForParcels);
			yield return new WaitForSeconds(1);
			if (mNodesForParcels.Count <= 2)
			{
				donedone = true;
				break;
			}
			mCurrentParcel.Clear();
			List<RoadNode> nodes = mNodesForParcels.OrderBy(i => i.mPosition.x).ToList();
			RoadNode startNode = nodes[0];
			RoadNode currentNode = startNode;
			Vector3 currentDirection = Vector3.forward;
			currentNode.mIsPath = true;
			RoadNode previousNode = currentNode;
			bool done = false;
			while (done == false)
			{
				currentNode.mConnections = currentNode.mConnections.OrderBy(i => Angle(currentNode.mPosition, i.mPosition, currentDirection)).ToList();
				if (currentNode == startNode)
					currentNode.mConnections.Reverse();
				RoadNode nextNode = currentNode.mConnections[0];
				for (int i = 0; i < currentNode.mConnections.Count; ++i)
				{
					if (currentNode.mConnections[i] == previousNode)
						continue;
					nextNode = currentNode.mConnections[i];
				}
				currentDirection = (currentNode.mPosition - nextNode.mPosition).normalized;
				previousNode = currentNode;
				currentNode = nextNode;
				currentNode.mIsPath = true;
				mCurrentParcel.Add(currentNode);
				if (currentNode == startNode)
					done = true;
				//yield return new WaitForSeconds(1);
			}
			FixParcel();
			mCurrentParcel.Last().mConnections.Remove(mCurrentParcel.First());
			mCurrentParcel.First().mConnections.Remove(mCurrentParcel.Last());
		
			yield return new WaitForSeconds(1);
		}
		yield return new WaitForSeconds(1);
	}

	private void ClearFilaments(ref List<RoadNode> aNodes)
	{
		bool done = false;
		while (done == false)
		{
			done = true;
			for (int i = 0; i < aNodes.Count; ++i)
			{
				if (aNodes[i].mConnections.Count <= 1)
				{
					done = false;
					foreach (RoadNode gnriegn in aNodes[i].mConnections)
					{
						if (gnriegn.mConnections.Contains(aNodes[i]) == true)
							gnriegn.mConnections.Remove(aNodes[i]);
					}
					aNodes.RemoveAt(i);
					--i;
				}
			}
		}
	}

	private float Angle(Vector3 aPointA, Vector3 aPointB, Vector3 aInitialAngle)
	{
		Vector2 A = new Vector2(aPointA.x, aPointA.z);
		Vector2 B = new Vector2(aPointB.x, aPointB.z);
		Vector2 dir = new Vector2(aInitialAngle.x, aInitialAngle.z);
		Vector2 AI = (dir - A).normalized;
		Vector2 AB = (B - A).normalized;
		float angle = Vector2.Angle(dir, AB);
		if (Vector3.Cross(dir, AB).z > 0.0f)
		{
			angle = 360.0f - angle;
		}
		return angle;
	}
	
	private void FixParcel()
	{
		int num = mCurrentParcel.Count;
		Vector3[] movedPoints = new Vector3[num];
		for (int i = 0; i < num; ++i)
		{
			int prev = i;
			int current = (i + 1) % num;
			int next = (i + 2) % num;
	
			Vector3 AB = (mCurrentParcel[current].mPosition - mCurrentParcel[prev].mPosition).normalized;
			Vector3 BC = (mCurrentParcel[next].mPosition - mCurrentParcel[current].mPosition).normalized;
			Vector3 normalAB = Quaternion.Euler(0.0f, 90.0f, 0.0f) * AB;
			Vector3 normalBC = Quaternion.Euler(0.0f, 90.0f, 0.0f) * BC;
	
			Vector3 p0 = mCurrentParcel[prev].mPosition + normalAB;
			Vector3 p1 = mCurrentParcel[current].mPosition + normalAB;
			Vector3 p2 = mCurrentParcel[current].mPosition + normalBC;
			Vector3 p3 = mCurrentParcel[next].mPosition + normalBC;
	
			Vector2 intersection = Vector2.zero;
			bool crossed = RayRayIntersection(
				new Vector2(p0.x, p0.z),
				new Vector2(p1.x, p1.z),
				new Vector2(p2.x, p2.z),
				new Vector2(p3.x, p3.z),
				out intersection
			);
			if (crossed == false)
				Debug.Log("!!!!");
	
			movedPoints[current] = new Vector3(intersection.x, 0.0f, intersection.y);
		}
		mParcels.Add(new Parcel { mColor = Random.ColorHSV(0, 1, 1, 1, 1, 1, 1, 1), mPoints = movedPoints.ToList() });
	}

	public bool RayRayIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, out Vector2 intersection)
	{
		intersection = Vector2.one * float.MinValue;
	
		float a1 = line1End.y - line1Start.y;
		float b1 = line1Start.x - line1End.x;
		float c1 = line1End.x * line1Start.y - line1Start.x * line1End.y;
	
		float a2 = line2End.y - line2Start.y;
		float b2 = line2Start.x - line2End.x;
		float c2 = line2End.x * line2Start.y - line2Start.x * line2End.y;
	
		float denom = a1 * b2 - a2 * b1;
		if (Mathf.Abs(denom) <= 0.000001f)
			return false;
	
		intersection.x = (b1 * c2 - b2 * c1) / denom;
		intersection.y = (a2 * c1 - a1 * c2) / denom;
		return true;
	}

	private void OnRenderObject()
	{
		LineDrawer.PushColor(Color.white);
		for (int i = 0; i < mNodesForParcels.Count; ++i)
		{
			for (int j = 0; j < mNodesForParcels[i].mConnections.Count; ++j)
			{
				LineDrawer.PushVertex(mNodesForParcels[i].mPosition);
				LineDrawer.PushVertex(mNodesForParcels[i].mConnections[j].mPosition);
			}
		}
		for (int i = 0; i < mParcels.Count; ++i)
		{
			Parcel parcel = mParcels[i];
			int num = parcel.mPoints.Count;
			LineDrawer.PushColor(parcel.mColor);
			for (int j = 0; j < num; ++j)
			{
				int current = j;
				int next = (j + 1) % num;
				LineDrawer.PushVertex(parcel.mPoints[current]);
				LineDrawer.PushVertex(parcel.mPoints[next]);
			}
		}
	}
}
