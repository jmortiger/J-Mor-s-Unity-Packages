using UnityEngine;

namespace JMor.AimAssist
{
	//[CreateAssetMenu(fileName = "mm_newMutatorMode", menuName = "Scriptable Object/MutatorMode")]
	public abstract class MutatorMode : ScriptableObject
	{
		public abstract Vector2[] MutateInputRange(float input, float output);

		/// <summary>
		/// Switch for debug log statements and the like.
		/// </summary>
		[SerializeField]
		protected bool doDebug = false;

		protected int MutateInputRange_ApplyResets(out Vector2[] inputOutputArray, float edgeDistanceFromMid, float midpoint, uint coreEntriesRequired = 3, CorrectionResetMode? correctionResetMode = null) {
			inputOutputArray = (correctionResetMode ?? this.correctionResetMode/*ResetMode*/) switch
			{
				CorrectionResetMode.ResetCorrectionAtEdge => new Vector2[coreEntriesRequired + 4],
				CorrectionResetMode.EaseCorrectionAtEdge => new Vector2[coreEntriesRequired + 2],
				_ => new Vector2[coreEntriesRequired],
			};
			float delta;
			var offset = 0;
			if (correctionResetMode == CorrectionResetMode.ResetCorrectionAtEdge)
			{
				delta = edgeDistanceFromMid + Vector2.kEpsilon;// delta = step * (inputCorrectionRangeMultiplier + Vector2.kEpsilon);
				inputOutputArray[offset] = new Vector2(midpoint - delta, midpoint - delta);
				inputOutputArray[^(offset + 1)] = new Vector2(midpoint + delta, midpoint + delta);
				offset = 1;
			}
			if (correctionResetMode == CorrectionResetMode.ResetCorrectionAtEdge ||
				correctionResetMode == CorrectionResetMode.EaseCorrectionAtEdge)
			{
				delta = edgeDistanceFromMid;// delta = step * inputCorrectionRangeMultiplier;
				inputOutputArray[offset] = new Vector2(midpoint - delta, midpoint - delta);
				inputOutputArray[^(offset + 1)] = new Vector2(midpoint + delta, midpoint + delta);
				offset++;
			}
			return offset;
		}

		// public virtual CorrectionResetMode ResetMode { get; set; } = CorrectionResetMode.None;
		[Tooltip("How should I resolve correction outside of the explicit correction range?")]
		public CorrectionResetMode correctionResetMode = CorrectionResetMode.ResetCorrectionAtEdge;

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