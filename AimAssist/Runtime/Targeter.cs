using JMor.Utility;
using JMor.Utility.Input;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using MyPIDController = JMor.AimAssist.PIDControllerObj;

namespace JMor.AimAssist
{
	// TODO: Create 3D variant.
	// TODO: Add mode for target update (i.e. naively update targets every frame, update on event, update on unity message,...). Be careful about instanced data.
	// TODO: Do this: Whenever the player input angle changes, I "pay back small amounts of debt", hiding the error inside of motion. If the player input changes dramatically enough (e.g., a 90 degree sudden change) I just pay back the debt all at once, and it is not noticeable to the player. 
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
		public float priorInputAngle = float.MinValue;
		public Vector2 priorInputVector = Vector2.negativeInfinity;
		public PlayerInput input;
		[Header("Only one of the following 2 fields needs to be a valid value.")]
		public string aimActionName = "Aim";
		public InputActionReference aimAction;
		[Header("Core Behaviour")]
		[Expandable] public OverlapResolutionMode overlapResolutionMode;
		[Expandable] public MutatorMode mutatorMode;
		public enum BehaviourPriority
		{
			PrioritizeTarget,
			PrioritizeTargeter,
		}
		public BehaviourPriority overlapResolutionModePriority;
		public BehaviourPriority mutatorModePriority;
		public enum ApplicationType
		{
			Direct,
			PID,
		}
		public ApplicationType applicationType = ApplicationType.PID;
		[Expandable]
		public MyPIDController pid;
		void Reset()
		{
			input = GetComponent<PlayerInput>();
			mutatorMode = ScriptableObject.CreateInstance<LinearMutatorMode>();
			overlapResolutionMode = ScriptableObject.CreateInstance<AverageOverlapsMode>();
			pid = new MyPIDController(1);
		}
		private Func<float, float> currAngleInterpolator;
		/*void Update()
		{
			var targets = FindObjectsOfType<Targetable>();
			var targetRanges = GetViableTargetRanges(targets);
			var interpolator = GetAngleInterpolator(targetRanges);
			currAngleInterpolator = interpolator;
			var aimAction = this.aimAction.action ?? input.actions.FindAction(aimActionName);
			var rawInputDirection = aimAction.ReadValue<Vector2>();
			if (aimAction?.activeControl?.device is Mouse)// Unity doesn't support actions in Edit mode
				rawInputDirection = GetVectorFromMeToScreenPos(rawInputDirection);
			if (rawInputDirection == Vector2.zero)
			{
				TargetAngle = 0f;
				TargetOrientation = Quaternion.identity;//transform.rotation = Quaternion.identity;
				TargetDirection = Vector2.zero;
				return;
			}
			var angle = MyMath.SignedAngle(Vector2.right, rawInputDirection);
			TargetAngle = interpolator(angle);
			TargetOrientation = Quaternion.Euler(0, 0, TargetAngle);
			TargetDirection = TargetOrientation * Vector2.right;
		}*/

		#region GetViableTargetRanges
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
		#endregion

		#region GetAngleInterpolator
		public Func<float, float> GetAngleInterpolator()
		{
			var targetRanges = GetViableTargetRanges/*WithTargets*/(FindObjectsOfType<Targetable>());
			return GetAngleInterpolator(targetRanges);
		}

		// public Func<float, float> GetAngleInterpolator(Tuple<Vector2, Targetable>[] targetRanges)
		// {
		// 	List<float> xsList, ysList;
		// 	if (overlapResolutionMode == null)
		// 		overlapResolutionMode = ScriptableObject.CreateInstance<AverageOverlapsMode>();
		// 	if (mutatorMode == null)
		// 		mutatorMode = ScriptableObject.CreateInstance<LinearMutatorMode>();
		// 	overlapResolutionMode.MutateInputRanges(targetRanges, out xsList, out ysList, mutatorMode.MutateInputRange);
		// 	float[] inputs = xsList.ToArray(), outputs = ysList.ToArray();
		// 	return MyMath.ConstructInterpolatorFunction(inputs, outputs);
		// }

