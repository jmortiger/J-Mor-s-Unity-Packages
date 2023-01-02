using UnityEngine;
using JMor.Utility;
using System;
using System.Collections.Generic;
using System.Reflection;
using JMor.Utility.Inspector;

namespace JMor.AimAssist
{
	[CreateAssetMenu(fileName = "pid_newPIDSettingsObj", menuName = "Scriptable Object/PIDSettingsObj")]
	public class PIDSettingsObj : ScriptableObject, IPIDSettings//, JMor.Utility.Inspector.IHasPropertyInspectors
	{
		#region Core
		public float maxAccumulatedError = float.PositiveInfinity;
		public float MaxAccumulatedError { get => maxAccumulatedError; set => maxAccumulatedError = value; }
		public float minAccumulatedError = float.NegativeInfinity;
		[PropertyInspector]
		public float MinAccumulatedError { get => minAccumulatedError; set => minAccumulatedError = value; }
		[SerializeField]
		[Tooltip("A value of 0 means there is no filtering of instantaneous error (jittery inputs are preserved); A value of 1 means there is no reaction to instantaneous error.")]
		[Range(0, 1f - UnityEngine.Vector2.kEpsilon)]
		private float derivativeFilterPercentage = 0f;
		public float DerivativeFilterPercentage
		{
			get => derivativeFilterPercentage;
			set => derivativeFilterPercentage = Mathf.Abs((value >= 1f && value < 100f) || (value <= -1f && value > -100f) ? value / 100f : value % 1f);
		}
		public float gainDerivative = 1f;
		public float GainDerivative { get => gainDerivative; set => gainDerivative = value; }
		public float gainIntegral = 1f;
		public float GainIntegral { get => gainIntegral; set => gainIntegral = value; }
		public float gainProportional = 1f;
		public float GainProportional { get => gainProportional; set => gainProportional = value; }
		[PropertyInspector]
		[Tooltip("Tooltip Works!")]
		public float TimeDerivativ { get => this.As<IPIDSettings>().TimeDerivative; set => this.As<IPIDSettings>().TimeDerivative = value; }
		public float TimeIntegra { get => this.As<IPIDSettings>().TimeIntegral; set => this.As<IPIDSettings>().TimeIntegral = value; }
		#endregion
		#region Error Adjusters
		public Func<float, float, float, float, Tuple<float, float, float, float>> jointErrorAdjusterOld;
		public Func<float, float, float, float, Tuple<float, float, float, float>> JointErrorAdjusterOld { get => jointErrorAdjusterOld; set => jointErrorAdjusterOld = value; }
		public Func<float, float, float, Tuple<float, float, float>> jointErrorAdjuster;
		public Func<float, float, float, Tuple<float, float, float>> JointErrorAdjuster { get => jointErrorAdjuster; set => jointErrorAdjuster = value; }
		public Func<float, float> individualErrorAdjuster;
		public Func<float, float> IndividualErrorAdjuster { get => individualErrorAdjuster; set => individualErrorAdjuster = value; }
		public Func<float, float, float, float, float, Tuple<float, float, float, float, float>> jointAccumulatedErrorAdjusterOld;
		public Func<float, float, float, float, float, Tuple<float, float, float, float, float>> JointAccumulatedErrorAdjusterOld { get => jointAccumulatedErrorAdjusterOld; set => jointAccumulatedErrorAdjusterOld = value; }
		public Func<float, float, float, float, Tuple<float, float, float, float>> jointAccumulatedErrorAdjuster;
		public Func<float, float, float, float, Tuple<float, float, float, float>> JointAccumulatedErrorAdjuster { get => jointAccumulatedErrorAdjuster; set => jointAccumulatedErrorAdjuster = value; }
		public Func<float, float> accumulatedErrorAdjuster;
		public Func<float, float> AccumulatedErrorAdjuster { get => accumulatedErrorAdjuster; set => accumulatedErrorAdjuster = value; }
		[SerializeReference]
		public PropertyInspectorObject inspectorObject;
		#endregion
		void Reset()
		{
			minAccumulatedError = float.NegativeInfinity;
			maxAccumulatedError = float.PositiveInfinity;
			derivativeFilterPercentage = 0;
			gainDerivative = gainIntegral = gainProportional = 1;
			individualErrorAdjuster = null;
			jointErrorAdjuster = null;
			jointErrorAdjusterOld = null;
			accumulatedErrorAdjuster = null;
			jointAccumulatedErrorAdjuster = null;
			jointAccumulatedErrorAdjusterOld = null;
			inspectorObject = new PropertyInspectorObject(this, this.GetType());
		}
		void OnValidate()
		{
			if (MinAccumulatedError > MaxAccumulatedError)
			{
				maxAccumulatedError = float.PositiveInfinity;
				minAccumulatedError = float.NegativeInfinity;
			}
			if (inspectorObject == null)
				inspectorObject = new PropertyInspectorObject(this, this.GetType());
			else if (inspectorObject.container == null)
				inspectorObject.container = this;
			if (inspectorObject.containerType != this.GetType())
			{
				Debug.LogWarning($"Container Type fell out of sync, updating. If you didn't rename the container class, I'd be worried.\n\tAssumed: {inspectorObject.containerTypeStored}(As Type object: {inspectorObject.containerType})\n\tActual: {this.GetType().AssemblyQualifiedName}(As Type object: {this.GetType()}).");
				inspectorObject.containerType = this.GetType();
			}
		}
		[ContextMenu("AssignDefaultAdjuster")]
		void AssignDefaultAdjuster()
		{
			individualErrorAdjuster = this.As<IPIDSettings>().WrapAngularError;
			jointErrorAdjuster = this.As<IPIDSettings>().WrapAngularError;
			jointErrorAdjusterOld = this.As<IPIDSettings>().WrapAngularError;
			accumulatedErrorAdjuster = null;
			jointAccumulatedErrorAdjuster = null;
			jointAccumulatedErrorAdjusterOld = null;
		}
		public float Calculate(PIDControllerState state, float currentValue, float desiredValue, float dt, bool useDiscreteVelocityFormForIntegral, bool correctTimeScaleForIntegral, float timeScalerForIntegral, bool correctTimeScaleForDerivative, float timeScalerForDerivative, bool useAlternateDerivativeForm) => this.As<IPIDSettings>().Calculate(state, currentValue, desiredValue, dt, useDiscreteVelocityFormForIntegral, correctTimeScaleForIntegral, timeScalerForIntegral, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm);
	}
}