using JMor.Utility;
using System;
using UnityEngine;

namespace JMor.AimAssist
{
	// TODO: Slim comments w/ inheritdoc
	[CreateAssetMenu(fileName = "pid_newController", menuName = "Scriptable Object/PID Controller")]
	public class PIDControllerObj : ScriptableObject
	{
		#region State
		private float priorError = 0;
		private float accumulatedError = 0;
		public float AccumulatedError => accumulatedError;
		private float priorEstimateForDerivative = 0;
		private float[] priorErrors = new float[3];
		#endregion
		#region Settings
		public float gainProportional = 1;
		public float gainIntegral = 1;
		public float gainDerivative = 1;
		public float maxAccumulatedError = float.PositiveInfinity;
		public float minAccumulatedError = float.NegativeInfinity;
		/// <summary>
		/// A value of 0 means there is no filtering of instantaneous error (jittery inputs are preserved); A value of 1 means there is no reaction to instantaneous error.
		/// </summary>
		[Range(0, 1f - UnityEngine.Vector2.kEpsilon)]// [Range(0, 1f)]
		[SerializeField]
		private float derivativeFilterPercentage = 0;
		#endregion
		#region Properties
		/// <summary>
		/// A value clamped from 0 to 1 that controls the low pass filter for the derivative.
		/// As the value approaches 0, current instantaneous error change rate has a greater impact than prior instantaneous error change rate.
		/// As the value approaches 1, current instantaneous error change rate has a lesser impact than prior instantaneous error change rate.
		/// A value of 0 means there is no derivative filtering of instantaneous error (jittery inputs are preserved).
		/// A value of 1 means there is no derivative reaction to instantaneous error.
		/// </summary>
		/// <remarks>
		/// If the desired behaviour is for the derivative to not affect the output, achieve it by setting <see cref="gainDerivative"/> to zero instead of setting this to 1.
		/// Changing DerivativeFilterPercentage to 1 means any stored change will be continuously applied, easily leading to drastic and unpredictable behaviour.
		/// Any attempts to set this value to 1 will be circumvented.
		/// </remarks>
		public float DerivativeFilterPercentage
		{
			get => derivativeFilterPercentage;
			set => derivativeFilterPercentage = Mathf.Abs((value >= 1f && value < 100f) || (value <= -1f && value > -100f) ? value / 100f : value % 1f);
			// set {
			// 	value >= 1f && value < 100f ? value / 100f : Mathf.Clamp01(value);
			// 	if (derivativeFilterPercentage == 1f)
			// 		derivativeFilterPercentage = 0f;
			// }
		}
		public float TimeDerivative
		{
			get => gainDerivative / gainProportional;
			set => gainDerivative = gainProportional * value;
		}
		public float TimeIntegral
		{
			get => gainProportional / gainIntegral;
			set => gainIntegral = gainProportional / value;
		}
		// public float GainDerivative {
		// 	get => gainProportional * timeDerivative;
		// 	set => timeDerivative = value / gainProportional;
		// }
		// public float GainIntegral {
		// 	get => gainProportional / timeIntegral;
		// 	set => timeIntegral = gainProportional / value;
		// }
		public bool IsDerivativeFiltered => derivativeFilterPercentage != 0;
		public bool IsDerivativeApplied => gainDerivative != 0;
		public bool IsIntegralApplied => gainIntegral != 0;
		#endregion
		#region Ctors
		public PIDControllerObj(
			float gainProportional = 1f,
			float gainIntegral = 1f,
			float gainDerivative = 1f,
			float maxAccumulatedError = float.PositiveInfinity,
			float minAccumulatedError = float.NaN,
			float derivativeFilterPercentage = .8f)
		{
			this.gainProportional = gainProportional;
			this.gainIntegral = gainIntegral;
			this.gainDerivative = gainDerivative;
			this.maxAccumulatedError = maxAccumulatedError;
			this.minAccumulatedError = float.IsNaN(minAccumulatedError) ? -maxAccumulatedError : minAccumulatedError;
			// this.derivativeFilterPercentage	= derivativeFilterPercentage >= 1f && derivativeFilterPercentage <= 100f ? derivativeFilterPercentage / 100f : Mathf.Clamp01(derivativeFilterPercentage);
			this.derivativeFilterPercentage = Mathf.Abs((derivativeFilterPercentage >= 1f && derivativeFilterPercentage < 100f) || (derivativeFilterPercentage <= -1f && derivativeFilterPercentage > -100f) ? derivativeFilterPercentage / 100f : derivativeFilterPercentage % 1f);
			if (this.minAccumulatedError > this.maxAccumulatedError)
			{
				var temp = this.maxAccumulatedError;
				this.maxAccumulatedError = this.minAccumulatedError;
				this.minAccumulatedError = temp;
			}
			this.priorError = this.accumulatedError = this.priorEstimateForDerivative = 0f;
			this.priorErrors = new float[3];
		}
		public PIDControllerObj(
			PIDControllerObj source,
			float priorError = float.NaN,
			float accumulatedError = float.NaN,
			float priorEstimateForDerivative = float.NaN,
			float gainProportional = float.NaN,
			float gainIntegral = float.NaN,
			float gainDerivative = float.NaN,
			float maxAccumulatedError = float.NaN,
			float minAccumulatedError = float.NaN,
			float derivativeFilterPercentage = float.NaN)
		{
			this.priorError = float.IsNaN(priorError) ? source.priorError : priorError;
			this.accumulatedError = float.IsNaN(accumulatedError) ? source.accumulatedError : accumulatedError;
			this.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? source.priorEstimateForDerivative : priorEstimateForDerivative;
			this.priorErrors = new float[3];
			this.gainProportional = float.IsNaN(gainProportional) ? source.gainProportional : gainProportional;
			this.gainIntegral = float.IsNaN(gainIntegral) ? source.gainIntegral : gainIntegral;
			this.gainDerivative = float.IsNaN(gainDerivative) ? source.gainDerivative : gainDerivative;
			this.maxAccumulatedError = float.IsNaN(maxAccumulatedError) ? source.maxAccumulatedError : maxAccumulatedError;
			this.minAccumulatedError = float.IsNaN(minAccumulatedError) ? source.minAccumulatedError : minAccumulatedError;
			this.derivativeFilterPercentage = float.IsNaN(derivativeFilterPercentage) ? source.derivativeFilterPercentage : derivativeFilterPercentage;
		}
		public static void ReassignPIDController(
			ref PIDControllerObj source,
			float priorError = float.NaN,
			float accumulatedError = float.NaN,
			float priorEstimateForDerivative = float.NaN,
			float[] priorErrors = null,
			float gainProportional = float.NaN,
			float gainIntegral = float.NaN,
			float gainDerivative = float.NaN,
			float maxAccumulatedError = float.NaN,
			float minAccumulatedError = float.NaN,
			float derivativeFilterPercentage = float.NaN)
		{
			source.priorError = float.IsNaN(priorError) ? source.priorError : priorError;
			source.accumulatedError = float.IsNaN(accumulatedError) ? source.accumulatedError : accumulatedError;
			source.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? source.priorEstimateForDerivative : priorEstimateForDerivative;
			source.priorErrors = priorErrors ?? source.priorErrors;
			source.gainProportional = float.IsNaN(gainProportional) ? source.gainProportional : gainProportional;
			source.gainIntegral = float.IsNaN(gainIntegral) ? source.gainIntegral : gainIntegral;
			source.gainDerivative = float.IsNaN(gainDerivative) ? source.gainDerivative : gainDerivative;
			source.maxAccumulatedError = float.IsNaN(maxAccumulatedError) ? source.maxAccumulatedError : maxAccumulatedError;
			source.minAccumulatedError = float.IsNaN(minAccumulatedError) ? source.minAccumulatedError : minAccumulatedError;
			source.derivativeFilterPercentage = float.IsNaN(derivativeFilterPercentage) ? source.derivativeFilterPercentage : derivativeFilterPercentage;
		}
		#endregion
		void Reset()
		{
			priorError					 = 0f;
			accumulatedError			 = 0f;
			priorEstimateForDerivative	 = 0f;
			priorErrors					 = new float[3] { 0f, 0f, 0f };
			gainProportional			 = 1f;
			gainIntegral				 = 1f;
			gainDerivative				 = 1f;
			maxAccumulatedError			 = float.PositiveInfinity;
			minAccumulatedError			 = float.NegativeInfinity;
			derivativeFilterPercentage	 = 0f;
		}
		[ContextMenu("Reset Controller State")]
		public void ResetControllerState(
			float priorError = float.NaN,
			float accumulatedError = float.NaN,
			float priorEstimateForDerivative = float.NaN,
			float[] priorErrors = null)
		{
			this.priorError = float.IsNaN(priorError) ? 0f : priorError;
			this.accumulatedError = float.IsNaN(accumulatedError) ? 0f : accumulatedError;
			this.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? 0f : priorEstimateForDerivative;
			this.priorErrors = priorErrors ?? new float[3] { 0, 0, 0 };
		}
		// TODO: Isn't really dependant on Ideal form from caller's perspective, refactor.
		public float CalculateIdealForm(
			float currentValue,
			float desiredValue,
			float dt,
			Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null,
			bool useDiscreteVelocityFormForIntegral = false,
			bool correctTimeScaleForIntegral = true,
			float timeScalerForIntegral = 1000f,
			bool correctTimeScaleForDerivative = true,
			float timeScalerForDerivative = 1000f,
			bool useAlternateDerivativeForm = false)
		{
			var currentError = GetIdealFormTermP(currentValue, desiredValue);
			if (valueAdjuster != null)
				(priorErrors[0], priorErrors[1], priorErrors[2], _) = valueAdjuster(currentError, priorErrors[0], priorErrors[1], priorErrors[2]);
			var finalIntegral = useDiscreteVelocityFormForIntegral ?
				GetIdealFormTermI(dt, priorErrors[0], 0, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral) :
				GetIdealFormTermI(dt, priorErrors[0], ref accumulatedError, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral);
			var finalDerivative = GetIdealFormTermD(dt, priorErrors[0], priorErrors[1], priorErrors[2], ref priorEstimateForDerivative, gainDerivative, derivativeFilterPercentage, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm);
			return gainProportional * currentError + finalIntegral + finalDerivative;
		}
		public float CalculateIdealForm_DEBUG(
			float currentValue,
			float desiredValue,
			float dt,
			Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null,
			bool useDiscreteVelocityFormForIntegral = false,
			bool correctTimeScaleForIntegral = true,
			float timeScalerForIntegral = 1000f,
			bool correctTimeScaleForDerivative = true,
			float timeScalerForDerivative = 1000f,
			bool useAlternateDerivativeForm = false,
			bool debugDownstream = true)
		{
			var currentError = GetIdealFormTermP(currentValue, desiredValue);
			Debug.Log($"currentError: {currentError}");
			Debug.Log($"priorErrors: [{currentError}, {priorErrors[0]}, {priorErrors[1]}, {priorErrors[2]}]");
			if (valueAdjuster != null)
				(priorErrors[0], priorErrors[1], priorErrors[2], _) = valueAdjuster(currentError, priorErrors[0], priorErrors[1], priorErrors[2]);
			Debug.Log($"Adjusted priorErrors: [{priorErrors[0]}, {priorErrors[1]}, {priorErrors[2]}]");
			Debug.Log($"useDiscreteVelocityFormForIntegral: {useDiscreteVelocityFormForIntegral}");
			Debug.Log($"prior accumulatedError: {accumulatedError}");
			var finalIntegral = useDiscreteVelocityFormForIntegral ?
				GetIdealFormTermI(dt, priorErrors[0], 0, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral) :
				(debugDownstream ? GetIdealFormTermI_DEBUG(dt, priorErrors[0], ref accumulatedError, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral) : GetIdealFormTermI(dt, priorErrors[0], ref accumulatedError, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral));
			Debug.Log($"new accumulatedError: {accumulatedError}");
			Debug.Log($"finalIntegral: {finalIntegral}");
			Debug.Log($"prior priorEstimateForDerivative: {priorEstimateForDerivative}");
			var finalDerivative = debugDownstream ? GetIdealFormTermD_DEBUG(dt, priorErrors[0], priorErrors[1], priorErrors[2], ref priorEstimateForDerivative, gainDerivative, derivativeFilterPercentage, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm) : GetIdealFormTermD(dt, priorErrors[0], priorErrors[1], priorErrors[2], ref priorEstimateForDerivative, gainDerivative, derivativeFilterPercentage, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm);
			Debug.Log($"new priorEstimateForDerivative: {priorEstimateForDerivative}");
			Debug.Log($"finalDerivative: {finalDerivative}");
			return gainProportional * currentError + finalIntegral + finalDerivative;
		}
		// public static CalculateIdealForm(
		// 	float currentValue, 
		// 	float desiredValue, 
		// 	float dt, 
		// 	Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null, 
		// 	bool useDiscreteVelocityFormForIntegral = false,
		// 	bool correctTimeScaleForIntegral = true,
		// 	float timeScalerForIntegral = 1000f,
		// 	bool correctTimeScaleForDerivative = true,
		// 	float timeScalerForDerivative = 1000f,
		// 	bool useAlternateDerivativeForm = false)
		// {
		// 	var currentError = GetIdealFormTermP(currentValue, desiredValue);
		// 	if (valueAdjuster != null)
		// 		(priorErrors[0], priorErrors[1], priorErrors[2], _) = valueAdjuster(currentError, priorErrors[0], priorErrors[1], priorErrors[2]);
		// 	var finalIntegral = useDiscreteVelocityFormForIntegral ? 
		// 		GetIdealFormTermI(dt, priorErrors[0], 0, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral) : 
		// 		GetIdealFormTermI(dt, priorErrors[0], ref accumulatedError, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral);
		// 	var finalDerivative = GetIdealFormTermD(dt, priorErrors[0], priorErrors[1], priorErrors[2], ref priorEstimateForDerivative, gainDerivative, derivativeFilterPercentage, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm);
		// 	return gainProportional * currentError + finalIntegral + finalDerivative;
		// }
		#region Get Ideal Form Components
		private static float GetIdealFormTermP(
			float currentValue,
			float desiredValue,
			Func<float, float> valueAdjuster = null,
			float gainProportional = 1f)
		{
			var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
			MyMath.Validate(ref currentError);
			return gainProportional * currentError;
		}
		#region GetIdealFormTermI
		/// <summary>Solves 𝑲ᵢ*₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ</summary>
		/// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		/// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		/// <param name="accumulatedError">Equivalent to ₀∫ᵗ𝓮(𝒂)𝒅𝒂 where 𝒂 = 𝒕ₙ-Δ𝒕. To emulate a discrete velocity integral, pass 0.</param>
		/// <param name="gainIntegral">Equivalent to 𝑲ᵢ. If the base term is desired (e.g. to store the accumulated error), a value of 1 will result in the base value.</param>
		/// <param name="minAccumulatedError">The minimmum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the lower end, a value of -Infinity will result in the unaltered value.</param>
		/// <param name="maxAccumulatedError">The maximmum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the upper end, a value of +Infinity will result in the unaltered value.</param>
		/// <param name="assumeTimeLessThan1"><paramref name="dt"/> appears to need to be equal to, or greater than one. Assuming <paramref name="dt"/> is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". Theoretically, this could affect integral as well. To resolve this, <paramref name="dt"/> is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, <paramref name="errorAtTMinus0"/> is divided (instead of multiplied) by <paramref name="dt"/>. If this behaviour is undesireable, pass in false to disable it.</param>
		/// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of <paramref name="dt"/> far less than 1. If equal to NaN || +/-Infinity, <paramref name="dt"/> is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		/// <returns></returns>
		private static float GetIdealFormTermI(
			float dt,
			float errorAtTMinus0,
			float accumulatedError,
			// Func<float, float> valueAdjuster = null,
			float gainIntegral = 1f,
			float minAccumulatedError = float.NegativeInfinity,
			float maxAccumulatedError = float.PositiveInfinity,
			bool assumeTimeLessThan1 = false,
			float scaleDTBy = 1000f)
		{
			var integral = accumulatedError;// + errorAtTMinus0 * dt * (assumeTimeLessThan1 ? 1000f : 1f);
			if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
			if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) integral += errorAtTMinus0 / dt;
			else integral += errorAtTMinus0 * dt;
			MyMath.Validate(ref integral);
			integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
			return gainIntegral * integral;
		}
		/// <summary>
		/// <inheritdoc cref="GetIdealFormTermI(float, float, float, float, float, float, bool, float)"/> while updating relevant fields
		/// </summary>
		/// <param name="accumulatedError">Equivalent to ₀∫ᵗ𝓮(𝒂)𝒅𝒂 where 𝒂 = 𝒕ₙ-Δ𝒕; will be updated to curr+=<paramref name="errorAtTMinus0"/> * dt * (<paramref name="assumeTimeLessThan1"/> ? <paramref name="scaleDTBy"/> : 1f)</param>
        /// <inheritdoc cref="GetIdealFormTermI(float, float, float, float, float, float, bool, float)"/>
		private static float GetIdealFormTermI(
			float dt,
			float errorAtTMinus0,
			ref float accumulatedError,
			// Func<float, float> valueAdjuster = null,
			float gainIntegral = 1f,
			float minAccumulatedError = float.NegativeInfinity,
			float maxAccumulatedError = float.PositiveInfinity,
			bool assumeTimeLessThan1 = false,
			float scaleDTBy = 1000f)
		{
			// accumulatedError += errorAtTMinus0 * dt * (assumeTimeLessThan1 ? 1000f : 1f);
			if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
			if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) accumulatedError += errorAtTMinus0 / dt;
			else accumulatedError += errorAtTMinus0 * dt;
			MyMath.Validate(ref accumulatedError);
			accumulatedError = Mathf.Clamp(accumulatedError, minAccumulatedError, maxAccumulatedError);
			return gainIntegral * accumulatedError;
		}
		/// <summary>
        /// <inheritdoc cref="GetIdealFormTermI(float, float, float, float, float, float, bool, float)"/> while logging debug statements
        /// </summary>
        /// <inheritdoc cref="GetIdealFormTermI(float, float, float, float, float, float, bool, float)"/>
		private static float GetIdealFormTermI_DEBUG(
			float dt,
			float errorAtTMinus0,
			float accumulatedError,
			// Func<float, float> valueAdjuster = null,
			float gainIntegral = 1f,
			float minAccumulatedError = float.NegativeInfinity,
			float maxAccumulatedError = float.PositiveInfinity,
			bool assumeTimeLessThan1 = false,
			float scaleDTBy = 1000f)
		{
			var integral = accumulatedError;// + errorAtTMinus0 * dt * (assumeTimeLessThan1 ? 1000f : 1f);
			if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
			if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) integral += errorAtTMinus0 / dt;
			else integral += errorAtTMinus0 * dt;
			MyMath.Validate(ref integral);
			integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
			return gainIntegral * integral;
		}
		/// <summary>
		/// <inheritdoc cref="GetIdealFormTermI(float, float, ref float, float, float, float, bool, float)"/> and logging debug statements.
		/// </summary>
        /// <inheritdoc cref="GetIdealFormTermI(float, float, ref float, float, float, float, bool, float)"/>
		private static float GetIdealFormTermI_DEBUG(
			float dt,
			float errorAtTMinus0,
			ref float accumulatedError,
			// Func<float, float> valueAdjuster = null,
			float gainIntegral = 1f,
			float minAccumulatedError = float.NegativeInfinity,
			float maxAccumulatedError = float.PositiveInfinity,
			bool assumeTimeLessThan1 = false,
			float scaleDTBy = 1000f)
		{
			// accumulatedError += errorAtTMinus0 * dt * (assumeTimeLessThan1 ? 1000f : 1f);
			Debug.Log($"GetIdealFormTermI_DEBUG");
			Debug.Log($"\taccumulatedError:{accumulatedError}; dt:{dt}");
			if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
			Debug.Log($"\taccumulatedError:{accumulatedError}; dt:{dt}");
			if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) accumulatedError += errorAtTMinus0 / dt;
			else accumulatedError += errorAtTMinus0 * dt;
			Debug.Log($"\taccumulatedError:{accumulatedError}");
			MyMath.Validate(ref accumulatedError);
			Debug.Log($"\taccumulatedError:{accumulatedError}");
			accumulatedError = Mathf.Clamp(accumulatedError, minAccumulatedError, maxAccumulatedError);
			Debug.Log($"\taccumulatedError:{accumulatedError}");
			Debug.Log($"Return value:{gainIntegral * accumulatedError}");
			return gainIntegral * accumulatedError;
		}
		#endregion
		#region GetIdealFormTermD
		/// <summary>Solves* 𝑲d*(𝒅𝓮(𝒕ₙ))/(𝒅𝒕ₙ)</summary>
		/// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		/// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		/// <param name="errorAtTMinus1">Equivalent to 𝓮(𝒕ₙ₋₁); the prior error</param>
		/// <param name="errorAtTMinus2">Equivalent to 𝓮(𝒕ₙ₋₂); the error before last</param>
		/// <param name="priorEstimateForDerivative">Equivalent to (𝒅𝓮(𝒕ₙ₋₁))/(𝒅𝒕ₙ₋₁) filtered for high frequency noise. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		/// <param name="gainDerivative">Equivalent to 𝑲d. If the base term is desired (i.e. to store the filtered derivative for filtering), a value of 1 will result in the base value.</param>
		/// <param name="derivativeFilterPercentage">The percentage for filtering derivative noise. Corrosponds to <see cref="PIDControllerObj.DerivativeFilterPercentage"/>. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		/// <param name="assumeTimeLessThan1">dt appears to need to be equal to, or greater than one. Assuming dt is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". To resolve this, dt is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. If this behaviour is undesireable, pass in false to disable it.</param>
		/// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of dt far less than 1. If equal to NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		/// <param name="useAlternateForm">This implementation was derived from the Wikipedia entry on PID Controllers. However, it appears as though their psuedocode incorrectly calculates this term (the correct form seems to be 𝑲d * (𝓮(𝒕ₙ) - 2*𝓮(𝒕ₙ₋₁) + 𝓮(𝒕ₙ₋₂)) / Δ𝒕, but their corrosponding psudeocode uses 𝑲d * 𝓮(𝒕ₙ) − 𝓮(𝒕ₙ₋₁)) / Δ𝒕. To use this (assumedly) incorrect calculation, pass in true. Note that this will render <paramref name="errorAtTMinus2"/> useless.</param>
		/// <returns></returns>
		private static float GetIdealFormTermD(
			float dt,
			float errorAtTMinus0,
			float errorAtTMinus1,
			float errorAtTMinus2,
			float priorEstimateForDerivative = 0f,
			float gainDerivative = 1f,
			float derivativeFilterPercentage = 0f,
			bool assumeTimeLessThan1 = true,
			float scaleDTBy = 1000f,
			bool useAlternateForm = false)
		{
			// var derivative = useAlternateForm ? (errorAtTMinus0 - errorAtTMinus1) / dt : (errorAtTMinus0 - 2f * errorAtTMinus1 + errorAtTMinus2) / dt;
			var derivative = (errorAtTMinus0 - (useAlternateForm ? errorAtTMinus1 : 2f * errorAtTMinus1 - errorAtTMinus2));
			if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
			if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) derivative *= dt;
			else derivative /= dt;
			MyMath.Validate(ref derivative);
			var filteredDerivative = derivativeFilterPercentage * priorEstimateForDerivative + (1f - derivativeFilterPercentage) * derivative;
			MyMath.Validate(ref filteredDerivative);
			return gainDerivative * filteredDerivative;
		}
        // TODO: Troubleshoot param inheritance below.
		/// <summary>
		/// <inheritdoc cref="GetIdealFormTermD(float, float, float, float, float, float, float, bool, float, bool)"/> while updating relevant terms
		/// </summary>
		/// <param name="priorEstimateForDerivative"><inheritdoc cref="GetIdealFormTermD(float, float, float, float, float, float, float, bool, float, bool)"/> Will be updated.</param>
		/// <inheritdoc cref="GetIdealFormTermD(float, float, float, float, float, float, float, bool, float, bool)"/>
		private static float GetIdealFormTermD(
			float dt,
			float errorAtTMinus0,
			float errorAtTMinus1,
			float errorAtTMinus2,
			ref float priorEstimateForDerivative,
			float gainDerivative = 1f,
			float derivativeFilterPercentage = 0f,
			bool assumeTimeLessThan1 = true,
			float scaleDTBy = 1000f,
			bool useAlternateForm = false)
		{
			// var derivative = useAlternateForm ? (errorAtTMinus0 - errorAtTMinus1) / dt : (errorAtTMinus0 - 2f * errorAtTMinus1 + errorAtTMinus2) / dt;
			var derivative = (errorAtTMinus0 - (useAlternateForm ? errorAtTMinus1 : 2f * errorAtTMinus1 - errorAtTMinus2));
			if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
			if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) derivative *= dt;
			else derivative /= dt;
			MyMath.Validate(ref derivative);
			priorEstimateForDerivative = derivativeFilterPercentage * priorEstimateForDerivative.Validate() + (1f - derivativeFilterPercentage) * derivative;
			MyMath.Validate(ref priorEstimateForDerivative);
			return gainDerivative * priorEstimateForDerivative;
		}
        /// <summary>
        /// <inheritdoc cref="GetIdealFormTermD(float, float, float, float, ref float, float, float, bool, float, bool)"/> and logging debug statements.
        /// </summary>
        /// <inheritdoc cref="GetIdealFormTermD(float, float, float, float, ref float, float, float, bool, float, bool)"/>
		private static float GetIdealFormTermD_DEBUG(
			float dt,
			float errorAtTMinus0,
			float errorAtTMinus1,
			float errorAtTMinus2,
			ref float priorEstimateForDerivative,
			float gainDerivative = 1f,
			float derivativeFilterPercentage = 0f,
			bool assumeTimeLessThan1 = true,
			float scaleDTBy = 1000f,
			bool useAlternateForm = false)
		{
			Debug.Log($"GetIdealFormTermD_DEBUG");
			var derivative = (errorAtTMinus0 - (useAlternateForm ? errorAtTMinus1 : 2f * errorAtTMinus1 - errorAtTMinus2));
			Debug.Log($"\tderivative:{derivative}; dt:{dt}");
			if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
			Debug.Log($"\tderivative:{derivative}; dt:{dt}");
			if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) derivative *= dt;
			else derivative /= dt;
			Debug.Log($"\tderivative:{derivative}; dt:{dt}");
			MyMath.Validate(ref derivative);
			Debug.Log($"\tderivative:{derivative}; dt:{dt}");
			Debug.Log($"\told priorEstimateForDerivative:{priorEstimateForDerivative}");
			priorEstimateForDerivative = derivativeFilterPercentage * priorEstimateForDerivative.Validate() + (1f - derivativeFilterPercentage) * derivative;
			Debug.Log($"\tnew priorEstimateForDerivative:{priorEstimateForDerivative}");
			MyMath.Validate(ref priorEstimateForDerivative);
			Debug.Log($"\t validated priorEstimateForDerivative:{priorEstimateForDerivative}");
			Debug.Log($"Return value:{gainDerivative * priorEstimateForDerivative}");
			return gainDerivative * priorEstimateForDerivative;
		}
		#endregion
		#endregion
		#region Convert Between Gains And Time
		public static float SolveForGainDerivative(float gainProportional, float timeDerivative) => gainProportional * timeDerivative;
		public static float SolveForTimeDerivative(float gainProportional, float gainDerivative) => gainDerivative / gainProportional;
		public static float SolveForGainIntegral(float gainProportional, float timeIntegral) => gainProportional / timeIntegral;
		public static float SolveForTimeIntegral(float gainProportional, float gainIntegral) => gainProportional / gainIntegral;
		#endregion
		// TODO: Obsolete, parse through and trim waaaaay down all below.
		#region Old Calculate Forms
		[Obsolete("Use CalculateIdealForm instead.", true)]
		public float Calculate_DEBUG(float currentValue, float desiredValue, float dt, Func<float, float> valueAdjuster = null, bool updateMembers = true)
		{
			var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
			Debug.Log($"currentError: {currentError}");
			if (!MyMath.Validate(ref currentError))
				Debug.Log($"Corrected currentError: {currentError}");
			var integral = accumulatedError + currentError * dt;
			Debug.Log($"integral: {integral}");
			MyMath.Validate(ref integral);
			integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
			Debug.Log($"Corrected integral: {integral}");
			var derivative = (currentError - 2f * priorError + priorErrors[1]) / dt;// var derivative = (currentError - priorError) / dt;
			Debug.Log($"derivative: {derivative}");
			MyMath.Validate(ref derivative);
			// derivative = Mathf.Clamp(derivative, -500f, 500f);
			Debug.Log($"Corrected derivative: {derivative}");
			var filteredDerivative = (derivativeFilterPercentage * priorEstimateForDerivative) + (1f - derivativeFilterPercentage) * derivative;
			Debug.Log($"filteredDerivative: {filteredDerivative}");
			MyMath.Validate(ref filteredDerivative);
			Debug.Log($"Corrected filteredDerivative: {filteredDerivative}");
			Debug.Log($"priorError: {priorError}");
			Debug.Log($"priorIntegralComponent: {accumulatedError}");
			if (updateMembers)
			{
				priorErrors[2] = priorErrors[1];
				priorErrors[1] = priorErrors[0];
				priorErrors[0] = currentError;
				priorError = currentError;
				accumulatedError = integral;
				priorEstimateForDerivative = filteredDerivative;
			}
			return MyMath.Wrap(gainProportional * currentError + gainIntegral * integral + gainDerivative * filteredDerivative, 0f, 360f);// TODO: Wrapping is for aim assist, but it shouldn't be done here, at least not statically. It should be changed to be implementation-agnostic. Maybe use the same solution as the error adjustment?
		}
		[Obsolete("Use CalculateIdealForm instead.", true)]
		public float Calculate(float currentValue, float desiredValue, float dt, Func<float, float> valueAdjuster = null, bool updateMembers = true)
		{
			var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
			MyMath.Validate(ref currentError);
			var integral = accumulatedError + currentError * dt;
			MyMath.Validate(ref integral);
			integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
			var derivative = (currentError - 2f * priorError + priorErrors[1]) / dt;// var derivative = (currentError - priorError) / dt;
			MyMath.Validate(ref derivative);
			// derivative = Mathf.Clamp(derivative, -500f, 500f);
			var filteredDerivative = (derivativeFilterPercentage * priorEstimateForDerivative) + (1f - derivativeFilterPercentage) * derivative;
			MyMath.Validate(ref filteredDerivative);
			if (updateMembers)
			{
				priorErrors[2] = priorErrors[1];
				priorErrors[1] = priorErrors[0];
				priorErrors[0] = currentError;
				priorError = currentError;
				accumulatedError = integral;
				priorEstimateForDerivative = filteredDerivative;
			}
			return MyMath.Wrap(gainProportional * currentError + gainIntegral * integral + gainDerivative * filteredDerivative, 0f, 360f);// TODO: Wrapping is for aim assist, but it shouldn't be done here, at least not statically. It should be changed to be implementation-agnostic. Maybe use the same solution as the error adjustment?
		}
		[Obsolete("Use CalculateIdealForm instead.", true)]
		public static float Calculate(
			float currentValue,
			float desiredValue,
			float dt,
			float gainProportional,
			float gainIntegral,
			float gainDerivative,
			ref float errorAtTMinus0,
			ref float errorAtTMinus1,
			ref float errorAtTMinus2,
			ref float accumulatedError,
			ref float priorEstimateForDerivative,
			float derivativeFilterPercentage = 0f,
			float minAccumulatedError = float.NegativeInfinity,
			float maxAccumulatedError = float.PositiveInfinity,
			Func<float, float> valueAdjuster = null)
		{
			var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
			MyMath.Validate(ref currentError);
			errorAtTMinus2 = errorAtTMinus1;
			errorAtTMinus1 = errorAtTMinus0;
			errorAtTMinus0 = currentError;
			var integral = accumulatedError + currentError * dt;
			MyMath.Validate(ref integral);
			integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
			accumulatedError = integral;
			var derivative = (currentError - 2f * errorAtTMinus0 + errorAtTMinus1) / dt;// var derivative = (currentError - priorError) / dt;
			MyMath.Validate(ref derivative);
			// derivative = Mathf.Clamp(derivative, -500f, 500f);
			var filteredDerivative = (derivativeFilterPercentage * priorEstimateForDerivative) + (1f - derivativeFilterPercentage) * derivative;
			MyMath.Validate(ref filteredDerivative);
			priorEstimateForDerivative = filteredDerivative;
			return gainProportional * currentError + gainIntegral * integral + gainDerivative * filteredDerivative;
		}
		[Obsolete("Use CalculateIdealForm instead.", true)]
		public float Calculate2(float currentValue, float desiredValue, float dt, Func<float, float> valueAdjuster = null)
		{
			var currentError = /*valueAdjuster?.Invoke(desiredValue - currentValue) ?? */desiredValue - currentValue;
			MyMath.Validate(ref currentError);
			priorErrors[2] = priorErrors[1];
			priorErrors[1] = priorErrors[0];
			priorErrors[0] = currentError;
			// return Wrap(GetStandardForm_Term1(dt) * priorErrors[0] + GetStandardForm_Term2(dt) * priorErrors[1] + GetStandardForm_Term3(dt) * priorErrors[2], 0f, 360f);// TODO: Wrapping is for aim assist, but it shouldn't be done here, at least not statically. It should be changed to be implementation-agnostic. Maybe use the same solution as the error adjustment?
			return GetDiscreteForm_Term1(dt) * priorErrors[0] + GetDiscreteForm_Term2(dt) * priorErrors[1] + GetDiscreteForm_Term3(dt) * priorErrors[2];
		}
		[Obsolete("Use CalculateIdealForm instead.", true)]
		public float Calculate2_DEBUG(float currentValue, float desiredValue, float dt, Func<float, float> valueAdjuster = null)
		{
			Debug.Log($"currentError: {desiredValue - currentValue}");
			var currentError = /*valueAdjuster?.Invoke(desiredValue - currentValue) ?? */desiredValue - currentValue;
			Debug.Log($"currentErrorAdjusted: {currentError}");
			MyMath.Validate(ref currentError);
			Debug.Log($"Corrected currentError: {currentError}");
			priorErrors[2] = priorErrors[1];
			priorErrors[1] = priorErrors[0];
			priorErrors[0] = currentError;
			Debug.Log($"priorErrors: [{priorErrors[0]}, {priorErrors[1]}, {priorErrors[2]}]");
			// return Wrap(GetStandardForm_Term1(dt) * priorErrors[0] + GetStandardForm_Term2(dt) * priorErrors[1] + GetStandardForm_Term3(dt) * priorErrors[2], 0f, 360f);// TODO: Wrapping is for aim assist, but it shouldn't be done here, at least not statically. It should be changed to be implementation-agnostic. Maybe use the same solution as the error adjustment?
			Debug.Log($"{GetDiscreteForm_Term1(dt)} * {priorErrors[0]} + {GetDiscreteForm_Term2(dt)} * {priorErrors[1]} + {GetDiscreteForm_Term3(dt)} * {priorErrors[2]}");
			return GetDiscreteForm_Term1(dt) * priorErrors[0] + GetDiscreteForm_Term2(dt) * priorErrors[1] + GetDiscreteForm_Term3(dt) * priorErrors[2];
		}
		#region GetDiscreteForm
		private float GetDiscreteForm_Term1(float dt) => gainProportional + gainIntegral * dt + gainDerivative / dt;
		private float GetDiscreteForm_Term2(float dt) => -gainProportional - 2f * gainDerivative / dt;
		private float GetDiscreteForm_Term3(float dt) => gainDerivative / dt;
		#endregion
		[Obsolete("Use CalculateIdealForm instead.", true)]
		public static float CalculateStandardForm(float currentValue, float desiredValue, float dt, float timeIntegral, float timeDerivative, float gainProportional, ref float errorAtTMinus0, ref float errorAtTMinus1, ref float errorAtTMinus2, Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null, bool updateMembers = true)
		{
			var currentError = desiredValue - currentValue;
			MyMath.Validate(ref currentError);
			var output =
					GetStandardForm_Term1(dt, timeIntegral, timeDerivative, gainProportional, currentError) +
					GetStandardForm_Term2(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus0) +
					GetStandardForm_Term3(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus1);
			if (updateMembers)
			{
				errorAtTMinus2 = errorAtTMinus1;
				errorAtTMinus1 = errorAtTMinus0;
				errorAtTMinus0 = currentError;
			}
			return output;
		}
		[Obsolete("Use CalculateIdealForm instead.", true)]
		public static float CalculateStandardForm_DEBUG(float currentValue, float desiredValue, float dt, float timeIntegral, float timeDerivative, float gainProportional, ref float errorAtTMinus0, ref float errorAtTMinus1, ref float errorAtTMinus2, Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null, bool updateMembers = true)
		{
			var currentError = desiredValue - currentValue;
			Debug.Log($"currentError: {currentError}");
			MyMath.Validate(ref currentError);
			Debug.Log($"Corrected currentError: {currentError}");
			Debug.Log($"current & priorErrors: {currentError} & [{errorAtTMinus0}, {errorAtTMinus1}, {errorAtTMinus2}]");
			(currentError, errorAtTMinus0, errorAtTMinus1, errorAtTMinus2) = valueAdjuster?.Invoke(currentError, errorAtTMinus0, errorAtTMinus1, errorAtTMinus2);
			Debug.Log($"Adjusted current & priorErrors: {currentError} & [{errorAtTMinus0}, {errorAtTMinus1}, {errorAtTMinus2}]");
			var output =
					GetStandardForm_Term1(dt, timeIntegral, timeDerivative, gainProportional, currentError) +
					GetStandardForm_Term2(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus0) +
					GetStandardForm_Term3(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus1);
			Debug.Log("" +
				$"{gainProportional} * {GetStandardForm_Term1(dt, timeIntegral, timeDerivative)} * {currentError} + " +
				$"{gainProportional} * {GetStandardForm_Term2(dt, timeIntegral, timeDerivative)} * {errorAtTMinus0} + " +
				$"{gainProportional} * {GetStandardForm_Term3(dt, timeIntegral, timeDerivative)} * {errorAtTMinus1} = " +
				$"{GetStandardForm_Term1(dt, timeIntegral, timeDerivative, gainProportional, currentError)} + " +
				$"{GetStandardForm_Term2(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus0)} + " +
				$"{GetStandardForm_Term3(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus1)} = " +
				$"{output}");
			if (updateMembers)
			{
				errorAtTMinus2 = errorAtTMinus1;
				errorAtTMinus1 = errorAtTMinus0;
				errorAtTMinus0 = currentError;
				Debug.Log($"new priorErrors: [{errorAtTMinus0}, {errorAtTMinus1}, {errorAtTMinus2}]");
			}
			else Debug.Log($"temp priorErrors: [{currentError}, {errorAtTMinus0}, {errorAtTMinus1}]");
			return output;
		}
		#region GetStandardForm Terms
		private static float GetStandardForm_Term1(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional = 1f,
			float error = 1f) => (1f + (timeIntegral != 0 ? dt / timeIntegral : 0) + timeDerivative / dt) * gainProportional * error;
		private static float GetStandardForm_Term2(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional = 1f,
			float error = 1f) => (-1f - 2f * timeDerivative / dt) * gainProportional * error;
		private static float GetStandardForm_Term3(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional = 1f,
			float error = 1f) => (timeDerivative / dt) * gainProportional * error;
		private static float GetStandardForm_TermPriorIntegral(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional = 1f,
			float accumulatedError = 1f) => ((timeIntegral != 0 ? dt / timeIntegral : 0)) * gainProportional * accumulatedError;
		#region Coefficients
		private static float GetStandardForm_CoefficientErrorAt0(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional) => (1f + (timeIntegral != 0 ? dt / timeIntegral : 0) + timeDerivative / dt) * gainProportional;
		private static float GetStandardForm_CoefficientErrorAt1(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional) => (-1f - 2f * timeDerivative / dt) * gainProportional;
		private static float GetStandardForm_CoefficientErrorAt2(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional) => (timeDerivative / dt) * gainProportional;
		private static float GetStandardForm_CoefficientPriorIntegral(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional) => ((timeIntegral != 0 ? dt / timeIntegral : 0)) * gainProportional;
		private static float GetStandardForm_CoefficientProportional(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional) => gainProportional;
		private static float GetStandardForm_CoefficientIntegral(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional) => ((timeIntegral != 0 ? dt / timeIntegral : 0)) * gainProportional;
		private static float GetStandardForm_CoefficientDerivative(
			float dt,
			float timeIntegral,
			float timeDerivative,
			float gainProportional) => (timeDerivative / dt) * gainProportional;
		#endregion
		#endregion
		#endregion
	}
}