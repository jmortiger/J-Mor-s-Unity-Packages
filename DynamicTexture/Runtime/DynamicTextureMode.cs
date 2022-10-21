using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace JMor.Utility.DynamicTexture
{
	[CreateAssetMenu(fileName = "dtm_NewMode", menuName = "Scriptable Object/Dynamic Texture Mode/Default")]
	public class DynamicTextureMode : ScriptableObject
	{
		public virtual void Initializer(Texture2D texture)
		{
			//if (buffer.Length != texture.width * texture.height)
			//	buffer = new Color[texture.width * texture.height];
			Updater(texture);
		}

		public virtual void Updater(Texture2D texture) => texture.Apply();

		private Color[] buffer;

		protected void GenerateStatic(Texture2D texture, int staticPixelSize)
		{
			var iComparisonVal = texture.height / staticPixelSize;
			var jComparisonVal = texture.width / staticPixelSize;
			for (int i = 0; i <= iComparisonVal; i++)
			{
				for (int j = 0; j <= jComparisonVal; j++)
				{
					var currColor = (Random.Range(0, 2) == 0) ? Color.white : Color.black;
					for (int i2 = 0; i2 < staticPixelSize && (i * staticPixelSize + i2) < texture.height; i2++)
					{
						for (int j2 = 0; j2 < staticPixelSize && (j * staticPixelSize + j2) < texture.width; j2++)
						{
							//var ind = ((j * staticPixelSize + j2) * texture.width) + (i * staticPixelSize + i2);
							//Debug.Log(ind);
							//buffer[ind] = currColor;
							texture.SetPixel(j * staticPixelSize + j2, i * staticPixelSize + i2, currColor);
						}
					}
				}
			}
			//texture.SetPixels(buffer);
		}

		protected void GenerateHorizontalGradient(Texture2D texture, Gradient gradient, int offset, uint horizontalPixelSize = 5)
		{
			// TODO: Optimize for speed of 0.
			int adjustedXPos;
			Color currColor;

			void UpdateVariables(int i)
			{
				adjustedXPos = i + offset;
				if (adjustedXPos < 0)
					adjustedXPos += texture.width;
				else if (adjustedXPos >= texture.width)
					adjustedXPos -= texture.width;
				currColor = gradient.Evaluate(adjustedXPos / (float)texture.width);
			}
			UpdateVariables(0);
			for (int i = 0; i < texture.width; i++)
			{
				if ((i + 1) % horizontalPixelSize == 0)
				{
					UpdateVariables(i);
				}
				for (int j = 0; j < texture.height; j++)
					texture.SetPixel(i, j, currColor);
			}
		}

		protected int UpdateOffset_Wrapped(int currOffset, float multiplier, int minInclusive, int maxExclusive)
		{
			currOffset += (int)(multiplier);
			if (currOffset < minInclusive)
				currOffset += maxExclusive - minInclusive;
			else if (currOffset >= maxExclusive)
				currOffset -= maxExclusive - minInclusive;
			return currOffset;
		}

		protected float UpdateOffset_Wrapped(float currOffset, float multiplier, int minInclusive, int maxExclusive)
		{
			currOffset += multiplier;
			if (currOffset < minInclusive)
				currOffset += maxExclusive - minInclusive;
			else if (currOffset >= maxExclusive)
				currOffset -= maxExclusive - minInclusive;
			return currOffset;
		}
	}
}