using JMor.Utility;
using JMor.Utility.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JMor.AimAssist
{
	// TODO: Create 3D variant.
	/* NOTE:
	 * This is actually a very interesting problem, because in top-down games, the player's angular input is expected to map directly to a world angle, as opposed to say FPS aim assist, were you're only manipulating the 1st derivative of your aim position. I was surprised when I realized the math is going to be totally different.
	 * I took a similar approach to you, however ran into a problem further down the road: When the entity I'm currently aim-assisted towards dies (eg something else kills it) or blinks away, the mapping function suddenly changes. The region around the player's current input sudden snaps back to the identity function, causing the player to perceive a jump in the output position despite holding their input angle steady. This temporal discontinuity was very jarring and violated the player expectation that "if I hold my finger still, so should my aiming line".
	 * I ended up implementing essentially the same function, but on the first derivative. That way, if the curve "changes behind the scenes" you wouldn't notice any discontinuity, only that passing back to the area now feels "faster" than before.
	 * This creates a new problem, which is that you will accumulate angular error, where after all targets are eliminated, a player input of "due north" may no longer map to due north in the game world! I ended up solving this by tracking how much error there is between player input and game world output, keeping that as a "debt to be repaid". Whenever the player input angle changes, I "pay back small amounts of debt", hiding the error inside of motion. If the player input changes dramatically enough (e.g., a 90 degree sudden change) I just pay back the debt all at once, and it is not noticeable to the player.
	 */
	[RequireComponent(typeof(PlayerInput))]
	public class Targeter : MonoBehaviour
	{
		public float TargetAngle { get; private set; }
		public Vector2 TargetDirection { get; private set; }
		public Quaternion TargetOrientation { get; private set; }
		public PlayerInput input;
		[Header("Only one of the following 2 fields needs to be a valid value.")]
		public string aimActionName = "Aim";
		public InputActionReference aimAction;
		[Header("Core Behaviour")]
		[Expandable] public OverlapResolutionMode overlapResolutionMode;
		[Expandable] public MutatorMode mutatorMode;
		// TODO: Add mode for target update (i.e. naively update targets every frame, update on event, update on unity message,...). Be careful about instanced data.
		void Reset()
		{
			input = GetComponent<PlayerInput>();
			mutatorMode = ScriptableObject.CreateInstance<LinearMutatorMode>();
		}
		private Func<float, float> currAngleInterpolater;
		void Update()
		{
			var targets = FindObjectsOfType<Targetable>();
			var targetRanges = GetViableTargetRanges(targets);
			var interpolater = GetAngleInterpolater(targetRanges);
			currAngleInterpolater = interpolater;
			var aimAction = this.aimAction.action ?? input.actions.FindAction(aimActionName);
			var rawInputDirection = aimAction.ReadValue<Vector2>();
			if (aimAction?.activeControl?.device is Mouse)// Unity doesn't support actions in Edit mode
				rawInputDirection = GetVectorFromMeToScreenPos(rawInputDirection);
			if (rawInputDirection == Vector2.zero)
			{
				TargetAngle = 0f;
				TargetOrientation/*transform.rotation*/ = Quaternion.identity;
				TargetDirection = Vector2.zero;
				return;
			}
			var angle = MyMath.SignedAngle(Vector2.right, rawInputDirection);
			TargetAngle = interpolater(angle);
			TargetOrientation = Quaternion.Euler(0, 0, TargetAngle);
			TargetDirection = TargetOrientation * Vector2.right;
		}
		public Vector2[] GetViableTargetRanges(Targetable[] targets)
		{
			var targetRanges = new Vector2[targets.Length];
			for (int i = 0; i < targets.Length; i++)
				targetRanges[i] = targets[i].GetInputRange(transform.position, Vector2.right);
			Array.Sort(targetRanges, new CompareVector2ByX());
			for (uint i = 1; i < targetRanges.Length && targetRanges[i].IsFinite(); i++)
			{
				// BUG: If both are exactly equal, it's a coin toss as to which gets axed. Insanely unlikely to ever be a concern, but I'll make note of it here regardless.
				// If the previous element's max is greater than or equal to mine (and the previous element's max is less than or equal to mine)...
				if (targetRanges[i - 1].y >= targetRanges[i].y)
				{
					// ... then I'm being occluded by them. Don't process my range, you can't hit me anyways.
					targetRanges.RemoveAt(i, out Vector2[] t);
					targetRanges = t;
					i--;
				}
			}
			return targetRanges;
		}
		class CompareByAssociatedVector2 : IComparer<Tuple<Vector2, Targetable>>
		{
			private readonly Tuple<Vector2, Targetable>[] targetRanges;
			public CompareByAssociatedVector2(ref Tuple<Vector2, Targetable>[] targetRanges)
			{
				this.targetRanges = targetRanges;
			}
			public int Compare(Tuple<Vector2, Targetable> x, Tuple<Vector2, Targetable> y)
			{
				int xIndex = -1, yIndex = -1;
				int i = 0;
				while (i < targetRanges.Length)
				{
					if (targetRanges[i].Item2 == x.Item2)
						xIndex = i;
					else if (targetRanges[i].Item2 == y.Item2)
						yIndex = i;
					if (xIndex < 0 && yIndex < 0)
						i++;
				}

				return new CompareVector2ByX().Compare(targetRanges[xIndex].Item1, targetRanges[yIndex].Item1);
			}
		}
		// TODO: Test
		public Tuple<Vector2, Targetable>[] GetViableTargetRangesWithTargets(Targetable[] targets)
		{
			var targetRanges = new Tuple<Vector2, Targetable>[targets.Length];
			for (int i = 0; i < targets.Length; i++)
				targetRanges[i] = new(targets[i].GetInputRange(transform.position, Vector2.right), targets[i]);
			Array.Sort(targetRanges, new CompareByAssociatedVector2(ref targetRanges));
			for (uint i = 1; i < targetRanges.Length && targetRanges[i].Item1.IsFinite(); i++)
			{
				// BUG: If both are exactly equal, it's a coin toss as to which gets axed. Insanely unlikely to ever be a concern, but I'll make note of it here regardless.
				// If the previous element's max is greater than or equal to mine (and the previous element's max is less than or equal to mine)...
				if (targetRanges[i - 1].Item1.y >= targetRanges[i].Item1.y)
				{
					// ... then I'm being occluded by them. Don't process my range, you can't hit me anyways.
					targetRanges.RemoveAt(i, out Tuple<Vector2, Targetable>[] t);
					targetRanges = t;
					i--;
				}
			}
			return targetRanges;
		}

		public Func<float, float> GetAngleInterpolater()
		{
			var targetRanges = GetViableTargetRanges(FindObjectsOfType<Targetable>());
			return GetAngleInterpolater(targetRanges);
		}

		public Func<float, float> GetAngleInterpolater(Vector2[] targetRanges)
		{
			List<float> xsList, ysList;
			if (overlapResolutionMode == null)
				overlapResolutionMode = ScriptableObject.CreateInstance<AverageOverlapsMode>();//MutateInputRanges_AverageOverlaps(targetRanges, out xsList, out ysList);
			if (mutatorMode == null)
				mutatorMode = ScriptableObject.CreateInstance<LinearMutatorMode>();
			overlapResolutionMode.MutateInputRanges(targetRanges, out xsList, out ysList, mutatorMode.MutateInputRange);
			float[] inputs = xsList.ToArray(), outputs = ysList.ToArray();
			return MyMath.ConstructInterpolaterFunction(inputs, outputs);
		}

		#region GetVectorFromMeToScreenPos
		/// <summary>
		/// 
		/// </summary>
		/// <param name="screenPos"></param>
		/// <returns></returns>
		/// <remarks>Only works when <see cref="Camera.main"/> is orthographic and aligned with the z axis.</remarks>
		private Vector2 GetVectorFromMeToScreenPos(Vector2 screenPos) => (Vector2)Camera.main.ScreenToWorldPoint(screenPos) - (Vector2)transform.position;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="screenPos">Represents the xy screenspace coordinates and the z value represents the distance from the camera.</param>
		/// <returns></returns>
		private Vector3 GetVectorFromMeToScreenPos(Vector3 screenPos) => Camera.main.ScreenToWorldPoint(screenPos) - transform.position;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="screenPos"></param>
		/// <param name="distanceFromCamera"></param>
		/// <returns></returns>
		private Vector3 GetVectorFromMeToScreenPos(Vector2 screenPos, float distanceFromCamera) => Camera.main.ScreenToWorldPoint(new(screenPos.x, screenPos.y, distanceFromCamera)) - transform.position;
		#endregion

		private void OnDrawGizmosSelected()
		{
			var targets = FindObjectsOfType<Targetable>();
			foreach (var t in targets)
				t.DrawGizmos(this);
			Gizmos.DrawRay(transform.position, Vector2.right * 3f);
			var rawInputDirection = (Gamepad.current != null) ? Gamepad.current.leftStick.ReadValue() : MouseHelper.ScreenPosition;
			Debug.Log($"rawInputDirection:{rawInputDirection}");
			var inputWorldMagnitude = 4f;
			if (Gamepad.current == null)
			{
				Debug.Log("Gamepad.current = null; using mouse input.");
				Debug.Log($"ScreenToWorldPoint:{Camera.main.ScreenToWorldPoint(rawInputDirection)}");
				rawInputDirection = GetVectorFromMeToScreenPos(rawInputDirection);
				inputWorldMagnitude = rawInputDirection.magnitude;
			}
			Debug.Log($"AimInput:{rawInputDirection}");
			rawInputDirection.Normalize();
			Debug.Log($"AimInputNormalized:{rawInputDirection}");
			// If there's no direction (i.e. Joystick neutral)...
			if (rawInputDirection == Vector2.zero)
			//	rawInputDirection = Vector2.right;// ...avoid errors by setting to a neutral value?
				return;// ...exit early
			var angle = Vector2.SignedAngle(Vector2.right, rawInputDirection);
			var calculatedInputDir = (Vector2)(Quaternion.Euler(0, 0, angle) * Vector2.right);
			angle = (angle < 0) ? angle + 360 : angle;
			Debug.Log($"InputAngle: {angle}");
			if (calculatedInputDir == Vector2.right && rawInputDirection == Vector2.zero)
				calculatedInputDir = Vector2.zero;
			// TODO: Work on buggy asserts
			if (Vector2.Distance(calculatedInputDir, rawInputDirection) > Vector2.kEpsilon * 10)
				if (!(calculatedInputDir == Vector2.right && rawInputDirection == Vector2.zero))
					Debug.LogWarning($"Failed Assert: {calculatedInputDir} != {rawInputDirection}, it should");

			Gizmos.DrawSphere(rawInputDirection * inputWorldMagnitude + (Vector2)transform.position, 1);
			Gizmos.DrawLine(transform.position, transform.position + (Vector3)calculatedInputDir * inputWorldMagnitude);
			// TODO: Work on buggy asserts
			if (Vector2.Distance((Vector2)transform.position + calculatedInputDir * inputWorldMagnitude, rawInputDirection * inputWorldMagnitude + (Vector2)transform.position) > Vector2.kEpsilon * 10)
				Debug.LogWarning($"Failed Assert: {(Vector2)transform.position + calculatedInputDir * inputWorldMagnitude} != {rawInputDirection * inputWorldMagnitude + (Vector2)transform.position}, it should");

			var targetRanges = GetViableTargetRanges(targets);
			for (int i = 0; i < targetRanges.Length; i++)
			{
				Debug.Log($"TargetRanges[{i}]: {targetRanges[i].x} - {targetRanges[i].y}");
			}

			if (overlapResolutionMode == null)
				overlapResolutionMode = ScriptableObject.CreateInstance<AverageOverlapsMode>();//MutateInputRanges_AverageOverlaps(targetRanges, out xsList, out ysList);
			if (mutatorMode == null)
				mutatorMode = ScriptableObject.CreateInstance<LinearMutatorMode>();
			overlapResolutionMode.MutateInputRanges(targetRanges, out float[] inputs, out float[] outputs, mutatorMode.MutateInputRange);
			for (int i = 0; i < inputs.Length; i++)
			{
				Debug.Log($"in: {inputs[i]} out: {outputs[i]}");
				var positInput = (Vector2)(Quaternion.Euler(0, 0, inputs[i]) * Vector2.right);
				positInput = transform.position + (Vector3)positInput * /*inputWorldMagnitude*/4f;
				Gizmos.color = new Color(0, 1, 0, .5f);
				Gizmos.DrawSphere(new Vector3(positInput.x, positInput.y, transform.position.z), .1f);
				var positOutput = (Vector2)(Quaternion.Euler(0, 0, outputs[i]) * Vector2.right);
				positOutput = transform.position + (Vector3)positOutput * /*inputWorldMagnitude*/4f;
				Gizmos.color = new Color(0, 0, 1, .5f);
				Gizmos.DrawSphere(new Vector3(positOutput.x, positOutput.y, transform.position.z), .1f);
			}
			var interpolater = GetAngleInterpolater(targetRanges);
			var mutatedAngle = interpolater(angle);
			Debug.Log($"Mutated Angle: {mutatedAngle}");
			var mutatedDirection = (Vector2)(Quaternion.Euler(0, 0, mutatedAngle) * Vector2.right);
			Debug.Log($"Mutated Direction: {mutatedDirection}");
			Gizmos.color = Color.yellow * 2;
			for (int i = 0; i < targetRanges.Length; i++)
				if (mutatedAngle.IsInRange(targetRanges[i]))
				{
					Gizmos.color = Color.red;
					break;
				}
			Gizmos.DrawLine(transform.position, transform.position + (Vector3)mutatedDirection * inputWorldMagnitude);
		}
	}
}