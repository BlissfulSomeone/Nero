using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NeroUtilities
{
	public static bool LineLineIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, out Vector2 intersection)
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

	public static bool RayRayIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, out Vector2 intersection)
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

	private static float Angle(Vector3 aPointA, Vector3 aPointB, Vector3 aInitialAngle)
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

	public static bool PointCircleIntersection(Vector2 point, Vector2 circleCenter, float circleRadius)
	{
		return (Vector2.Distance(circleCenter, point) < circleRadius);
	}
}
