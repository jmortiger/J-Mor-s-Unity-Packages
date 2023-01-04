using System;
using System.Linq;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace JMor.Utility
{
	public static class ExtensionMethods
	{
		#region Vector Helpers
		#region For Both
		public static bool IsFinite(this Vector2 v) => float.IsFinite(v.x) && float.IsFinite(v.y);
		public static bool IsFinite(this Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);

		#region Approximate Distance
		// TODO: Implement for V3 + V2 and V2 + V3
		public static bool Approximately(this Vector2 v1, Vector2 v2, float maxDistance = Vector2.kEpsilon)
		{
			if (!v1.IsFinite())
				throw new ArgumentException("One of the vectors is not finite.", "v1");
			else if (!v2.IsFinite())
				throw new ArgumentException("One of the vectors is not finite.", "v2");
			return ApproximatelyTry(v1, v2, maxDistance);
		}
		public static bool ApproximatelyTry(this Vector2 v1, Vector2 v2, float maxDistance = Vector2.kEpsilon)
		{
			float Δx = v1.x - v2.x, Δy = v1.y - v2.y;
			float magnitudeSquared = Δx * Δx + Δy * Δy;
			return magnitudeSquared < maxDistance * maxDistance;
		}

		public static bool Approximately(this Vector3 v1, Vector3 v2, float maxDistance = Vector3.kEpsilon)
		{
			if (!v1.IsFinite())
				throw new ArgumentException("One of the vectors is not finite.", "v1");
			else if (!v2.IsFinite())
				throw new ArgumentException("One of the vectors is not finite.", "v2");
			return ApproximatelyTry(v1, v2, maxDistance);
		}
		public static bool ApproximatelyTry(this Vector3 v1, Vector3 v2, float maxDistance = Vector3.kEpsilon)
		{
			float Δx = v1.x - v2.x, Δy = v1.y - v2.y, Δz = v1.z - v2.z;
			float magnitudeSquared = Δx * Δx + Δy * Δy + Δz * Δz;
			return magnitudeSquared < maxDistance * maxDistance;
		}

		#endregion
		#region Round Vector
		public static Vector2 Round(this Vector2 input, bool roundX = true, bool roundY = true)
		{
			if (roundX) input.x = Mathf.Round(input.x);
			if (roundY) input.y = Mathf.Round(input.y);
			return input;
		}

		public static Vector3 Round(this Vector3 input, bool roundX = true, bool roundY = true, bool roundZ = true)
		{
			if (roundX) input.x = Mathf.Round(input.x);
			if (roundY) input.y = Mathf.Round(input.y);
			if (roundZ) input.z = Mathf.Round(input.z);
			return input;
		}
		#endregion
		#endregion

		#region Fill in the holes for Vector2
		public static Vector2 RotateTowards(this Vector2 from, Vector2 to, float maxRadiansDelta, float maxMagnitudeDelta = 0f)
		{
			return Vector3.RotateTowards(from, to, maxRadiansDelta, maxMagnitudeDelta);
		}
		#endregion
		#endregion

		#region PlayerInput Helpers
#if ENABLE_INPUT_SYSTEM
		#region InputAction.name via string
		public static bool WasPressedThisFrame(this PlayerInput input, string actionNameOrId)
		{
			return ((ButtonControl)input.actions.FindAction(actionNameOrId)?.activeControl)?.wasPressedThisFrame ?? false;
		}

		public static bool IsPressed(this PlayerInput input, string actionNameOrId)
		{
			//if (input.actions.FindAction(actionNameOrId)?.IsPressed() == null)
			//	Debug.Log($"Bad val of input");
			return input.actions.FindAction(actionNameOrId)?.IsPressed()/*activeControl?.IsPressed()*/ ?? false;
		}

		public static InputAction FindAction(this PlayerInput input, string actionNameOrId) => input.actions.FindAction(actionNameOrId);

		public static Vector2 GetActionValueAsJoystick(this PlayerInput input, string actionNameOrId, Vector2 relativeTo)
		{
			var control = input.actions.FindAction(actionNameOrId);
			var val = control.ReadValue<Vector2>();
			return ((control?.activeControl?.device is Mouse) ?
				((Vector2)Camera.main.ScreenToWorldPoint(val) - relativeTo) :
				val).normalized;
		}
		#endregion
		#region InputAction.name via Generic Enum
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T">
		/// An enum where the names of all the values DIRECTLY correspond 
		/// to the name of an <see cref="InputAction.name"/>.
		/// </typeparam>
		/// <param name="input"></param>
		/// <param name="actionName"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method requires an enum where the names of all the values DIRECTLY correspond 
		/// to the name of an <see cref="InputAction.name"/>. As such, InputActions must have names representable 
		/// in code (i.e. no spaces, certain special characters, etc).
		/// </remarks>
		public static bool WasPressedThisFrame<T>(this PlayerInput input, T actionName) where T : System.Enum => input.WasPressedThisFrame(actionName.ToString());
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T">
		/// An enum where the names of all the values DIRECTLY correspond 
		/// to the name of an <see cref="InputAction.name"/>.
		/// </typeparam>
		/// <param name="input"></param>
		/// <param name="actionName"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method requires an enum where the names of all the values DIRECTLY correspond 
		/// to the name of an <see cref="InputAction.name"/>. As such, InputActions must have names representable 
		/// in code (i.e. no spaces, certain special characters, etc).
		/// </remarks>
		public static bool IsPressed<T>(this PlayerInput input, T actionName) where T : System.Enum => input.IsPressed(actionName.ToString());
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T">
		/// An enum where the names of all the values DIRECTLY correspond 
		/// to the name of an <see cref="InputAction.name"/>.
		/// </typeparam>
		/// <param name="input"></param>
		/// <param name="actionName"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method requires an enum where the names of all the values DIRECTLY correspond 
		/// to the name of an <see cref="InputAction.name"/>. As such, InputActions must have names representable 
		/// in code (i.e. no spaces, certain special characters, etc).
		/// </remarks>
		public static InputAction FindAction<T>(this PlayerInput input, T actionName) where T : System.Enum => input.FindAction(actionName.ToString());
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T">
		/// An enum where the names of all the values DIRECTLY correspond 
		/// to the name of an <see cref="InputAction.name"/>.
		/// </typeparam>
		/// <param name="input"></param>
		/// <param name="actionName"></param>
		/// <returns></returns>
		/// <remarks>
		/// This method requires an enum where the names of all the values DIRECTLY correspond 
		/// to the name of an <see cref="InputAction.name"/>. As such, InputActions must have names representable 
		/// in code (i.e. no spaces, certain special characters, etc).
		/// </remarks>
		public static Vector2 GetActionValueAsJoystick<T>(this PlayerInput input, T actionName, Vector2 relativeTo) where T : System.Enum => input.GetActionValueAsJoystick(actionName.ToString(), relativeTo);
		#endregion
		public static Vector2 GetRightStickOrMouseValueAsJoystickEditor(this PlayerInput input, Vector2 relativeTo)
		{
			return ((Gamepad.current != null) ?
				Gamepad.current.rightStick.ReadValue() :
				(Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - relativeTo).normalized;
		}
#endif
		#endregion

		#region Orthographic Bounds
		/// <summary>
		/// Uses <see cref="Screen"/> and <see cref="Camera"/> to determine the bounds of the camera.
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public static Bounds OrthographicBoundsByScreen(this Camera camera)
		{
			float screenAspect = (float)Screen.width / (float)Screen.height;
			float cameraHeight = camera.orthographicSize * 2;
			Bounds bounds = new(
				camera.transform.position,
				new Vector3(cameraHeight * screenAspect, cameraHeight, 0));
			return bounds;
		}
		/// <summary>
		/// Uses <see cref="Camera"/> to determine the bounds of the camera.
		/// </summary>
		/// <param name="camera"></param>
		/// <returns></returns>
		public static Bounds OrthographicBoundsByCamera(this Camera camera)
		{
			float cameraHeight = camera.orthographicSize * 2;
			Bounds bounds = new(
				camera.transform.position,
				new Vector3(cameraHeight * camera.aspect, cameraHeight, 0));
			return bounds;
		}
		#endregion

		#region Array Manipulation
		#region SlideDown
		/// <summary>
		/// i.e. the element at source[0] is moved to source[0+indexesToSlideDown], etc.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source"></param>
		/// <param name="defaultValue"></param>
		/// <param name="output"></param>
		/// <param name="indexesToSlideDown"></param>
        /// <exception cref="ArgumentException"></exception>
		public static void SlideElementsDown<T>(this T[] source, T defaultValue, out T[] output, uint indexesToSlideDown = 1)
		{
			if (indexesToSlideDown > source.Length)
				throw new ArgumentException("indexesToSlideDown is not <= source.Length");
			output = new T[source.Length];
			for (int i = source.Length - 1; i >= 0; i--)
				output[i] = (i - indexesToSlideDown < 0) ? defaultValue : source[i - indexesToSlideDown];
		}
		/// <inheritdoc cref="SlideElementsDown{T}(T[], T, out T[], uint)"/>
		public static void SlideElementsDown<T>(this T[] source, T defaultValue, uint indexesToSlideDown = 1)
		{
			if (indexesToSlideDown > source.Length)
				throw new ArgumentException("indexesToSlideDown is not <= source.Length");
			for (int i = source.Length - 1; i >= 0; i--)
				source[i] = (i - indexesToSlideDown < 0) ? defaultValue : source[i - indexesToSlideDown];
		}
		#endregion
		#region SlideUp
		public static void SlideElementsUp<T>(this T[] source, T defaultValue, out T[] output, uint indexesToSlideUp = 1)
		{
			if (indexesToSlideUp > source.Length)
				throw new ArgumentException("indexesToSlideUp is not <= source.Length");
			output = new T[source.Length];
			for (int i = 0; i < source.Length; i++)
				output[i] = (i + indexesToSlideUp >= source.Length) ? defaultValue : source[i + indexesToSlideUp];
		}
		public static void SlideElementsUp<T>(this T[] source, T defaultValue, uint indexesToSlideUp = 1)
		{
			if (indexesToSlideUp > source.Length)
				throw new ArgumentException("indexesToSlideUp is not <= source.Length");
			for (int i = 0; i < source.Length; i++)
				source[i] = (i + indexesToSlideUp >= source.Length) ? defaultValue : source[i + indexesToSlideUp];
		}
		#endregion
		#region RemoveAt
		public static void RemoveAt<T>(this T[] source, int index, out T[] output) => source.RemoveAt((uint)index, out output);
		public static void RemoveAt<T>(this T[] source, uint index, out T[] output)
		{
			if (index >= source.Length)
				throw new ArgumentException("index is not < source.Length");
			output = new T[source.Length - 1];
			for (int i = 0; i < source.Length - 1; i++)
				output[i] = (i < index) ? source[i] : source[i + 1];
		}
		//public static void RemoveAt<T>(this T[] source, int index) => source.RemoveAt((uint)index);
		//public static void RemoveAt<T>(this T[] source, uint index)
		//{
		//	if (index >= source.Length)
		//		throw new ArgumentException("index is not < source.Length");
		//	var t = new T[source.Length - 1];
		//	for (int i = 0; i < source.Length - 1; i++)
		//		t[i] = (i < index) ? source[i] : source[i + 1];
		//	source = t;
		//}
		#endregion
		#region Filter
		/// <summary>
        /// Filters elements out of the array based on the given function.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="source">The starting array to filter from.</param>
        /// <param name="predicate">The filtering function. If the value for an element evaluates to true, it will be included in the resultant array.</param>
        /// <param name="output">The new array of filtered elements.</param>
        /// <returns>The length of the new <paramref name="output"/> array.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="predicate"/> are null.</exception>
		public static int Filter<T>(this T[] source, Predicate<T> predicate, out T[] output) {
			if (source == null || predicate == null) throw new ArgumentNullException();
			output = source.Where(t => predicate(t)).ToArray();
			return output.Length;
		}
		/// <inheritdoc cref="Filter{T}(T[], Predicate{T}, out T[])"/>
		public static int Filter<T>(this T[] source, Func<T, bool> predicate, out T[] output) {
			if (source == null || predicate == null) throw new ArgumentNullException();
			output = source.Where(predicate).ToArray();
			return output.Length;
		}
		/// <summary>
        /// <inheritdoc cref="Filter{T}(T[], Predicate{T}, out T[])"/> Mutates the given array.
        /// </summary>
        /// <param name="source">The array to filter. This will be mutated by this function.</param>
        /// <returns>The new length of <paramref name="source"/>.</returns>
        /// <inheritdoc cref="Filter{T}(T[], Predicate{T}, out T[])"/>
		public static int Filter<T>(this T[] source, Predicate<T> predicate) {
			if (source == null || predicate == null) throw new ArgumentNullException();
			source = source.Where(t => predicate(t)).ToArray();
			return source.Length;
		}
		/// <inheritdoc cref="Filter{T}(T[], Predicate{T})"/>
		public static int Filter<T>(this T[] source, Func<T, bool> predicate) {
			if (source == null || predicate == null) throw new ArgumentNullException();
			source = source.Where(predicate).ToArray();
			return source.Length;
		}
		#endregion
		#region Sort
		// TODO: Implement Alias for Array.Sort
		// /// <summary>
        // /// Sorts the elements in the array.
        // /// </summary>
        // /// <typeparam name="T">The type of the array.</typeparam>
        // /// <param name="source">The starting array to filter from.</param>
        // /// <param name="predicate">The filtering function. If the value for an element evaluates to true, it will be included in the resultant array.</param>
        // /// <param name="output">The new array of filtered elements.</param>
        // /// <returns>The length of the new <paramref name="output"/> array.</returns>
        // /// <exception cref="ArgumentNullException">If <paramref name="source"/> or <paramref name="predicate"/> are null.</exception>
		// public static T[] Sort<T>(this T[] source, Predicate<T> predicate, out T[] output) {
		// 	if (source == null || predicate == null) throw new ArgumentNullException();
		// 	output = source.Where(t => predicate(t)).ToArray();
		// 	return output;
		// }
		// /// <inheritdoc cref="Sort{T}(T[], Predicate{T}, out T[])"/>
		// public static T[] Sort<T>(this T[] source, Func<T, bool> predicate, out T[] output) {
		// 	if (source == null || predicate == null) throw new ArgumentNullException();
		// 	output = source.Where(predicate).ToArray();
		// 	return output;
		// }
		// /// <summary>
        // /// <inheritdoc cref="Sort{T}(T[], Predicate{T}, out T[])"/> Mutates the given array.
        // /// </summary>
        // /// <param name="source">The array to filter. This will be mutated by this function.</param>
        // /// <returns>The new length of <paramref name="source"/>.</returns>
        // /// <inheritdoc cref="Sort{T}(T[], Predicate{T}, out T[])"/>
		// public static T[] Sort<T>(this T[] source, Predicate<T> predicate) {
		// 	if (source == null || predicate == null) throw new ArgumentNullException();
		// 	source = source.Where(t => predicate(t)).ToArray();
		// 	return source;
		// }
		// /// <inheritdoc cref="Sort{T}(T[], Predicate{T})"/>
		// public static T[] Sort<T>(this T[] source, Func<T, bool> predicate) {
		// 	if (source == null || predicate == null) throw new ArgumentNullException();
		// 	source = source.Where(predicate).ToArray();
		// 	return source;
		// }
		#endregion
		// TODO: Add js-like map functions
		#endregion

		#region Validation
		// public static bool Validate(ref this float value, float substitute = 0f)
		// {
		// 	if (float.IsFinite(value))
		// 		return true;
		// 	else {
		// 		value = substitute;
		// 		return false;
		// 	}
		// }
		public static float Validate(this float value, float substitute = 0f) => float.IsFinite(value) ? value : substitute;
		public static bool IsValid(this float value, float minInclusive = float.MinValue, float maxInclusive = float.MaxValue) => float.IsFinite(value) && value >= minInclusive && value <= maxInclusive ? true : false;
		#endregion

		#region Floating Point Comparison
		// TODO: Unit Test the following 2. This concerns me https://roundwide.com/equality-comparison-of-floating-point-numbers-in-csharp/. Adapted from https://stackoverflow.com/a/3875619/9819929
		public static bool Approximately(this double a, double b, double epsilon = 2.2250738585072014E-308d/*1E-10d*//*double.Epsilon*/)
		{
			if (a.Equals(b)) return true; // Strict Equality
			byte safety = 100;
			while (epsilon >= 1 && safety > 0) {
				epsilon = 1d / epsilon;
				--safety;
			}
		    var absA = Math.Abs(a);
		    var absB = Math.Abs(b);
			var diff = Math.Abs(a - b);
		
			return  diff <= epsilon || // Absolute Error
					diff <= Math.Min/*Max*/(absA, absB) * epsilon; // Relative Error
			#region Old
			// const double MinNormal = 2.2250738585072014E-308d;
			// var absA = Math.Abs(a);
			// var absB = Math.Abs(b);
			// var diff = Math.Abs(a - b);
		
			// if (a.Equals(b))
			//     return true;
			// else if (a == 0 || b == 0 || absA + absB < MinNormal) 
			//     // a or b is zero or both are extremely close to it; relative error is less meaningful here
			//     return diff < (epsilon * MinNormal);
			// else // use relative error
			//     return diff / (absA + absB) < epsilon;
			#endregion
		}
		public static bool Approximately(this float a, float b, float epsilon = 1.17549435E-38f/*1E-10f*//*float.Epsilon*/)
		{
			if (a.Equals(b)) return true; // Strict Equality
			byte safety = 100;
			while (epsilon >= 1 && safety > 0) {
				epsilon = 1f / epsilon;
				--safety;
			}
		    var absA = Math.Abs(a);
		    var absB = Math.Abs(b);
			var diff = Math.Abs(a - b);
		
			return  diff <= epsilon || // Absolute Error
					diff <= Math.Min/*Max*/(absA, absB) * epsilon; // Relative Error
			#region Old
			// const float MinNormal = 1.17549435E-38f;
			// var absA = Math.Abs(a);
			// var absB = Math.Abs(b);
			// var diff = Math.Abs(a - b);
		
			// if (a.Equals(b))
			//     return true;
			// else if (a == 0 || b == 0 || absA + absB < MinNormal) 
			//     // a or b is zero or both are extremely close to it; relative error is less meaningful here
			//     return diff < (epsilon * MinNormal);
			// else // use relative error
			//     return diff / (absA + absB) < epsilon;
			#endregion
		}
		#endregion

		#region Miscellaneous
		public static void LogQualifiedName(this Type type)
		{
			Debug.Log($"{type.AssemblyQualifiedName}, {type.Assembly}");
		}

		public static bool HasFlag<T>(this T[] enumFlags, T flag) where T : Enum
		{
			for (int i = 0; i < enumFlags.Length; i++)
				if (enumFlags[i].HasFlag(flag))
					return true;
			return false;
		}

		public static bool IsInRange(this float value, float min, float max) => value > min && value < max/* || value < min && value > max*/;
		public static bool IsInRange(this float value, Vector2 range) => value > range.x && value < range.y || value < range.x && value > range.y;

		#region Casting
		#nullable enable
		public static T? As<T>(this object o) where T : class
		{
			try { return o as T; }
			catch (InvalidCastException) { return null; }
		}
		#nullable restore
		#endregion
		#endregion
	}
}