using System;
using UnityEngine;
using UnityEngine.Events;

namespace JMor.Utility.DynamicTexture
{
	[CreateAssetMenu(fileName = "dtm_StaticMode0", menuName = "Scriptable Object/Dynamic Texture Mode/Static Mode")]
	public class StaticMode : DynamicTextureMode
	{
		[Tooltip("How many screen pixels make up 1 pixel of static.")]
		[Range(1, 100)]
		public int staticPixelSize = 1;

		public override void Initializer(Texture2D texture) => base.Initializer(texture);

		public override void Updater(Texture2D texture)
		{
			GenerateStatic(texture, staticPixelSize);
			base.Updater(texture);
		}
	}
}