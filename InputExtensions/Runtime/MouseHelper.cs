using JMor.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JMor.Utility.Input
{
	public static class MouseHelper
	{
#if ENABLE_INPUT_SYSTEM
		private static Vector2 screenPosition;
		public static Vector2 ScreenPosition
		{
			get
			{
				var t = Mouse.current.position.ReadValue();
				if (t.IsFinite())
					return screenPosition = t;
				else
					return screenPosition;
			}
		}
#else
	public static Vector2 ScreenPosition { get => Input.mousePosition; }
#endif
		public static Vector2 WorldPosition { get => Camera.main.ScreenToWorldPoint(ScreenPosition); }

		public static bool IsMouseUp()
		{
			// TODO: Use Input instead of Event
			return Event.current.isMouse &&
			(Event.current.type == EventType.MouseUp ||
			(Event.current.type != EventType.MouseDown &&
			Event.current.type != EventType.MouseDrag));
		}

		public static Vector2 GetWorldPosition(/*Vector2 screenPos*/)
		{
			var screenPosition = new Vector2(ScreenPosition.x, Camera.main.pixelHeight - ScreenPosition.y);
			return Camera.main.ScreenToWorldPoint(screenPosition);
		}
	} 
}