using JMor.Utility;
using UnityEngine.InputSystem;

namespace JMor.Utility.Input
{
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// This class requires a string that DIRECTLY corrosponds 
	/// to the name of an <see cref="InputAction.name"/>.
	/// </remarks>
	public class ButtonInfo
	{
		private readonly bool[] onPressedBuffer;
		private bool inputPressedLastFrame = false;
		private bool inputPressedThisFrame = false;
		public string InputToMonitorName { get; private set; }
		public bool InputPressedOnThisFrame { get => inputPressedThisFrame && (!inputPressedLastFrame); }
		public bool InputPressedOnBufferedFrames
		{
			get
			{
				foreach (var item in onPressedBuffer)
					if (item)
						return true;
				return false;
			}
		}

		public ButtonInfo(string inputToMonitor, uint bufferLength = 10)
		{
			InputToMonitorName = inputToMonitor;
			onPressedBuffer = new bool[bufferLength];
		}

		public void Update(PlayerInput input) => inputPressedThisFrame = inputPressedThisFrame || input.IsPressed(InputToMonitorName);

		public void AdvanceToNextFrame()
		{
			// Buffer update
			if (onPressedBuffer.Length > 0)
				onPressedBuffer.SlideElementsDown(inputPressedThisFrame);

			inputPressedLastFrame = inputPressedThisFrame;
			inputPressedThisFrame = false;
		}
#if UNITY_EDITOR
		#region Debug Methods
		private uint updates = 0;
		public void Update_DEBUG(PlayerInput input)
		{
			updates++;
			UnityEngine.Debug.Log($"[{InputToMonitorName}] #{updates}: {inputPressedThisFrame} || {input.IsPressed(InputToMonitorName)} = {inputPressedThisFrame || input.IsPressed(InputToMonitorName)}");
			Update(input);
		}

		public void AdvanceToNextFrame_DEBUG()
		{
			updates = 0;
			UnityEngine.Debug.Log($"[{InputToMonitorName}] FINAL: {inputPressedThisFrame} && {!inputPressedLastFrame} = {inputPressedThisFrame && (!inputPressedLastFrame)} = {InputPressedOnThisFrame}");
			AdvanceToNextFrame();
		}
		#endregion
#endif
	}
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T">
	/// An enum where the names of all the values DIRECTLY corrospond 
	/// to the name of an <see cref="InputAction.name"/>.
	/// </typeparam>
	/// <remarks>
	/// This class requires an enum where the names of all the values DIRECTLY corrospond 
	/// to the name of an <see cref="InputAction.name"/>. As such, InputActions must have names representable 
	/// in code (i.e. no spaces, certain special characters, etc).
	/// </remarks>
	public class ButtonInfo<T> : ButtonInfo where T : System.Enum
	{
		public T InputToMonitor { get; private set; }

		public ButtonInfo(T inputToMonitor, uint bufferLength = 10)
			: base(inputToMonitor.ToString(), bufferLength) { }
	}
}