		public Func<float, float> GetAngleInterpolator(Vector2[] targetRanges)
		{
			List<float> xsList, ysList;
			if (overlapResolutionMode == null)
				overlapResolutionMode = ScriptableObject.CreateInstance<AverageOverlapsMode>();
			if (mutatorMode == null)
				mutatorMode = ScriptableObject.CreateInstance<LinearMutatorMode>();
			overlapResolutionMode.MutateInputRanges(targetRanges, out xsList, out ysList, mutatorMode.MutateInputRange);
			float[] inputs = xsList.ToArray(), outputs = ysList.ToArray();
			return MyMath.ConstructInterpolatorFunction(inputs, outputs, 1/*0*/);
		}
		#endregion

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
		/// <param name="screenPos">Represents the xy screen-space coordinates and the z value represents the distance from the camera.</param>
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
		#region TESTING
		[ContextMenu("Test Interpolator")]
		public void TestInterpolator()
		{
			Debug.Log($"-----INTERPOLATOR TEST: START-----");
			Debug.Log($"\t---CONSTRUCTING INTERPOLATOR: START---");
			var targets = FindObjectsOfType<Targetable>();
			var targetRanges = GetViableTargetRanges(targets);
			var interpolator = GetAngleInterpolator(targetRanges);
			Debug.Log($"\t---CONSTRUCTING INTERPOLATOR: END---");
			Debug.Log($"\t---TESTING INTERPOLATOR: START---");
			var testTargetAngle = 0f;
			var testTargetOrientation = Quaternion.identity;
			var testTargetDirection = Vector2.zero;
			var testTargetAngle_cached = 0f;
			var testTargetOrientation_cached = Quaternion.identity;
			var testTargetDirection_cached = Vector2.zero;
			for (float angle = 0f; angle < 360f; angle++)
			{
				testTargetAngle_cached = testTargetAngle;
				testTargetOrientation_cached = testTargetOrientation;
				testTargetDirection_cached = testTargetDirection;

				testTargetAngle = interpolator(angle);
				testTargetOrientation = Quaternion.Euler(0, 0, testTargetAngle);
				testTargetDirection = testTargetOrientation * Vector2.right;
				if (angle == 0f)
				{
					testTargetAngle_cached = testTargetAngle;
					testTargetOrientation_cached = testTargetOrientation;
					testTargetDirection_cached = testTargetDirection;
				}
				/*if (angle == testTargetAngle && !(testTargetAngle - testTargetAngle_cached < 0)) {
					// Debug.Log($"\t-{angle} -> {testTargetAngle}-");
				}
				else*/
				if (angle != testTargetAngle || testTargetAngle - testTargetAngle_cached < 0)
				{
					Debug.Log($"\t\t-{angle}-");
					Debug.Log(message: $"\t\t\tInput Angle: {angle}; Input Direction: {Quaternion.Euler(0, 0, angle) * Vector2.right}");
					Debug.Log($"\t\t\tOutput Angle: {testTargetAngle}; Output Direction: {testTargetDirection}");
					if (testTargetAngle - testTargetAngle_cached < 0)
						Debug.LogWarning($"\t\t\tDelta Output Angle: {testTargetAngle - testTargetAngle_cached}; Likely not monotone.");
					else
						Debug.Log($"\t\t\tDelta Output Angle: {testTargetAngle - testTargetAngle_cached}");
					Debug.Log($"\t\t\tDelta Input to Output Angle: {testTargetAngle - angle}");
				}
			}
			Debug.Log($"\t---TESTING INTERPOLATOR: END---");
			Debug.Log($"-----INTERPOLATOR TEST: END-----");
		}
		public float currentAngle = 0;
		[ContextMenu("Reset Values")]
		void ResetValues()
		{
			currentAngle = 0;
			pid.ResetControllerState();
			cumulativeTime = 0;
			deltas.Clear();
			mins.Clear();
			maxs.Clear();
			cTimes.Clear();
			outs.Clear();
			ins.Clear();
		}
		public double cumulativeTime = 0;
		List<float> mins = new List<float>();
		List<float> maxs = new List<float>();
		List<double> deltas = new List<double>();
		[ContextMenu("Get Frequencies")]
		void GetFrequencies()
		{
			Debug.LogWarning("Getting Frequencies");
			var minsFrequencies = new List<uint>();
			var minsValues = new List<float>();
			var maxsFrequencies = new List<uint>();
			var maxsValues = new List<float>();
			var deltasFrequencies = new List<uint>();
			var deltasValues = new List<double>();
			for (int i = 0; i < mins.Count; i++)
			{
				var index = minsValues.FindIndex(currVal => { return currVal.Approximately(mins[i], 1E-6f); });
				if (index < 0)
				{
					minsValues.Add(mins[i]);
					minsFrequencies.Add(1);
				}
				else
					minsFrequencies[index] += 1;
				index = maxsValues.FindIndex(currVal => { return currVal.Approximately(maxs[i], 1E-6f); });
				if (index < 0)
				{
					maxsValues.Add(maxs[i]);
					maxsFrequencies.Add(1);
				}
				else
					maxsFrequencies[index] += 1;
				index = deltasValues.FindIndex(currVal => { return currVal.Approximately(deltas[i], 1E-6d); });
				if (index < 0)
				{
					deltasValues.Add(deltas[i]);
					deltasFrequencies.Add(1);
				}
				else
					deltasFrequencies[index] += 1;
			}
			for (int i = 0; i < deltasValues.Count; i++)
				Debug.LogWarning($"Deltas[{i}]: {deltasValues[i]} (Frequency: {deltasFrequencies[i]})");
			for (int i = 0; i < minsValues.Count; i++)
				if (minsFrequencies[i] >= 3)
					Debug.LogWarning($"Mins[{i}]: {minsValues[i]} (Frequency: {minsFrequencies[i]})");
			for (int i = 0; i < maxsValues.Count; i++)
				if (maxsFrequencies[i] >= 3)
					Debug.LogWarning($"Maxs[{i}]: {maxsValues[i]} (Frequency: {maxsFrequencies[i]})");
			Debug.LogWarning("Done With Frequencies");
		}
		List<float> outs = new List<float>();
		List<float> ins = new List<float>();
		List<double> cTimes = new List<double>();
		[ContextMenu("Get Responses")]
		void GetResponses()
		{
			Debug.LogWarning("Getting Responses");
			var ssv = "";
			for (int i = 0; i < cTimes.Count; i++)
				ssv += $"{cTimes[i]:f9},{ins[i]:f9},{outs[i]:f9}\r\n";
			cTimes.Clear();
			outs.Clear();
			ins.Clear();
			Debug.LogWarning(ssv);
			Debug.LogWarning("Done With Responses");
		}
		void Start()
		{
			cumulativeTime = 0;
			Debug.LogWarning($"myDT: {myDT}");
		}
		private double myDT { get => (1d / 60d) / myDTDivisor; }
		public double myDTDivisor = 16d;
		#endregion
		public bool debugsInGizmos = false;
		public bool debugDownstream = false;
		public bool useDiscreteVelocityFormForIntegral = false;
		public bool correctTimeScaleForIntegral = true;
		public float timeScalerForIntegral = 1000f;
		public bool correctTimeScaleForDerivative = true;
		public float timeScalerForDerivative = 1000f;
		public bool useAlternateDerivativeForm = false;
		public float maxAccumulatedError = float.PositiveInfinity;
		public float minAccumulatedError = float.NegativeInfinity;
		private void OnDrawGizmosSelected()
		{
			cumulativeTime += myDT;
			var targets = FindObjectsOfType<Targetable>();
			foreach (var t in targets)
				t.DrawGizmos(this);
			Gizmos.DrawRay(transform.position, Vector2.right * 3f);
			var rawInputDirection = (Gamepad.current != null) ? Gamepad.current.leftStick.ReadValue() : MouseHelper.ScreenPosition;
			// Debug.Log($"rawInputDirection:{rawInputDirection}");
			var inputWorldMagnitude = 4f;
			if (Gamepad.current == null)
			{
				if (debugsInGizmos) Debug.Log("Gamepad.current = null; using mouse input.");
				if (debugsInGizmos) Debug.Log($"ScreenToWorldPoint:{Camera.main.ScreenToWorldPoint(rawInputDirection)}");
				rawInputDirection = GetVectorFromMeToScreenPos(rawInputDirection);
				inputWorldMagnitude = rawInputDirection.magnitude;
			}
			// Debug.Log($"AimInput:{rawInputDirection}");
			var unalteredInputVector = rawInputDirection;
			rawInputDirection.Normalize();
			// Debug.Log($"AimInputNormalized:{rawInputDirection}");
			// If there's no direction (i.e. Joystick neutral)...
			if (rawInputDirection == Vector2.zero)
				//	rawInputDirection = Vector2.right;// ...avoid errors by setting to a neutral value?
				return;// ...exit early
			var angle = Vector2.SignedAngle(Vector2.right, rawInputDirection);
			angle = (angle < 0) ? angle + 360 : angle;
			var calculatedInputDir = (Vector2)(Quaternion.Euler(0, 0, angle) * Vector2.right);
			if (debugsInGizmos) Debug.Log($"InputAngle: {angle}");
			if (calculatedInputDir == Vector2.right && rawInputDirection == Vector2.zero)
				calculatedInputDir = Vector2.zero;
			// TODO: Asserts only seem to go wrong at ~180 & ~0/360 degrees. Why?
			if (Vector2.Distance(calculatedInputDir, rawInputDirection) > Vector2.kEpsilon * 10)
				if (!(calculatedInputDir == Vector2.right && rawInputDirection == Vector2.zero))
					if (debugsInGizmos) Debug.LogWarning($"Failed Assert: {calculatedInputDir} != {rawInputDirection}, it should; Input Angle: {angle}");

			Gizmos.DrawSphere(rawInputDirection * inputWorldMagnitude + (Vector2)transform.position, 1);
			Gizmos.DrawLine(transform.position, transform.position + (Vector3)calculatedInputDir * inputWorldMagnitude);
			// TODO: Asserts only seem to go wrong at ~180 & ~0/360 degrees. Why?
			if (Vector2.Distance((Vector2)transform.position + calculatedInputDir * inputWorldMagnitude, rawInputDirection * inputWorldMagnitude + (Vector2)transform.position) > Vector2.kEpsilon * 10)
				if (debugsInGizmos) Debug.LogWarning($"Failed Assert: {(Vector2)transform.position + calculatedInputDir * inputWorldMagnitude} != {rawInputDirection * inputWorldMagnitude + (Vector2)transform.position}, it should; Input Angle: {angle}");

			var targetRanges = GetViableTargetRanges(targets);
			// for (int i = 0; i < targetRanges.Length && debugsInGizmos; i++)
			// 	Debug.Log($"TargetRanges[{i}]: {targetRanges[i].x} - {targetRanges[i].y}");

			if (overlapResolutionMode == null)
				overlapResolutionMode = ScriptableObject.CreateInstance<AverageOverlapsMode>();
			if (mutatorMode == null)
				mutatorMode = ScriptableObject.CreateInstance<LinearMutatorMode>();
			overlapResolutionMode.MutateInputRanges(targetRanges, out float[] inputs, out float[] outputs, mutatorMode.MutateInputRange);
			for (int i = 0; i < inputs.Length; i++)
			{
				// if (debugsInGizmos) Debug.Log($"in: {inputs[i]} out: {outputs[i]}");
				var positInput = (Vector2)(Quaternion.Euler(0, 0, inputs[i]) * Vector2.right);
				positInput = transform.position + (Vector3)positInput * /*inputWorldMagnitude*/4f;
				Gizmos.color = new Color(0, 1, 0, .5f);
				Gizmos.DrawSphere(new Vector3(positInput.x, positInput.y, transform.position.z), .1f);
				var positOutput = (Vector2)(Quaternion.Euler(0, 0, outputs[i]) * Vector2.right);
				positOutput = transform.position + (Vector3)positOutput * /*inputWorldMagnitude*/4f;
				Gizmos.color = new Color(0, 0, 1, .5f);
				Gizmos.DrawSphere(new Vector3(positOutput.x, positOutput.y, transform.position.z), .1f);
			}
			var interpolator = GetAngleInterpolator(targetRanges);
			var mutatedAngle = interpolator(angle);
			if (debugsInGizmos) Debug.Log($"Mutated Angle: {mutatedAngle}");
			var mutatedDirection = (Vector2)(Quaternion.Euler(0, 0, mutatedAngle) * Vector2.right);
			// if (debugsInGizmos) Debug.Log($"Mutated Direction: {mutatedDirection}");
			Gizmos.color = Color.yellow * 2;
			for (int i = 0; i < targetRanges.Length; i++)
				if (mutatedAngle.IsInRange(targetRanges[i]))
				{
					Gizmos.color = Color.red;
					break;
				}
			Gizmos.DrawLine(transform.position, transform.position + (Vector3)mutatedDirection * inputWorldMagnitude);
			if (debugsInGizmos) Debug.Log($"Current Angle: {currentAngle}");
			Gizmos.color = Color.cyan;
			Gizmos.DrawSphere((Vector2)(Quaternion.Euler(0, 0, currentAngle) * Vector2.right) * inputWorldMagnitude + (Vector2)transform.position, 1);
			Gizmos.DrawLine(transform.position, transform.position + (Vector3)(Vector2)(Quaternion.Euler(0, 0, currentAngle) * Vector2.right) * inputWorldMagnitude);
			if (applicationType == ApplicationType.Direct)
				return;
			// Func<float, float> valueAdjuster = x => {
			// 	x %= 360f;
			// 	if (x < 0)
			// 		x += 360;
			// 	if (x > 180)
			// 		return x - 360;
			// 	if (x < -180)
			// 		return x + 360;
			// 	return x;
			// };
			Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = (currentError, errorAtTMinus0, errorAtTMinus1, errorAtTMinus2) =>
			{
				Func<float, float> normalModification = x =>
				{
					x %= 360f;
					if (x < 0)
						x += 360;
					if (x > 180)
						return x - 360;
					if (x < -180)
						return x + 360;
					return x;
				};
				currentError = normalModification(currentError);
				errorAtTMinus0 = normalModification(errorAtTMinus0);
				errorAtTMinus1 = normalModification(errorAtTMinus1);
				errorAtTMinus2 = normalModification(errorAtTMinus2);
				return new Tuple<float, float, float, float>(currentError, errorAtTMinus0, errorAtTMinus1, errorAtTMinus2);
			};
			pid.maxAccumulatedError = maxAccumulatedError;
			pid.minAccumulatedError = minAccumulatedError;
			Debug.Log($"useDiscreteVelocityFormForIntegral: {useDiscreteVelocityFormForIntegral}");
			var correctedAngle = debugsInGizmos ?
				pid.CalculateIdealForm_DEBUG(
					currentAngle,
					mutatedAngle,
					(float)myDT,
					valueAdjuster,
					useDiscreteVelocityFormForIntegral,
					correctTimeScaleForIntegral,
					timeScalerForIntegral,
					correctTimeScaleForDerivative,
					timeScalerForDerivative,
					useAlternateDerivativeForm,
					debugDownstream) + currentAngle :
				pid.CalculateIdealForm(
					currentAngle,
					mutatedAngle,
					(float)myDT,
					valueAdjuster,
					useDiscreteVelocityFormForIntegral,
					correctTimeScaleForIntegral,
					timeScalerForIntegral,
					correctTimeScaleForDerivative,
					timeScalerForDerivative,
					useAlternateDerivativeForm) + currentAngle;
			if (debugsInGizmos) Debug.Log($"Accumulated Error: {pid.AccumulatedError}");
			if (Application.isPlaying)
			{
				cTimes.Add(cumulativeTime);
				ins.Add(currentAngle);
				outs.Add(correctedAngle);
			}
			correctedAngle = MyMath.Wrap(correctedAngle, 0f, 360f);
			if (debugsInGizmos) Debug.Log($"Corrected Angle: {correctedAngle}");
			var correctedDirection = (Vector2)(Quaternion.Euler(0, 0, correctedAngle) * Vector2.right);
			// if (debugsInGizmos) Debug.Log($"Corrected Direction: {correctedDirection}");
			Gizmos.color = Color.magenta;
			for (int i = 0; i < targetRanges.Length; i++)
				if (correctedAngle.IsInRange(targetRanges[i]))
				{
					Gizmos.color = Color.red;
					break;
				}
			Gizmos.DrawLine(transform.position, transform.position + (Vector3)correctedDirection * inputWorldMagnitude);
			if (debugsInGizmos) currentAngle = float.IsFinite(correctedAngle) ? correctedAngle : currentAngle;
			priorInputAngle = angle;
			priorInputVector = unalteredInputVector;
		}
	}
}