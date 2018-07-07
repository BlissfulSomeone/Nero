using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
	private enum eRoadQueryResult
	{
		SUCCESS,
		SUCCESS_AND_STOP,
		FAIL,
	}

	private class RoadModule
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

	private class RoadNode
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

	private class Parcel
	{
		public Color mColor;
		public List<Vector3> mPoints = new List<Vector3>();
	}

	private List<RoadModule> mQueue = new List<RoadModule>();
	private List<RoadModule> mModules = new List<RoadModule>();
	private List<Vector3> mCrossings = new List<Vector3>();
	private Dictionary<Vector3, RoadNode> mNodes = new Dictionary<Vector3, RoadNode>();
	RoadModule mQueryRoad = null;

	private Material mLineMaterial;

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

		// Unity has a built-in shader that is useful for drawing
		// simple colored things.
		Shader shader = Shader.Find("Hidden/Internal-Colored");
		mLineMaterial = new Material(shader);
		mLineMaterial.hideFlags = HideFlags.HideAndDontSave;
		// Turn on alpha blending
		mLineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		mLineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		// Turn backface culling off
		mLineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		// Turn off depth writes
		mLineMaterial.SetInt("_ZWrite", 0);
	}

	private void InitLists()
	{
		mQueue.Clear();
		mModules.Clear();
		mCrossings.Clear();
		mNodes.Clear();
		mParcels.Clear();
		mCurrentParcel.Clear();

		RoadModule initialRoadModule = new RoadModule();
		initialRoadModule.mPriority = 0;
		initialRoadModule.mPosition = Vector3.zero;
		initialRoadModule.mLength = 10.0f;
		initialRoadModule.mDirection = 0.0f;
		mQueue.Add(initialRoadModule);

		mQueue = mQueue.OrderBy(i => i.mPriority).ToList();

		StopAllCoroutines();
	}

	float accumulatedTime = 0.0f;

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Q) == true)
		{
			InitLists();
		}
		if (mNodes.Count == 0)
		{
			if (Input.GetKey(KeyCode.Z) == true)
			{
				accumulatedTime += Time.deltaTime;
				if (accumulatedTime > 1.0f / 60.0f)
				{
					for (int i = 0; i < 15; ++i)
					{
						Generate();
					}
					accumulatedTime -= 1.0f / 60.0f;
				}
			}
			if (Input.GetKeyDown(KeyCode.Space) == true)
			{
				Generate();
			}
		}
		else
		{
			if (Input.GetKeyDown(KeyCode.Space) == true && mParcels.Count == 0)
			{
				StopAllCoroutines();
				foreach (KeyValuePair<Vector3, RoadNode> kvp in mNodes)
				{
					kvp.Value.mIsPath = false;
				}
				StartCoroutine(GenerateParcels());
			}
		}
	}
	
	private void Generate()
	{
		if (mQueue.Count > 0)
		{
			mQueryRoad = mQueue[0];
			mQueue.RemoveAt(0);
			eRoadQueryResult result = LocalConstraints(mQueryRoad);
			if (result != eRoadQueryResult.FAIL)
			{
				mModules.Add(mQueryRoad);
				if (result == eRoadQueryResult.SUCCESS)
					GlobalGoals(mQueryRoad);
			}
		}
		else if (mNodes.Count == 0)
		{
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
			}
		}
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
				if (LineLineIntersection(start1, end1, start2, end2, out intersection) == true)
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
			if (LineLineIntersection(start1, end1, start2, end2, out intersection) == true)
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
			if (PointCircleIntersection(end, crossing, aRoadModule.mLength * 0.75f) == true)
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
			if (LineLineIntersection(start1, end1, start2, end2, out intersection) == true)
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
			if (LineLineIntersection(start1, end1, start2, end2, out intersection) == true)
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
	
	private List<RoadNode> mCurrentParcel = new List<RoadNode>();
	private List<Parcel> mParcels = new List<Parcel>();

	private IEnumerator GenerateParcels()
	{
		List<RoadNode> nodesForParcels = mNodes.Values.ToList();
		bool donedone = false;
		while (donedone == false)
		{
			ClearFilaments(ref nodesForParcels);
			if (nodesForParcels.Count <= 2)
			{
				donedone = true;
				break;
			}
			mCurrentParcel.Clear();
			List<RoadNode> nodes = nodesForParcels.OrderBy(i => i.mPosition.x).ToList();
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
				yield return new WaitForEndOfFrame();
			}
			FixParcel();
			mCurrentParcel[0].mConnections.Remove(mCurrentParcel[1]);
			mCurrentParcel[1].mConnections.Remove(mCurrentParcel[0]);

			yield return new WaitForEndOfFrame();
		}
		yield return new WaitForEndOfFrame();
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
					GetNode(aNodes[i].mPosition).mIsVisited = true;
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
		mNodes.Clear();
		foreach (RoadNode node in aNodes)
		{
			mNodes.Add(node.mPosition, node);
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

	public bool LineLineIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, out Vector2 intersection)
	{
		intersection.x = float.MinValue;
		intersection.y = float.MinValue;

		float s1_x, s1_y, s2_x, s2_y;

		s1_x = line1End.x - line1Start.x;
		s1_y = line1End.y - line1Start.y;

		s2_x = line2End.x - line2Start.x;
		s2_y = line2End.y - line2Start.y;

		float s, t;
		s = (-s1_y * (line1Start.x - line2Start.x) + s1_x * (line1Start.y - line2Start.y)) / (-s2_x * s1_y + s1_x * s2_y);
		t = (s2_x * (line1Start.y - line2Start.y) - s2_y * (line1Start.x - line2Start.x)) / (-s2_x * s1_y + s1_x * s2_y);

		if (0 < s && s < 1 && 0 < t && t < 1)
		{
			intersection.x = line1Start.x + (t * s1_x);
			intersection.y = line1Start.y + (t * s1_y);
			return true;
		}

		return false;
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

	public bool PointCircleIntersection(Vector2 point, Vector2 circleCenter, float circleRadius)
	{
		return (Vector2.Distance(circleCenter, point) < circleRadius);
	}
	
	private void OnRenderObject()
	{
		mLineMaterial.SetPass(0);
		GL.PushMatrix();
		GL.MultMatrix(transform.localToWorldMatrix);
		GL.Begin(GL.LINES);
		if (mShowRoads == true)
			OnRenderObjectRoads();
		if (mShowParcels == true)
			OnRenderObjectNodes();
		GL.End();
		GL.PopMatrix();
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
			GL.Color(color);

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
			GL.Vertex(point0);
			GL.Vertex(point1);
			GL.Vertex(point1);
			GL.Vertex(point2);
			GL.Vertex(point2);
			GL.Vertex(point0);
		}
		for (int i = 0; i < mQueue.Count; ++i)
		{
			GL.Color(Color.red);

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
			GL.Vertex(point0);
			GL.Vertex(point1);
			GL.Vertex(point1);
			GL.Vertex(point2);
			GL.Vertex(point2);
			GL.Vertex(point0);
		}
		GL.Color(Color.white);
		for (int i = 0; i < mCrossings.Count; ++i)
		{
			float step = (Mathf.PI * 2.0f) / 8.0f;
			for (int j = 0; j < 8; ++j)
			{
				float cos0 = Mathf.Cos(step * j);
				float sin0 = Mathf.Sin(step * j);
				float cos1 = Mathf.Cos(step * (j + 1));
				float sin1 = Mathf.Sin(step * (j + 1));
				GL.Vertex(mCrossings[i] + new Vector3(cos0, 0, sin0));
				GL.Vertex(mCrossings[i] + new Vector3(cos1, 0, sin1));
			}
		}
	}
	
	private void OnRenderObjectNodes()
	{
		for (int i = 0; i < mParcels.Count; ++i)
		{
			Parcel parcel = mParcels[i];
			int num = parcel.mPoints.Count;
			GL.Color(parcel.mColor);
			for (int j = 0; j < num; ++j)
			{
				int current = j;
				int next = (j + 1) % num;
				GL.Vertex(parcel.mPoints[current]);
				GL.Vertex(parcel.mPoints[next]);
			}
		}
	}
}
