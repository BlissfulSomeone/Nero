using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SimpleRoadGenerator : MonoBehaviour
{
	private List<RoadModule> mModules = new List<RoadModule>();
	private Dictionary<Vector3, RoadNode> mNodes = new Dictionary<Vector3, RoadNode>();

	[SerializeField]
	private bool mShowRoads = true;

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
		for (int x = 0; x < 10; ++x)
		{
			for (int z = 0; z < 6; ++z)
			{
				mModules.Add(new RoadModule(0, new Vector3(x * 6, 0, z * 4), 6, 0.0f));
				mModules.Add(new RoadModule(0, new Vector3(x * 6, 0, z * 4), 4, 90.0f));
			}
		}
		foreach (RoadModule module in mModules)
		{
			GetNode(module.mPosition).AddConnection(GetNode(module.End));
			GetNode(module.End).AddConnection(GetNode(module.mPosition));
		}
		GetComponent<ParcelGenerator>().GenerateParcels(mNodes.Values.ToList());
	}

	private void OnRenderObject()
	{
		if (mShowRoads == true)
			OnRenderObjectRoads();
	}

	private void OnRenderObjectRoads()
	{
		LineDrawer.PushColor(Color.white);
		for (int i = 0; i < mModules.Count; ++i)
		{
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
	}
}
