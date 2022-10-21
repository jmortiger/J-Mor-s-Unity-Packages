using System;
using UnityEngine;

namespace JMor.Utility.DynamicTexture
{
	[CreateAssetMenu(fileName = "dtm_HorizGradMode0", menuName = "Scriptable Object/Dynamic Texture Mode/Horizontal Gradient Mode")]
	public class HorizontalGradientMode : DynamicTextureMode
	{
		public Gradient gradient;
		[Tooltip("The speed to scroll in pixels per second. Postive scrolls left to right, negative scrolls right to left, 0 doesn't scroll.")]
		public float scrollSpeed;
		//[Tooltip("How many ")]
		public uint resolution = 5;
		private float currOffset;
		public override void Updater(Texture2D texture)
		{
			currOffset = UpdateOffset_Wrapped(currOffset, (-scrollSpeed * Time.fixedDeltaTime), 0, texture.width);
			GenerateHorizontalGradient(texture, gradient, (int)currOffset, resolution);
			base.Updater(texture);
		}
	}
}
