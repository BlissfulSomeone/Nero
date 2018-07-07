using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bgbehag : MonoBehaviour
{
	private Material mLineMaterial;

	private void Start()
	{
		Shader shader = Shader.Find("Hidden/Internal-Colored");
		mLineMaterial = new Material(shader);
		mLineMaterial.hideFlags = HideFlags.HideAndDontSave;
		mLineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
		mLineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
		mLineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
		mLineMaterial.SetInt("_ZWrite", 0);
	}

	private Vector3[] mOriginalVertices = new Vector3[4];
	private Vector3[] mMovedVertices = new Vector3[4];

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space) == true)
		{
			StopAllCoroutines();

			mOriginalVertices[0] = Vector3.forward * 5.0f + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
			mOriginalVertices[1] = Vector3.right * 5.0f + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
			mOriginalVertices[2] = Vector3.back * 5.0f + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
			mOriginalVertices[3] = Vector3.left * 5.0f + new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));

			for (int i = 0; i < 4; ++i)
			{
				mMovedVertices[i] = mOriginalVertices[i];
			}

			mCurrentIndex = 0;
			StartCoroutine(FixVertices());
		}
	}

	private int mCurrentIndex = 0;

	private IEnumerator FixVertices()
	{
		yield return new WaitForSeconds(1.0f);
		while (mCurrentIndex < 4)
		{
			int prev = mCurrentIndex;
			int current = (mCurrentIndex + 1) % 4;
			int next = (mCurrentIndex + 2) % 4;

			Vector3 AB = (mOriginalVertices[current] - mOriginalVertices[prev]).normalized;
			Vector3 BC = (mOriginalVertices[next] - mOriginalVertices[current]).normalized;
			Vector3 normalAB = Quaternion.Euler(0.0f, 90.0f, 0.0f) * AB;
			Vector3 normalBC = Quaternion.Euler(0.0f, 90.0f, 0.0f) * BC;

			Vector3 p0 = mOriginalVertices[prev] + normalAB;
			Vector3 p1 = mOriginalVertices[current] + normalAB;
			Vector3 p2 = mOriginalVertices[current] + normalBC;
			Vector3 p3 = mOriginalVertices[next] + normalBC;

			Vector2 intersection = Vector2.zero;
			//LineLineIntersection(Vec2(p0), Vec2(p1), Vec2(p2), Vec2(p3), out intersection);
			RayRayIntersection(Vec2(p0), Vec2(p1), Vec2(p2), Vec2(p3), out intersection);

			mMovedVertices[current] = Vec3(intersection);

			++mCurrentIndex;

			yield return new WaitForSeconds(1.0f);
		}
		yield return new WaitForEndOfFrame();
	}

	private Vector2 Vec2(Vector3 vector)
	{
		return new Vector2(vector.x, vector.z);
	}

	private Vector3 Vec3(Vector2 vector)
	{
		return new Vector3(vector.x, 0.0f, vector.y);
	}

	private bool LineLineIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, out Vector2 intersection)
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

	private void OnRenderObject()
	{
		mLineMaterial.SetPass(0);
		GL.PushMatrix();
		GL.MultMatrix(transform.localToWorldMatrix);
		GL.Begin(GL.LINES);
		GL.Color(Color.white);
		for (int i = 0; i < 4; ++i)
		{
			GL.Vertex(mOriginalVertices[i]);
			GL.Vertex(mOriginalVertices[(i + 1) % 4]);
		}
		GL.Color(Color.red);
		for (int i = 0; i < 4; ++i)
		{
			GL.Vertex(mMovedVertices[i]);
			GL.Vertex(mMovedVertices[(i + 1) % 4]);
		}
		GL.End();
		GL.PopMatrix();
	}
}
