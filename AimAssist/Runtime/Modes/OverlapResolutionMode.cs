using System;
using System.Collections.Generic;
using UnityEngine;

namespace JMor.AimAssist
{
	//[CreateAssetMenu(fileName = "orm_newOverlapResolutionMode", menuName = "Scriptable Object/OverlapResolutionMode")]
	public abstract class OverlapResolutionMode : ScriptableObject
	{
		public abstract void MutateInputRanges(
			Vector2[] inputMinMaxes, 
			out List<float> inputs, 
			out List<float> outputs, 
			Func<float, float, Vector2[]> mutator);

		#region Helpers
		/// <summary>
		/// 
		/// </summary>
		/// <param name="inputMinMaxes">An array, sorted with <see cref="CompareVector2ByX"/>, of angle ranges.</param>
		/// <param name="inputs"></param>
		/// <param name="outputs"></param>
		public void MutateInputRanges(Vector2[] inputMinMaxes, out float[] inputs, out float[] outputs, Func<float, float, Vector2[]> mutator)
		{
			MutateInputRanges(inputMinMaxes, out List<float> inputsList, out List<float> outputsList, mutator);

			inputs = inputsList.ToArray();
			outputs = outputsList.ToArray();
		}

		/// <summary>
		/// Directly mutates. No adjustment for overlap.
		/// </summary>
		/// <param name="inputMinMaxes">An array, sorted with <see cref="CompareVector2ByX"/>, of angle ranges.</param>
		/// <param name="inputs"></param>
		/// <param name="outputs"></param>
		/// <remarks>Call at the start of custom <see cref="MutateInputRanges(Vector2[], out List{float}, out List{float}, Func{float, float, Vector2[]})"/>.</remarks>
		public void MutateInputRanges_Sparse(Vector2[] inputMinMaxes, out List<float> inputs, out List<float> outputs, Func<float, float, Vector2[]> mutator)
		{
			//Array.Sort(inputMinMaxes, new CompareVector2ByX());

			inputs = new List<float>();
			outputs = new List<float>();
			for (int i = 0; i < inputMinMaxes.Length; i++)
			{
				var mutated = mutator(inputMinMaxes[i].x, inputMinMaxes[i].y);
				for (int j = 0; j < mutated.Length; j++)
				{
					inputs.Add(mutated[j].x);
					outputs.Add(mutated[j].y);
				}
			}
		}

		// TODO: Figure out how many points on either side of a given point affect the curve at that point. Put here.
		const sbyte numPointsAffectingSplineAtPoint = 5;
		/// <summary>
		/// To prevent snapping around 0 & 360 degrees, you need to duplicate your final points with a + & - 360 degree offset. This function handles this.
		/// </summary>
		/// <param name="inputs"></param>
		/// <param name="outputs"></param>
		/// <remarks>Call at end of custom <see cref="MutateInputRanges(Vector2[], out List{float}, out List{float}, Func{float, float, Vector2[]})"/>.</remarks>
		public void MutateInputRanges_HandleLoopingRange(ref List<float> inputs, ref List<float> outputs)
		{
			var initialCount = inputs.Count;
			// Naive Approach: Duplicate the sets for the high and low ends. Higher complexity, more memory, works for sure.
			// for (int i = 0; i < initialCount; i++)
			// {
			// 	inputs.Add(inputs[i] - 360f);
			// 	outputs.Add(outputs[i] - 360f);
			// 	inputs.Add(inputs[i] + 360f);
			// 	outputs.Add(outputs[i] + 360f);
			// }
			// Conservative Approach: Copy only the points at the beginning and end
			var pointsToCopy = (numPointsAffectingSplineAtPoint - (numPointsAffectingSplineAtPoint % 2)) / 2;
			pointsToCopy = pointsToCopy > initialCount ? initialCount : pointsToCopy;
			for (int i = initialCount - pointsToCopy; i < initialCount; i++)
			{
				inputs.Add(inputs[i] - 360f);
				outputs.Add(outputs[i] - 360f);
			}
			for (int i = 0; i < pointsToCopy; i++)
			{
				inputs.Add(inputs[i] + 360f);
				outputs.Add(outputs[i] + 360f);
			}
		}
		#endregion
	}
}