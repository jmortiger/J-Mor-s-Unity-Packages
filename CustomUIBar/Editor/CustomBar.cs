using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace JMor.EditorScripts
{
	public class CustomBar : ScriptableObject
	{
		#region Create UI Bar Element
		[MenuItem("GameObject/UI/J Mor's Custom/Bar")]
		static void CreateBar() => CreateBarHelper();
		[MenuItem("GameObject/UI/J Mor's Custom/Bar (Masked)")]
		static void CreateBarMasked() => CreateBarHelper(true);
		static void CreateBarHelper(bool createMask = false)
		{
			var canvas = FindObjectOfType<Canvas>();
			if (canvas == null)
			{
				canvas = new GameObject(
					"Canvas",
					typeof(Canvas),
					typeof(CanvasScaler),
					typeof(GraphicRaycaster)) { layer = LayerMask.NameToLayer("UI") }.GetComponent<Canvas>();
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			}
			var bar = new GameObject($"Bar{(createMask ? " (Masked)" : "")}", typeof(Slider)) { layer = LayerMask.NameToLayer("UI") };
			var rt = bar.GetComponent<RectTransform>();
			rt.SetParent(canvas.transform);
			rt.sizeDelta = new(200f, 200f / 5f);
			rt.localPosition = Vector3.zero;
			var slider = bar.GetComponent<Slider>();
			slider.interactable = false;
			slider.transition = Selectable.Transition.None;
			slider.navigation = new Navigation() { mode = Navigation.Mode.None };
			slider.value = .5f; // For demonstration
			var fill = new GameObject("Fill", typeof(Image)) { layer = LayerMask.NameToLayer("UI") };
			fill.GetComponent<Image>().color = Color.red;
			var fillRT = fill.GetComponent<RectTransform>();
			fillRT.SetParent(rt);
			fillRT.anchorMin = Vector2.zero;
			fillRT.anchorMax = Vector2.one;
			fillRT.offsetMin = Vector2.zero;
			fillRT.offsetMax = Vector2.zero;
			slider.fillRect = fillRT;
			var borderImage = new GameObject("Border", typeof(Image)) { layer = LayerMask.NameToLayer("UI") }.GetComponent<Image>();
			var borderRT = borderImage.GetComponent<RectTransform>();
			borderRT.SetParent(rt);
			borderRT.anchorMin = Vector2.zero;
			borderRT.anchorMax = Vector2.one;
			borderRT.offsetMin = Vector2.zero;
			borderRT.offsetMax = Vector2.zero;
			//border.GetComponent<Image>().sprite = AssetDatabase.
			if (createMask)
			{
				bar.AddComponent<Mask>().showMaskGraphic = false;
				var barImage = bar.AddComponent<Image>();
				barImage.maskable = true;
				borderImage.maskable = false;
				Debug.LogWarning("Be sure to set the 'Sprite' field in the 'Image' component on 'Bar' to define the mask shape.");
			}
			Debug.LogWarning("Be sure to set the 'Sprite' field in the 'Image' component on 'Border' to define the border.");
			Selection.activeTransform = borderRT;
		}
		#endregion
	}
}