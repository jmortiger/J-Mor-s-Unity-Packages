using System;
using UnityEngine;

namespace JMor.AimAssist
{
	[CreateAssetMenu(fileName = "imm_newInterpolatedMutatorMode", menuName = "Scriptable Object/InterpolatedMutatorMode")]
	public class InterpolatedMutatorMode : MutatorMode
	{
		[Header("Correction Settings")]
		[Tooltip("The amount of the total range of input correction to use for strong input correction.")]
		[Range(0, 1)]
		public float inputRangeAmount = .75f;
		[SerializeField]
		private Vector2[] desiredAngularSizePoints = new Vector2[]
		{
			new(-3, -3),
			new(-2, -2),
			new(-1, -1),
			new(0, 0),
			new(15, 30),
			new(30, 35),
			new(35, 40),
			new(40, 45),
			new(45, 45),
			new(90, 45),
			new(180, 45),
			new(270, 45),
		};
		public Vector2[] DesiredAngularSizePoints
		{
			get => desiredAngularSizePoints;
			set
			{
				cached_desiredAngularSizePoints = desiredAngularSizePoints = value;
				cachedInterpolator = Utility.MyMath.ConstructInterpolatorFunction(cached_desiredAngularSizePoints);
			}
		}
		private Func<float, float> cachedInterpolator;
		private Vector2[] cached_desiredAngularSizePoints;
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
			if (cached_desiredAngularSizePoints != desiredAngularSizePoints)
			{
				cached_desiredAngularSizePoints = desiredAngularSizePoints;
				cachedInterpolator = Utility.MyMath.ConstructInterpolatorFunction(cached_desiredAngularSizePoints);
			}
			var modifiedStep = cachedInterpolator(xInputMax - xInputMin) / 2f;
			var offset = MutateInputRange_ApplyResets(out var inputOutputArray, modifiedStep, mid);
			float delta = modifiedStep * inputRangeAmount;
			inputOutputArray[0 + offset] = new Vector2(mid - delta, xInputMin);
			inputOutputArray[1 + offset] = new Vector2(mid, mid);
			inputOutputArray[2 + offset] = new Vector2(mid + delta, xInputMax);
			if (doDebug) Debug.Log($"Angular Range: {xInputMin} - {xInputMax} ({xInputMax - xInputMin}) -> {inputOutputArray[0]} - {inputOutputArray[^1]} ({inputOutputArray[^1] - inputOutputArray[0]})");
			return inputOutputArray;
		}
	}
}