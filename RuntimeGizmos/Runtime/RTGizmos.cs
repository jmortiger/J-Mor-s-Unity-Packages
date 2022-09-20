using System;
using UnityEngine;
using JMor.Utility;

namespace JMor.RuntimeGizmos
{
	public enum DrawMode
	{
		LINES			= GL.LINES,
		LINE_STRIP		= GL.LINE_STRIP,
		QUADS			= GL.QUADS,
		TRIANGLES		= GL.TRIANGLES,
		TRIANGLE_STRIP	= GL.TRIANGLE_STRIP,
	}
	// TODO: Expand
	public static class RTGizmos
	{
		#region State
		private static Material defaultMaterial;
		public static Material DefaultMaterial
		{
			get
			{
				if (defaultMaterial == null)
				{
					defaultMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
					{ hideFlags = HideFlags.HideAndDontSave };
					// Turn on alpha blending
					defaultMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					defaultMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
				}
				return defaultMaterial;
			}
		}
		private static Material material;
		public static Material Material { get => material != null ? material : DefaultMaterial; set => material = value; }
		public static Color Color { get; set; } = Color.white;
		#endregion

		#region Lines
		public static void DrawLine3D(
			Vector3 from,
			Vector3 to,
			Color? color = null,
			Material mat = null)
		{
			BeginDraw(GL.LINES, mat, color);
			GL.Vertex(from);
			GL.Vertex(to);
			EndDraw(mat);
		}

		public static void DrawLineSegments3D(
			Tuple<Vector3, Vector3>[] lineSegments,
			bool drawSegmentsConnected = true,
			Color? color = null,
			Material mat = null)
		{
			BeginDraw(GL.LINES, mat, color);
			for (int i = 0; i < lineSegments.Length; i++)
			{
				GL.Vertex(lineSegments[i].Item1);
				GL.Vertex(lineSegments[i].Item2);
				if (drawSegmentsConnected && i + 1 < lineSegments.Length)
				{
					GL.Vertex(lineSegments[i].Item2);
					GL.Vertex(lineSegments[i + 1].Item1);
				}
			}
			EndDraw(mat);
		}

		// TODO: Use LineStrip?
		public static void DrawConnectedPoints3D(
			Vector3[] points,
			bool closeShape = false,
			Color? color = null,
			Material mat = null)
		{
			BeginDraw(GL.LINES, mat, color);
			for (int i = 1; i < points.Length; i++)
			{
				GL.Vertex(points[i - 1]);
				GL.Vertex(points[i]);
			}
			if (closeShape)
			{
				GL.Vertex(points[^1]);
				GL.Vertex(points[0]);
			}
			EndDraw(mat);
		}

		public static void DrawConnectedPoints2D(
			Vector2[] points,
			bool closeShape = false,
			Color? color = null,
			Material mat = null)
		{
			BeginDraw(GL.LINES, mat, color);
			for (int i = 1; i < points.Length; i++)
			{
				GL.Vertex(points[i - 1]);
				GL.Vertex(points[i]);
			}
			if (closeShape)
			{
				GL.Vertex(points[^1]);
				GL.Vertex(points[0]);
			}
			EndDraw(mat);
		}
		#endregion

		#region Box
		public static void DrawWireframeBox2D(
			Bounds bounds,
			Quaternion? orientation = null,
			Color? color = null,
			Material mat = null)
		{
			var o = orientation ?? Quaternion.identity;
			var points = new Vector2[]
			{
				o * bounds.min,
				o * new Vector2(bounds.min.x, bounds.max.y),
				o * bounds.max,
				o * new Vector2(bounds.max.x, bounds.min.y),
			};
			DrawConnectedPoints2D(points, true, color, mat);
		}

		public static void DrawOutlinedBox2D(
			Bounds bounds,
			Quaternion? orientation = null,
			float thickness = 1f,
			Color? color = null,
			Material mat = null)
		{
			var o = orientation ?? Quaternion.identity;
			var points = new Vector2[]
			{
				o * bounds.min,
				o * new Vector2(bounds.min.x, bounds.max.y),
				o * bounds.max,
				o * new Vector2(bounds.max.x, bounds.min.y),
			};
			var halfThick = thickness / 2f;
			var pointsInner = (Vector2[])points.Clone();
			var pointsOuter = (Vector2[])points.Clone();
			for (int i = 0; i < pointsInner.Length; i++)
			{
				pointsInner[i].x -= halfThick;
				pointsInner[i].y -= halfThick;
				pointsOuter[i].x += halfThick;
				pointsOuter[i].y += halfThick;
			}
			BeginDraw(DrawMode.TRIANGLE_STRIP, mat, color);
			DrawOrderedClosedOutline2D(pointsInner, pointsOuter);
			EndDraw(mat);
		}

		public static void DrawFilledBox2D(
			Bounds bounds,
			Quaternion? orientation = null,
			Color? color = null,
			Material mat = null)
		{
			var o = orientation ?? Quaternion.identity;
			var points = new Vector2[]
			{
				o * bounds.min,
				o * new Vector2(bounds.min.x, bounds.max.y),
				o * bounds.max,
				o * new Vector2(bounds.max.x, bounds.min.y),
			};
			BeginDraw(DrawMode.TRIANGLE_STRIP, mat, color);
			DrawOrderedClosedShape2D(points);
			EndDraw(mat);
		}
		#endregion

