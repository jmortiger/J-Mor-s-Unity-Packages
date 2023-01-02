using UnityEngine;

namespace JMor.AimAssist
{
	[CreateAssetMenu(fileName = "mm_ccm_new", menuName = "Scriptable Object/ConstantMutatorMode")]
	public class ConstantMutatorMode : MutatorMode
	{
		[Header("Correction Settings")]
		[Tooltip("The amount of the total range of input correction to use for strong input correction.")]
		[Range(0, 1)]
		public float inputRangeAmount = .75f;
		// [Tooltip(
		// 	"How much should the range of angles a target takes up be expanded (or contracted) by?\n" +
		// 	"i.e. A target takes up 20 degrees of the targeter's total 360 degrees. A multiplier of 2 means the target will have correction applied across 40 degrees.")]
		// public float inputCorrectionRangeMultiplier = 2.5f;
		[Tooltip("The desired angular size of the target. If the target's true angular size is less than or greater than this, the corrected size will be increased or decreased to attempt to match this value.")]
		[Range(0, 180)]
		public float desiredAngularSize = 30f;
		/// <summary>
		/// Constructs an array of inputs and outputs for the interpolator by mutating the given range by its set configuration.
		/// </summary>
		/// <param name="xInputMin"></param>
		/// <param name="xInputMax"></param>
		/// <returns></returns>
		/// <remarks>
		/// This is where the core behaviour of the algorithm is defined. 
		/// Cannot mutate the output (Vector2.y), as <see cref="OverlapResolutionMode.MutateInputRanges(Vector2[], out float[], out float[], System.Func{float, float, Vector2[]})"/> 
		/// uses this before mutation to prevent overlap.
		/// </remarks>
		override public Vector2[] MutateInputRange(float xInputMin, float xInputMax)
		{
			// Edge case: Values are identical
			if (xInputMax == xInputMin)
				return new Vector2[] { new Vector2(xInputMin, xInputMax) };
			var mid = (xInputMax + xInputMin) / 2f;
			// var step = (xInputMax - xInputMin) / 2f;
			var offset = MutateInputRange_ApplyResets(out var inputOutputArray, desiredAngularSize, mid, 3, correctionResetMode);
			float delta = desiredAngularSize * inputRangeAmount;
			inputOutputArray[0 + offset] = new Vector2(mid - delta, xInputMin);
			inputOutputArray[1 + offset] = new Vector2(mid, mid);
			inputOutputArray[2 + offset] = new Vector2(mid + delta, xInputMax);
			if (doDebug) Debug.Log($"Angular Range: {xInputMin} - {xInputMax} ({xInputMax - xInputMin}) -> {inputOutputArray[0]} - {inputOutputArray[^1]} ({inputOutputArray[^1] - inputOutputArray[0]})");
			return inputOutputArray;
		}
	}
}