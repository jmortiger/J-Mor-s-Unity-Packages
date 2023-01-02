using System;
using System.Collections.Generic;
using UnityEngine;

namespace JMor.AimAssist
{
	[CreateAssetMenu(fileName = "orm_newNoResolutionMode", menuName = "Scriptable Object/Aim Assist/OverlapResolutionMode/NoResolutionMode")]
	public class NoResolution : OverlapResolutionMode
	{
		/// <summary>
		/// Mutates the given inputs, and DOES NOT resolve overlaps. Mainly for testing monotonicity.
		/// </summary>
		/// <param name="inputMinMaxes">An array, sorted with <see cref="CompareVector2ByX"/>, of angle ranges.</param>
		/// <param name="inputs"></param>
		/// <param name="outputs"></param>
		override public void MutateInputRanges(Vector2[] inputMinMaxes, out List<float> inputs, out List<float> outputs, Func<float, float, Vector2[]> mutator)
		{
			MutateInputRanges_Sparse(inputMinMaxes, out inputs, out outputs, mutator);

			// for (int i = 1; i < inputs.Count; i++)
			// {
			// 	if (inputs[i - 1] == inputs[i])
			// 	{
			// 		outputs[i - 1] = (outputs[i - 1] + outputs[i]) / 2f;
			// 		inputs.RemoveAt(i);
			// 		outputs.RemoveAt(i);
			// 		i--;
			// 	}
			// 	else if (inputs[i - 1] > inputs[i])
			// 	{
			// 		float iAccumulator = inputs[i], oAccumulator = outputs[i];
			// 		int currIndex = i - 1;
			// 		while (inputs[currIndex] > inputs[i] && currIndex >= 0)
			// 		{
			// 			iAccumulator += inputs[currIndex];
			// 			oAccumulator += outputs[currIndex];
			// 			currIndex--;
			// 		}
			// 		int numElements = i - currIndex;
			// 		currIndex++;
			// 		inputs[currIndex] = iAccumulator / numElements;
			// 		outputs[currIndex] = oAccumulator / numElements;
			// 		for (int t = currIndex; t < i; i--)
			// 		{
			// 			inputs.RemoveAt(i);
			// 			outputs.RemoveAt(i);
			// 		}
			// 	}
			// }

			MutateInputRanges_HandleLoopingRange(ref inputs, ref outputs);
		}
	}
}