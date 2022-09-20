using System.Collections.Generic;
using UnityEngine;

namespace JMor.RuntimeGizmos
{
	// TODO: Clean up names
	// TODO: Expand
	/// <summary>
	/// Stores draw calls and renders them when able to. Needs to be in at least 1 scene to work; won't be destroyed on load.
	/// </summary>
	public class RTGizmosBuffer : MonoBehaviour
	{
		#region Singleton
		private static RTGizmosBuffer instance;
		public static RTGizmosBuffer Instance
		{
			get
			{
				if (instance == null)
					instance = FindObjectOfType<RTGizmosBuffer>();
				return instance;
			}
		}
		void SingletonSetup()
		{
			DontDestroyOnLoad(this);
			if (instance == null)
				instance = this;
			else if (instance != this)
			{
				Destroy(gameObject);
				return;
			}
		}
		#endregion

		private void Awake() => SingletonSetup();

		struct DrawFilledCircle2DBuffer
		{
			public Vector2 origin;
			public float radius;
			public int totalPoints;
			public Color? color;
			public Material mat;

			public DrawFilledCircle2DBuffer(
				Vector2 origin,
				float radius,
				int totalPoints = 32,
				Color? color = null,
				Material mat = null)
			{
				this.origin = origin;
				this.radius = radius;
				this.totalPoints = totalPoints;
				this.color = color;
				this.mat = mat;
			}
		}
		private static List<DrawFilledCircle2DBuffer> filledCircle2DBuffer = new List<DrawFilledCircle2DBuffer>();
		private static List<DrawFilledCircle2DBuffer> filledCircle2DBuffer_Static = new List<DrawFilledCircle2DBuffer>();
		public static void DrawFilledCircle2D(
			Vector2 origin,
			float radius,
			int totalPoints = 32,
			Color? color = null,
			Material mat = null)
		{
			filledCircle2DBuffer.Add(new(
				origin,
				radius,
				totalPoints,
				color,
				mat));
		}
		public static void DrawFilledCircle2D_Static(
			Vector2 origin,
			float radius,
			int totalPoints = 32,
			Color? color = null,
			Material mat = null)
		{
			filledCircle2DBuffer_Static.Add(new(
				origin,
				radius,
				totalPoints,
				color,
				mat));
		}
		public static void Clear_DrawFilledCircle2D_Static() => filledCircle2DBuffer_Static.Clear();

		public static int maxStaticBufferSizes = 1000;

		private void OnRenderObject()
		{
			filledCircle2DBuffer.ForEach((b) => RTGizmos.DrawFilledCircle2D(
				b.origin,
				b.radius,
				b.totalPoints,
				b.color,
				b.mat));
			filledCircle2DBuffer.Clear();
			filledCircle2DBuffer_Static.ForEach((b) => RTGizmos.DrawFilledCircle2D(
				b.origin,
				b.radius,
				b.totalPoints,
				b.color,
				b.mat));
			if (filledCircle2DBuffer_Static.Count > maxStaticBufferSizes)
				filledCircle2DBuffer_Static.Clear();
		}
	}
}
