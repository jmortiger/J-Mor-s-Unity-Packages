using UnityEngine;

namespace JMor.AimAssist
{
	[CreateAssetMenu(fileName = "lmm_newLinearMutatorMode", menuName = "Scriptable Object/Aim Assist/MutatorMode/LinearMutatorMode")]
	public class LinearMutatorMode : MutatorMode
	{
		[Header("Correction Settings")]
		[Tooltip("The amount of the total range where input is corrected to use for strong input correction.")]
		[Range(0, 1)]
		public float inputRangeAmount = .75f;
		[Tooltip(
			"How much should the range of angles a target takes up be expanded (or contracted) by?\n" +
			"i.e. A target takes up 20 degrees of the targeter's total 360 degrees. A multiplier of 2 means the target will have correction applied across 40 degrees.")]
		public float inputCorrectionRangeMultiplier = 2.5f;
		[Tooltip("How should I resolve correction outside of the explicit correction range?")]
		public CorrectionResetMode correctionResetMode = CorrectionResetMode.ResetCorrectionAtEdge;
		/// <summary>
		/// Constructs an array of inputs and outputs for the interpolator by mutating the given range by its set cofiguration.
		/// </summary>
		/// <param name="xInputMin"></param>
		/// <param name="xInputMax"></param>
		/// <returns></returns>
		/// <remarks>
		/// This is where the core behaviour of the algorithm is defined. 
		/// Cannot mutate the output (Vector2.y), as <see cref="MutateInputRanges_AverageOverlaps(Vector2[], out float[], out float[])"/> 
		/// uses this before mutation to prevent overlap.
		/// </remarks>
		override public Vector2[] MutateInputRange(float xInputMin, float xInputMax)
		{
			// Edge case: Values are identical
			if (xInputMax == xInputMin)
				return new Vector2[] { new Vector2(xInputMin, xInputMax) };
			var mid = (xInputMax + xInputMin) / 2f;
			var step = (xInputMax - xInputMin) / 2f;
			Vector2[] inputOutputArray = correctionResetMode switch
			{
				CorrectionResetMode.ResetCorrectionAtEdge => new Vector2[7],
				CorrectionResetMode.EaseCorrectionAtEdge => new Vector2[5],
				_ => new Vector2[3],
			};
			var offset = 0;
			if (correctionResetMode == CorrectionResetMode.ResetCorrectionAtEdge)
			{
				inputOutputArray[offset] = new Vector2(mid - step * (inputCorrectionRangeMultiplier + .0001f), mid - step * (inputCorrectionRangeMultiplier + .0001f));
				inputOutputArray[^(offset + 1)] = new Vector2(mid + step * (inputCorrectionRangeMultiplier + .0001f), mid + step * (inputCorrectionRangeMultiplier + .0001f));
				offset = 1;
			}
			if (correctionResetMode == CorrectionResetMode.ResetCorrectionAtEdge ||
				correctionResetMode == CorrectionResetMode.EaseCorrectionAtEdge)
			{
				inputOutputArray[offset] = new Vector2(mid - step * inputCorrectionRangeMultiplier, mid - step * inputCorrectionRangeMultiplier);
				inputOutputArray[^(offset + 1)] = new Vector2(mid + step * inputCorrectionRangeMultiplier, mid + step * inputCorrectionRangeMultiplier);
				offset++;
			}
			inputOutputArray[0 + offset] = new Vector2(mid - step * inputRangeAmount * inputCorrectionRangeMultiplier, xInputMin);
			inputOutputArray[1 + offset] = new Vector2(mid, mid);
			inputOutputArray[2 + offset] = new Vector2(mid + step * inputRangeAmount * inputCorrectionRangeMultiplier, xInputMax);
			return inputOutputArray;
		}

		public enum CorrectionResetMode : ushort
		{
			/// <summary>
			/// Allow correction to pollute values outside of the explicit correction range.
			/// </summary>
			[Tooltip("Allow correction to pollute values outside of the explicit correction range.")]
			None = 0b00,
			/// <summary>
			/// Reduce correction outside of the explicit correction range.
			/// </summary>
			[Tooltip("Reduce correction outside of the explicit correction range.")]
			EaseCorrectionAtEdge = 0b01,
			/// <summary>
			/// Remove correction outside of the explicit correction range. Can cause problems like the correction pushing values outside of <see cref="inputRangeAmount"/> to the edge of a target.
			/// </summary>
			[Tooltip("Remove correction outside of the explicit correction range. Can cause problems like the correction pushing values outside of Input Range Amount to the edge of a target.")]
			ResetCorrectionAtEdge = 0b11,
		}
	}
}