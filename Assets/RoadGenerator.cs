using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
	private List<RoadModule> mQueue = new List<RoadModule>();
	private List<RoadModule> mModules = new List<RoadModule>();
	private List<Vector3> mCrossings = new List<Vector3>();
	private Dictionary<Vector3, RoadNode> mNodes = new Dictionary<Vector3, RoadNode>();
	private RoadModule mQueryRoad = null;
	
	[SerializeField]
	private float mWorldSize = 256;
	[SerializeField]
	private bool mShowRoads = true;
	[SerializeField]
	private bool mShowParcels = true;

	private RoadNode GetNode(Vector3 aPoint)
	{
		Vector3 point = Vector3.zero;
		point.x = Mathf.Floor(aPoint.x * 100.0f) / 100.0f;
		point.z = Mathf.Floor(aPoint.z * 100.0f) / 100.0f;
		if (mNodes.ContainsKey(point) == false)
		{
			mNodes.Add(point, new RoadNode { mPosition = point });
		}
		return mNodes[point];
	}

	private void Start()
	{
		InitLists();
	}

	private void InitLists()
	{
		mQueue.Clear();
		mModules.Clear();
		mCrossings.Clear();
		mNodes.Clear();

		RoadModule initialRoadModule = new RoadModule();
		initialRoadModule.mPriority = 0;
		initialRoadModule.mPosition = Vector3.zero;
		initialRoadModule.mLength = 10.0f;
		initialRoadModule.mDirection = 0.0f;
		mQueue.Add(initialRoadModule);

		mQueue = mQueue.OrderBy(i => i.mPriority).ToList();

		//StopAllCoroutines();
	}

	private float accumulatedTime = 0.0f;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Q) == true)
		{
			StopAllCoroutines();
			InitLists();
		}
		if (Input.GetKeyDown(KeyCode.Space) == true)
		{
			StopAllCoroutines();
			StartCoroutine(Coroutine_Generate());
		}
		//if (mNodes.Count == 0)
		//{
		//	if (Input.GetKey(KeyCode.Z) == true)
		//	{
		//		accumulatedTime += Time.deltaTime;
		//		if (accumulatedTime > 1.0f / 60.0f)
		//		{
		//			for (int i = 0; i < 15; ++i)
		//			{
		//				Generate();
		//			}
		//			accumulatedTime -= 1.0f / 60.0f;
		//		}
		//	}
		//	if (Input.GetKeyDown(KeyCode.Space) == true)
		//	{
		//		Generate();
		//	}
		//}
		//else
		//{
		//	if (Input.GetKeyDown(KeyCode.Space) == true)
		//	{
		//		GetComponent<ParcelGenerator>().GenerateParcels(mNodes.Values.ToList());
		//	}
		//}
	}
	
	private IEnumerator Coroutine_Generate()
	{
		InitLists();
		while (mQueue.Count > 0)
		{
			Debug.Log("Generating");
			mQueryRoad = mQueue[0];
			mQueue.RemoveAt(0);
			eRoadQueryResult result = LocalConstraints(mQueryRoad);
			if (result != eRoadQueryResult.FAIL)
			{
				mModules.Add(mQueryRoad);
				if (result == eRoadQueryResult.SUCCESS)
					GlobalGoals(mQueryRoad);
			}
			yield return new WaitForEndOfFrame();
		}
		for (int i = 0; i < mModules.Count; ++i)
		{
			if (mModules[i].mLength <= 0.000001f)
				Debug.Log("Road with no length!");
			
			GetNode(mModules[i].mPosition).AddConnection(GetNode(mModules[i].End));
			GetNode(mModules[i].End).AddConnection(GetNode(mModules[i].mPosition));
			if (mModules[i].mBadRoad == true)
			{
				GetNode(mModules[i].mPosition).mBadNode = true;
				GetNode(mModules[i].End).mBadNode = true;
			}
			yield return new WaitForEndOfFrame();
		}
		GetComponent<ParcelGenerator>().GenerateParcels(mNodes.Values.ToList());
		yield return new WaitForEndOfFrame();
	}

	private eRoadQueryResult LocalConstraints(RoadModule aRoadModule)
	{
		eRoadQueryResult result = eRoadQueryResult.SUCCESS;
		bool valid = false;
		int debugID = -1;

		if (aRoadModule.End.x < -mWorldSize || aRoadModule.End.x > mWorldSize || aRoadModule.End.z < -mWorldSize || aRoadModule.End.z > mWorldSize)
			return eRoadQueryResult.FAIL;

		if (TestIntersection(aRoadModule) == true)
		{
			result = eRoadQueryResult.SUCCESS_AND_STOP;
			valid = true;
			debugID = 1;
		}

		if (valid == false && TestCrossing(aRoadModule) == true)
		{
			result = eRoadQueryResult.SUCCESS_AND_STOP;
			valid = true;
			debugID = 2;
		}

		if (valid == false && TestExtending(aRoadModule) == true)
		{
			result = eRoadQueryResult.SUCCESS_AND_STOP;
			valid = true;
			debugID = 3;
		}

		Vector2 start1 = new Vector2(aRoadModule.mPosition.x, aRoadModule.mPosition.z);
		Vector2 end1 = new Vector2(aRoadModule.End.x, aRoadModule.End.z);
		for (int i = 0; i < mModules.Count; ++i)
		{
			Vector2 start2 = new Vector2(mModules[i].mPosition.x, mModules[i].mPosition.z);
			Vector2 end2 = new Vector2(mModules[i].End.x, mModules[i].End.z);

			if ((start1 == start2 && end1 == end2) || (start1 == end2 && start2 == end1))
			{
				valid = false;
				aRoadModule.mBadRoad = true;
				Debug.Log("OVERLAPPING ROAD");
			}
			else
			{
				ShortenLine(ref start1, ref end1, 0.1f);
				ShortenLine(ref start2, ref end2, 0.1f);
				Vector2 intersection = Vector2.zero;
				if (NeroUtilities.LineLineIntersection(start1, end1, start2, end2, out intersection) == true)
				{
					valid = false;
					aRoadModule.mBadRoad = true;
					Debug.Log("BAD ROAD");
				}
			}
		}

		return result;
	}

	private void ShortenLine(ref Vector2 lhs, ref Vector2 rhs, float amount)
	{
		Vector2 delta = rhs - lhs;
		float distance = delta.magnitude;
		Vector2 direction = delta.normalized;
		lhs += direction * amount;
		rhs -= direction * amount;
	}

	private bool TestIntersection(RoadModule aRoadModule)
	{
		Vector2 closestIntersection = Vector2.one * float.MinValue;
		float shortestDistance = float.MaxValue;
		RoadModule intersectedRoad = null;
		Vector2 start1 = new Vector2(aRoadModule.mPosition.x, aRoadModule.mPosition.z);
		Vector2 end1 = new Vector2(aRoadModule.End.x, aRoadModule.End.z);
		start1 += (end1 - start1).normalized * 0.001f;
		Vector2 intersection = Vector2.zero;
		for (int i = 0; i < mModules.Count; ++i)
		{
			Vector2 start2 = new Vector2(mModules[i].mPosition.x, mModules[i].mPosition.z);
			Vector2 end2 = new Vector2(mModules[i].End.x, mModules[i].End.z);
			if (NeroUtilities.LineLineIntersection(start1, end1, start2, end2, out intersection) == true)
			{
				float distance = Vector2.Distance(start1, intersection);
				if (distance < shortestDistance)
				{
					closestIntersection = intersection;
					shortestDistance = distance;
					intersectedRoad = mModules[i];
				}
			}
		}

		if (intersectedRoad != null)
		{
			aRoadModule.mLength = Vector3.Distance(aRoadModule.mPosition, new Vector3(closestIntersection.x, 0.0f, closestIntersection.y));
			mCrossings.Add(aRoadModule.End);
			Debug.Log("TestIntersection -> Creating crossing.");
			if (aRoadModule.mLength < 0.000001f)
				Debug.LogWarning("TestIntersection -> Road segment length is 0.");

			float originalLength = intersectedRoad.mLength;
			float newLength = Vector3.Distance(intersectedRoad.mPosition, new Vector3(closestIntersection.x, 0.0f, closestIntersection.y));
			intersectedRoad.mLength = newLength;
			RoadModule newModule = new RoadModule(0, intersectedRoad.End, originalLength - newLength, intersectedRoad.mDirection);
			mModules.Add(newModule);
			return true;
		}
		return false;
	}

	private bool TestCrossing(RoadModule aRoadModule)
	{
		Vector2 closestPoint = Vector3.one * float.MinValue;
		float shortestDistance = float.MaxValue;
		Vector2 start = new Vector2(aRoadModule.mPosition.x, aRoadModule.mPosition.z);
		Vector2 end = new Vector2(aRoadModule.End.x, aRoadModule.End.z);
		Vector2 intersection = Vector2.zero;
		bool foundIntersection = false;
		for (int i = 0; i < mCrossings.Count; ++i)
		{
			Vector2 crossing = new Vector2(mCrossings[i].x, mCrossings[i].z);
			if (NeroUtilities.PointCircleIntersection(end, crossing, aRoadModule.mLength * 0.75f) == true)
			{
				float distance = Vector2.Distance(end, crossing);
				if (distance < shortestDistance && bgriegebri(start, crossing) == true)
				{
					foundIntersection = true;
					closestPoint = crossing;
					shortestDistance = distance;
				}
			}
		}
		if (foundIntersection == true)
		{
			aRoadModule.mLength = Vector2.Distance(start, closestPoint);
			Vector2 delta = closestPoint - start;
			aRoadModule.mDirection = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
			Debug.Log("TestCrossing -> Snapping to crossing.");
			if (aRoadModule.mLength < 0.000001f)
				Debug.LogWarning("TestCrossing -> Road segment length is 0.");
			return true;
		}
		return false;
	}

	private bool bgriegebri(Vector2 start1, Vector2 end1)
	{
		Vector2 intersection = Vector2.zero;
		for (int i = 0; i < mModules.Count; ++i)
		{
			Vector2 start2 = new Vector2(mModules[i].mPosition.x, mModules[i].mPosition.z);
			Vector2 end2 = new Vector2(mModules[i].End.x, mModules[i].End.z);
			if (NeroUtilities.LineLineIntersection(start1, end1, start2, end2, out intersection) == true)
				return false;
		}
		return true;
	}

	private bool TestExtending(RoadModule aRoadModule)
	{
		Vector2 closestIntersection = Vector2.one * float.MinValue;
		float shortestDistance = float.MaxValue;
		RoadModule intersectedRoad = null;
		Vector2 start1 = new Vector2(aRoadModule.mPosition.x, aRoadModule.mPosition.z);
		Vector2 end1 = new Vector2(
				aRoadModule.mPosition.x + Mathf.Cos(aRoadModule.mDirection * Mathf.Deg2Rad) * aRoadModule.mLength * 1.5f,
				aRoadModule.mPosition.z + Mathf.Sin(aRoadModule.mDirection * Mathf.Deg2Rad) * aRoadModule.mLength * 1.5f);
		start1 += (end1 - start1).normalized * 0.001f;
		Vector2 intersection = Vector2.zero;
		for (int i = 0; i < mModules.Count; ++i)
		{
			Vector2 start2 = new Vector2(mModules[i].mPosition.x, mModules[i].mPosition.z);
			Vector2 end2 = new Vector2(mModules[i].End.x, mModules[i].End.z);
			if (NeroUtilities.LineLineIntersection(start1, end1, start2, end2, out intersection) == true)
			{
				float distance = Vector2.Distance(start1, intersection);
				if (distance < shortestDistance)
				{
					closestIntersection = intersection;
					shortestDistance = distance;
					intersectedRoad = mModules[i];
				}
			}
		}
		if (intersectedRoad != null)
		{
			aRoadModule.mLength = Vector3.Distance(aRoadModule.mPosition, new Vector3(closestIntersection.x, 0.0f, closestIntersection.y));
			mCrossings.Add(aRoadModule.End);
			Debug.Log("TestExtending -> Creating crossing.");
			if (aRoadModule.mLength < 0.000001f)
				Debug.LogWarning("TestExtending -> Road segment length is 0.");

			float originalLength = intersectedRoad.mLength;
			float newLength = Vector3.Distance(intersectedRoad.mPosition, new Vector3(closestIntersection.x, 0.0f, closestIntersection.y));
			intersectedRoad.mLength = newLength;
			RoadModule newModule = new RoadModule(0, intersectedRoad.End, originalLength - newLength, intersectedRoad.mDirection);
			mModules.Add(newModule);
			return true;
		}
		return false;
	}

	private void GlobalGoals(RoadModule aRoadModule)
	{
		mQueue.Add(new RoadModule(aRoadModule.mPriority + 1, aRoadModule.End, 10.0f, aRoadModule.mDirection + Random.Range(-10.0f, 10.0f)));
		bool crossingCreated = false;
		int splitChance = 15;
		if (Random.Range(0, 100) < splitChance)
		{
			mQueue.Add(new RoadModule(aRoadModule.mPriority + 1, aRoadModule.End, 10.0f, aRoadModule.mDirection + 90.0f + Random.Range(-10.0f, 10.0f)));
			crossingCreated = true;
		}
		if (Random.Range(0, 100) < splitChance)
		{
			mQueue.Add(new RoadModule(aRoadModule.mPriority + 1, aRoadModule.End, 10.0f, aRoadModule.mDirection - 90.0f + Random.Range(-10.0f, 10.0f)));
			crossingCreated = true;
		}
		if (crossingCreated == true)
		{
			mCrossings.Add(aRoadModule.End);
			Debug.Log("GlobalGoals -> Creating crossing.");
		}
	}
	
	private void OnRenderObject()
	{
		if (mShowRoads == true)
			OnRenderObjectRoads();
	}

	private void OnRenderObjectRoads()
	{
		for (int i = 0; i < mModules.Count; ++i)
		{
			Color color = Color.white;
			if (mModules[i].mBadRoad == true)
				color = Color.magenta;
			else if (mModules[i] == mQueryRoad)
				color = Color.green;
			LineDrawer.PushColor(color);

			Vector3 point0 = mModules[i].mPosition +
				new Vector3(
					Mathf.Cos((mModules[i].mDirection - 90.0f) * Mathf.Deg2Rad),
					0.0f,
					Mathf.Sin((mModules[i].mDirection - 90.0f) * Mathf.Deg2Rad));
			Vector3 point1 = mModules[i].mPosition +
				new Vector3(
					Mathf.Cos((mModules[i].mDirection + 90.0f) * Mathf.Deg2Rad),
					0.0f,
					Mathf.Sin((mModules[i].mDirection + 90.0f) * Mathf.Deg2Rad));
			Vector3 point2 = mModules[i].End;
			LineDrawer.PushVertex(point0);
			LineDrawer.PushVertex(point1);
			LineDrawer.PushVertex(point1);
			LineDrawer.PushVertex(point2);
			LineDrawer.PushVertex(point2);
			LineDrawer.PushVertex(point0);
		}
		for (int i = 0; i < mQueue.Count; ++i)
		{
			LineDrawer.PushColor(Color.red);

			Vector3 point0 = mQueue[i].mPosition +
				new Vector3(
					Mathf.Cos((mQueue[i].mDirection - 90.0f) * Mathf.Deg2Rad),
					0.0f,
					Mathf.Sin((mQueue[i].mDirection - 90.0f) * Mathf.Deg2Rad));
			Vector3 point1 = mQueue[i].mPosition +
				new Vector3(
					Mathf.Cos((mQueue[i].mDirection + 90.0f) * Mathf.Deg2Rad),
					0.0f,
					Mathf.Sin((mQueue[i].mDirection + 90.0f) * Mathf.Deg2Rad));
			Vector3 point2 = mQueue[i].End;
			LineDrawer.PushVertex(point0);
			LineDrawer.PushVertex(point1);
			LineDrawer.PushVertex(point1);
			LineDrawer.PushVertex(point2);
			LineDrawer.PushVertex(point2);
			LineDrawer.PushVertex(point0);
		}
		LineDrawer.PushColor(Color.white);
		for (int i = 0; i < mCrossings.Count; ++i)
		{
			float step = (Mathf.PI * 2.0f) / 8.0f;
			for (int j = 0; j < 8; ++j)
			{
				float cos0 = Mathf.Cos(step * j);
				float sin0 = Mathf.Sin(step * j);
				float cos1 = Mathf.Cos(step * (j + 1));
				float sin1 = Mathf.Sin(step * (j + 1));
				LineDrawer.PushVertex(mCrossings[i] + new Vector3(cos0, 0, sin0));
				LineDrawer.PushVertex(mCrossings[i] + new Vector3(cos1, 0, sin1));
			}
		}
	}
}
