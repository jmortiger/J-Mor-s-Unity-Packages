using System;
using UnityEngine;

namespace JMor.AimAssist
{
	[CreateAssetMenu(fileName = "mm_imm_InverseMutatorMode", menuName = "Scriptable Object/InverseMutatorMode")]
	public class InverseMutatorMode : MutatorMode // TODO: Test
	{
		[Header("Correction Settings")]
		[Tooltip("The amount of the total range of input correction to use for strong input correction.")]
		[Range(0, 1)]
		public float inputRangeAmount = .75f;
		// [Tooltip(
		// 	"How much should the range of angles a target takes up be expanded (or contracted) by?\n" +
		// 	"i.e. A target takes up 20 degrees of the targeter's total 360 degrees. A multiplier of 2 means the target will have correction applied across 40 degrees.")]
		// public float inputCorrectionRangeMultiplier = 2.5f;
		public enum InversionMode
		{
			NotModified = 0,
			ConstantMedian,
			ConstantMin,
			ConstantMax,
			Inverted,
		}
		[Tooltip(
			"How should I map true angular size to desired angular size?\n" +
			"i.e. A value of ((0, 90), inverted) means a target w/ a true angular size of 10 would have an output size of 80, and all values outside would be the same as the last Vector2.y (in this case, 90).\n" +
			"These are applied from 1st to last. If 2 ranges overlap, the first one that includes the true angular size will be used." +
			"i.e. A value of ((45, 360), constantMin), ((0, 90), inverted) means a target w/ a true angular size of 0 - 45 would have an output size of 90 - 45, and a target w/ a true angular size of 45 - 360 would have an output size of 45.")]
		public Tuple<Vector2, InversionMode>[] inversionRanges = new Tuple<Vector2, InversionMode>[2] {
			new(new(45, 360), InversionMode.ConstantMin),
			new(new(0, 90), InversionMode.Inverted)
		};
		/// <summary>
		/// Constructs an array of inputs and outputs for the interpolator by mutating the given range by its set configuration.
		/// </summary>
		/// <param name="xInputMin"></param>
		/// <param name="xInputMax"></param>
		/// <returns></returns>
		/// <remarks>
		/// This is where the core behaviour of the algorithm is defined. 
		/// Cannot mutate the output (Vector2.y), as <see cref="OverlapResolutionMode.MutateInputRanges(Vector2[], out float[], out float[], Func{float, float, Vector2[]})"/> 
		/// uses this before mutation to prevent overlap.
		/// </remarks>
		override public Vector2[] MutateInputRange(float xInputMin, float xInputMax)
		{
			// Edge case: Values are identical
			if (xInputMax == xInputMin)
				return new Vector2[] { new Vector2(xInputMin, xInputMax) };
			var mid = (xInputMax + xInputMin) / 2f;
			var step = (xInputMax - xInputMin) / 2f;
			var angularSize = xInputMax - xInputMin;
			float mutatedStep = step;
			for (int i = 0; i < inversionRanges.Length; i++)
			{
				if (angularSize >= inversionRanges[i].Item1.x && angularSize < inversionRanges[i].Item1.y)
				{
					switch (inversionRanges[i].Item2)
					{
						case InversionMode.ConstantMedian:
							mutatedStep = (inversionRanges[i].Item1.x + inversionRanges[i].Item1.y) / 2f;
							break;
						case InversionMode.ConstantMin:
							mutatedStep = inversionRanges[i].Item1.x;
							break;
						case InversionMode.ConstantMax:
							mutatedStep = inversionRanges[i].Item1.y;
							break;
						case InversionMode.Inverted:
							mutatedStep = Mathf.LerpAngle(inversionRanges[i].Item1.y, inversionRanges[i].Item1.x, angularSize / (inversionRanges[i].Item1.y - inversionRanges[i].Item1.x));
							break;
						case InversionMode.NotModified:
						default:
							mutatedStep = step;
							break;
					}
					break;
				}
			}
			float delta;
			var offset = MutateInputRange_ApplyResets(out var inputOutputArray, mutatedStep/* * inputCorrectionRangeMultiplier*/, mid);
			delta = mutatedStep * inputRangeAmount/* * inputCorrectionRangeMultiplier*/;
			inputOutputArray[0 + offset] = new Vector2(mid - delta, xInputMin);
			inputOutputArray[1 + offset] = new Vector2(mid, mid);
			inputOutputArray[2 + offset] = new Vector2(mid + delta, xInputMax);
			if (doDebug) Debug.Log($"Angular Range: {xInputMin} - {xInputMax} ({xInputMax - xInputMin}) -> {inputOutputArray[0]} - {inputOutputArray[^1]} ({inputOutputArray[^1] - inputOutputArray[0]})");
			return inputOutputArray;
		}
	}
}