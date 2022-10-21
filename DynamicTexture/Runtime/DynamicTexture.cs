using UnityEngine;
using UnityEngine.UI;

namespace JMor.Utility.DynamicTexture
{
	public class DynamicTexture : MonoBehaviour
	{
		public DynamicTextureMode textureMode;

		public Texture2D texture;

		public Vector2Int dimensions = new(200, 200);
		[Tooltip("If assigned to a component with a defined size (i.e. a UI component with a RectTransform), ignore the defined dimensions?")]
		public bool ignoreDimensions = true;

		public Component output;

		public bool preview = true;

		void Reset()
		{
			//image = GetComponent<Image>();
			dimensions = new(200, 200);
			texture = new Texture2D(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);
			//InitFunction(texture);
		}

		void Start()
		{
			textureMode.Initializer(texture);
		}

		void Update()
		{
			if (output != null)
				textureMode?.Updater(texture);
		}

		//void OnDrawGizmosSelected()
		//{
		//	if (preview)
		//	{
		//		try
		//		{
		//			if (output != null)
		//				textureMode?.Updater(texture);//UpdateFunction(texture);
		//		}
		//		catch (NullReferenceException)
		//		{
		//			textureMode?.Initializer(texture);
		//		}
		//	}
		//}

		[ContextMenu("OutputType")]
		public void GetOutputType()
		{
			switch (output)
			{
				case Image:
					var s = GetComponent<RectTransform>().rect.size;
					if (ignoreDimensions)
						dimensions = new((int)s.x, (int)s.y);
					((Image)output).sprite = Sprite.Create(texture, Rect.MinMaxRect(0, 0, dimensions.x, dimensions.y), dimensions / 2);
					break;
				case SpriteRenderer:
					((SpriteRenderer)output).sprite = Sprite.Create(texture, Rect.MinMaxRect(0, 0, dimensions.x, dimensions.y), dimensions / 2);
					break;
				default:
					break;
			}
			textureMode.Initializer(texture);
		}

		[ContextMenu("Init Texture")]
		public void InitTexture()
		{
			texture.Reinitialize(dimensions.x, dimensions.y, TextureFormat.RGBA32, false);
			textureMode.Initializer(texture);
		}
	}
}