		#region Circle
		public static void DrawWireframeCircle2D(
			Vector2 origin,
			float radius,
			int totalPoints = 32,
			Color? color = null,
			Material mat = null)
		{
			var points = MyMath.ComputeCircle(radius: radius, origin: origin, totalPoints: totalPoints);
			DrawConnectedPoints2D(points, true, color, mat);
		}

		public static void DrawOutlinedCircle2D(
			Vector2 origin,
			float radius,
			int totalPoints = 32,
			float thickness = 1f,
			Color? color = null,
			Material mat = null)
		{
			var pointsOuter = MyMath.ComputeCircle(radius: radius + thickness / 2f, origin: origin, totalPoints: totalPoints);
			var pointsInner = MyMath.ComputeCircle(radius: radius - thickness / 2f, origin: origin, totalPoints: totalPoints);
			BeginDraw(DrawMode.TRIANGLE_STRIP, mat, color);
			DrawOrderedClosedOutline2D(pointsInner, pointsOuter);
			EndDraw(mat);
		}

		public static void DrawFilledCircle2D(
			Vector2 origin,
			float radius,
			int totalPoints = 32,
			Color? color = null,
			Material mat = null)
		{
			var points = MyMath.ComputeCircle(radius: radius, origin: origin, totalPoints: totalPoints);
			BeginDraw(DrawMode.TRIANGLE_STRIP, mat, color);
			DrawOrderedClosedShape2D(points);
			EndDraw(mat);
		}
		#endregion

		#region Helpers
		#region Begin and End Draw Helpers
		private static int priorCullValue = 0;
		private static void BeginDraw(int mode, Material mat = null, Color? color = null)
		{
			switch (mode)
			{
				case GL.LINES:
					BeginDraw(DrawMode.LINES, mat, color);
					break;
				case GL.LINE_STRIP:
					BeginDraw(DrawMode.LINE_STRIP, mat, color);
					break;
				case GL.QUADS:
					BeginDraw(DrawMode.QUADS, mat, color);
					break;
				case GL.TRIANGLES:
					BeginDraw(DrawMode.TRIANGLES, mat, color);
					break;
				case GL.TRIANGLE_STRIP:
					BeginDraw(DrawMode.TRIANGLE_STRIP, mat, color);
					break;
				default:
					throw new ArgumentOutOfRangeException("mode", mode, "mode must be one of the constant values in GL/DrawMode.");
			}
		}
		private static void BeginDraw(DrawMode mode, Material mat = null, Color? color = null)
		{
			// Temporarily Change culling for safety
			priorCullValue = (mat != null ? mat : Material).GetInt("_Cull");
			(mat != null ? mat : Material).SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Apply Material
			Material.SetPass(0);
			// Store current Model, View, and Projection Matricies
			GL.PushMatrix();
			// Begin Draw
			GL.Begin((int)mode);
			// Set Color
			GL.Color(color ?? Color);
		}
		private static void EndDraw(Material mat = null)
		{
			// End Draw
			GL.End();
			// Restore prior Model, View, and Projection Matricies
			GL.PopMatrix();
			// Reset culling
			priorCullValue = (mat != null ? mat : Material).GetInt("_Cull");
			(mat != null ? mat : Material).SetInt("_Cull", priorCullValue);
		}
		#endregion

		#region Ordered Closed
		#region Shape
		private static void DrawOrderedClosedShape2D(Vector2[] points)
		{
			// Assumes GL.Begin(GL.TRIANGLE_STRIP) has been called.
			GL.Vertex(points[0]);
			int halfLength = points.Length / 2;
			for (int i = 1; i <= halfLength; i++)
			{
				GL.Vertex(points[i]);
				GL.Vertex(points[^i]);
			}
			if (points.Length % 2 == 0)
				GL.Vertex(points[halfLength]);
			// Leaves GL.End() to caller
		}

		private static void DrawOrderedClosedShape3D(Vector3[] points)
		{
			// Assumes GL.Begin(GL.TRIANGLE_STRIP) has been called.
			if (points.Length == 0)
				return;
			int halfLength = points.Length / 2;
			GL.Vertex(points[0]);
			for (int i = 1; i <= halfLength; i++)
			{
				GL.Vertex(points[i]);
				GL.Vertex(points[^i]);
			}
			if (points.Length % 2 == 0)
				GL.Vertex(points[halfLength]);
			// Leaves GL.End() to caller
		}
		#endregion
		#region Outline
		private static void DrawOrderedClosedOutline2D(Vector2[] pointsInner, Vector2[] pointsOuter)
		{
			Debug.Assert(pointsOuter.Length == pointsInner.Length);
			// Assumes GL.Begin(GL.TRIANGLE_STRIP) has been called.
			for (int i = 0; i < pointsOuter.Length; i++)
			{
				GL.Vertex(pointsOuter[i]);
				GL.Vertex(pointsInner[i]);
			}
			GL.Vertex(pointsOuter[0]);
			GL.Vertex(pointsInner[0]);
			// Leaves GL.End() to caller
		}
		private static void DrawOrderedClosedOutline3D(Vector3[] pointsInner, Vector3[] pointsOuter)
		{
			Debug.Assert(pointsOuter.Length == pointsInner.Length);
			// Assumes GL.Begin(GL.TRIANGLE_STRIP) has been called.
			for (int i = 0; i < pointsOuter.Length; i++)
			{
				GL.Vertex(pointsOuter[i]);
				GL.Vertex(pointsInner[i]);
			}
			GL.Vertex(pointsOuter[0]);
			GL.Vertex(pointsInner[0]);
			// Leaves GL.End() to caller
		}
		#endregion
		#endregion
		#endregion
	}
}