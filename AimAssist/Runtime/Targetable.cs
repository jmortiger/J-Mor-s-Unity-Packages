using UnityEngine;
using JMor.Utility.Input;

namespace JMor.AimAssist
{
	// TODO: Create 3D variant.
	[RequireComponent(typeof(Collider2D))]
	public class Targetable : MonoBehaviour
	{
		#region Fields and Properties
		public new Collider2D collider;
		// [Tooltip("The strength of the correction.")]
		// [Range(0, 1)]
		// public float correctionStrength = 1;

		// [Tooltip("The strength of the correction.")]
		public MutatorMode mutatorMode = null;

		[Tooltip("Tells targeters to use the mutator defined above over their own, even if they would normally prioritize their own.")]
		public bool forceMyMutator = false;

		public Vector2 Center { get => collider.bounds.center; }
		public Vector3 CenterV3 { get => collider.bounds.center; }
		#endregion

		#region Unity Methods
		void Reset()
		{
			collider = GetComponent<Collider2D>();
		}

		void Start()
		{
			collider = collider != null ? collider : GetComponent<Collider2D>();
		}

		//public void DrawGizmos()
		//private void OnDrawGizmos/*Selected*/()
		//{
		//	var targeters = FindObjectsOfType<Targeter>();
		//	for (int i = 0; i < targeters.Length; i++)
		//		DrawGizmos(targeters[i]);
		//}
		public void DrawGizmos(Targeter targeter, bool logStatements = false)
		{
			var from = targeter.transform.position;

			var fV2 = (Vector2)from;
			Gizmos.DrawRay(collider.bounds.center, GetVectorToSide(fV2, true));
			Gizmos.DrawRay(collider.bounds.center, GetVectorToSide(fV2, false));

			var r = GetInputRange(from, Vector2.right);
			if (logStatements) Debug.Log($"{name} Angle Range: {r.x} - {r.y}");
			var fromToCursorDisplacement = MouseHelper.WorldPosition - (Vector2)from;
			var mAngle = Vector2.SignedAngle(Vector2.right, fromToCursorDisplacement);
			mAngle = (mAngle < 0) ? mAngle + 360 : mAngle;
			Gizmos.color = (mAngle > r.x && mAngle < r.y) ? Color.red : Color.white;
			Gizmos.DrawLine(from, CenterV3);

			Gizmos.color = Color.green;
			var p = GetExtremePoint(from, true);
			if (logStatements) Debug.Log($"{name}'s PointRight: {p}");
			Gizmos.DrawSphere(p, .1f);
			Gizmos.color = (mAngle > r.x && mAngle < r.y) ? Color.red : Color.green;
			Gizmos.DrawLine(from, from + Quaternion.Euler(0, 0, r.x) * Vector2.right * (p - fV2).magnitude);

			Gizmos.color = Color.blue;
			p = GetExtremePoint(from);
			if (logStatements) Debug.Log($"{name}'s PointLeft: {p}");
			Gizmos.DrawSphere(p, .1f);
			Gizmos.color = (mAngle > r.x && mAngle < r.y) ? Color.red : Color.blue;
			Gizmos.DrawLine(from, from + Quaternion.Euler(0, 0, r.y) * Vector2.right * (p - fV2).magnitude);
			Gizmos.color = Color.white;
		}
		#endregion

		#region Core Methods
		/// <summary>
		/// Gets a vector perpendicular to the line defined by a given position to this object's center.
		/// </summary>
		/// <param name="from">The position to get the perpendicular vectors from.</param>
		/// <param name="fromMinAngle">Whether to get the perpendicular vector with the smallest absolute angle or not.</param>
		/// <returns>A <see cref="Vector2"/> representing a unit vector directed 90degrees from (<see cref="Center"/> - <paramref name="from"/>).</returns>
		public Vector2 GetVectorToSide(Vector2 from, bool fromMinAngle = false)
		{
			var displacement = Center - from;
			var vecToSide = Vector2.Perpendicular(displacement * (!fromMinAngle ? 1 : -1));
			//if (!fromMinAngle)
			//	vecToSide = Vector2.Perpendicular(displacement);
			//else
			//	vecToSide = Vector2.Perpendicular(displacement * -1);
			return vecToSide.normalized;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="from">The position to get the extreme point in relation to.</param>
		/// <param name="fromMinAngle">Whether to get the point with the smallest absolute angle from <see cref="Vector2.right"/> or not.</param>
		/// <returns>A <see cref="Vector2"/> representing the most extreme point to the side on the perimeter of this object.</returns>
		public Vector2 GetExtremePoint(Vector2 from, bool fromMinAngle = false)
		{
			var vecToSide = GetVectorToSide(from, fromMinAngle);
			vecToSide = vecToSide.normalized * collider.bounds.extents.magnitude * 2;
			vecToSide = collider.ClosestPoint(vecToSide + Center);
			return vecToSide;
		}

		/// <summary>
		/// Get the range of angles from the given position that will collide with this object.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="positiveX"></param>
		/// <returns></returns>
		public Vector2 GetInputRange(Vector2 from, Vector2 positiveX)
		{
			var r = Vector2.positiveInfinity;
			r.x = Vector2.SignedAngle(positiveX, GetExtremePoint(from, true) - from);
			r.y = Vector2.SignedAngle(positiveX, GetExtremePoint(from) - from);
			if (r.x < 0)
				r.x += 360;
			if (r.y < 0)
				r.y += 360;
			// Check that x <= y
			if (r.x > r.y)
			{
				var t = r.x;
				r.x = r.y;
				r.y = t;
			}
			return r;
		}
		#endregion
	}
}