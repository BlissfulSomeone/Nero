using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineDrawer : MonoBehaviour
{
	private static Vector3 NONE_VECTOR = new Vector3(float.MinValue, float.MinValue, float.MinValue);

	private class DrawerData
	{
		public Vector3 point = NONE_VECTOR;
		public Color color = Color.clear;
	}

	private Material mLineMaterial;

	private static List<DrawerData> mVertices = new List<DrawerData>();

	private void Awake()
	{
		Shader shader = Shader.Find("Hidden/Internal-Colored");
		mLineMaterial = new Material(shader);
		mLineMaterial.hideFlags = HideFlags.HideAndDontSave;
		mLineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		mLineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		mLineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		mLineMaterial.SetInt("_ZWrite", 0);
	}

	public static void PushColor(Color c)
	{
		mVertices.Add(new DrawerData { color = c });
	}

	public static void PushVertex(Vector3 v)
	{
		mVertices.Add(new DrawerData { point = v });
	}

	private void OnRenderObject()
	{
		mLineMaterial.SetPass(0);
		GL.PushMatrix();
		GL.MultMatrix(transform.localToWorldMatrix);
		GL.Begin(GL.LINES);
		foreach (DrawerData data in mVertices)
		{
			if (data.point != NONE_VECTOR)
				GL.Vertex(data.point);
			else if (data.color != Color.clear)
				GL.Color(data.color);
		}
		GL.End();
		GL.PopMatrix();
		mVertices.Clear();
	}
}
