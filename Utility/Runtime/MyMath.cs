using System;
using System.Collections.Generic;
using UnityEngine;

namespace JMor.Utility
{
	// TODO: Housekeeping
	public static class MyMath
	{
		// TODO: SignedAngle does not return a signed angle; rename.
		#region SignedAngle
		public static float SignedAngle(Vector2 from, Vector2 to) => SignedAngleAbsolute(from, to);
		public static float SignedAngleAbsolute(Vector2 from, Vector2 to)
		{
			var angle = Vector2.SignedAngle(from, to);
			angle = (angle < 0) ? angle + 360 : angle;
			Debug.Assert(angle >= 0 && angle < 360);
			if (float.IsNaN(angle))
				Debug.LogWarning($"angle Nan, from: {from}, to: {to}");
			if (!(angle >= 0 && angle < 360))
				Debug.LogWarning($"{angle} is not >= 0 && < 360, it should be.");
			return angle;
		}
		public static float SignedAngleAbsolute(Vector3 from, Vector3 to, Vector3 axis)
		{
			var angle = Vector3.SignedAngle(from, to, axis);
			angle = (angle < 0) ? angle + 360 : angle;
			Debug.Assert(angle >= 0 && angle < 360);
			return angle;
		}
		#endregion

		#region Interpolater Constructors
		// TODO: Fix up to mirror float interpolater.
		public static Func<double, double> ConstructInterpolaterFunctionDouble(double[] xValues, double[] yValues)
		{
			// var i, length = xs.length;
			int length = xValues.Length;
			#region Deal with length edge cases
			if (length != yValues.Length)
				throw new ArgumentException("xValues and yValues must have same length");
			if (length == 0) return (x) => { return 0; };
			// f(x) = y;
			if (length == 1)
			{
				// Precomputing the result prevents problems if yValues is mutated later and allows garbage collection of yValues
				/*
					// Impl: Unary plus properly converts values to numbers
					var result = +ys[0];
					return function(x) { return result; };
				}*/
				var result = yValues[0];
				return (x) => result;
			}
			#endregion
			/*
			var indexes = [];
			for (i = 0; i < length; i++) { indexes.push(i); }
			indexes.sort(function(a, b) { return xs[a] < xs[b] ? -1 : 1; });
			var oldXs = xs, oldYs = ys;
			// Impl: Creating new arrays also prevents problems if the input arrays are mutated later
			xs = []; ys = [];
			// Impl: Unary plus properly converts values to numbers
			for (i = 0; i < length; i++) { xs.push(+oldXs[indexes[i]]); ys.push(+oldYs[indexes[i]]); }
			*/
			#region Rearrange xValues and yValues so that xValues is sorted
			//var indexes = new int[length];
			//for (int i = 0; i < length; i++)
			//	indexes[i] = i;
			//Debug.Log(xValues);
			//Debug.Log(yValues);
			var oldXValues = xValues;
			var oldYValues = yValues;
			var xs = new double[length];
			var ys = new double[length];
			oldXValues.CopyTo(xs, 0);
			oldYValues.CopyTo(ys, 0);
			Array.Sort(xs, ys);
			//Debug.Log(xs);
			//Debug.Log(ys);
			//Array.Sort<double[]>(indexes, Comparer<double[]>.Default)
			#endregion
			#region Get consecutive differences and slopes
			var dxs = new double[length - 1];
			Debug.Log($"dxs.Length: {dxs.Length}");
			var dys = new double[length - 1];
			Debug.Log($"dys.Length: {dys.Length}");
			var ms = new double[length - 1];
			Debug.Log($"ms.Length: {ms.Length}");
			for (int i = 0; i < length - 1; i++)
			{
				dxs[i] = xs[i + 1] - xs[i];
				dys[i] = ys[i + 1] - ys[i];
				ms[i] = dys[i] / dxs[i];
			}
			#endregion
			#region Get degree-1 coefficients
			/*
			var c1s = [ms[0]];
			for (i = 0; i < dxs.length - 1; i++)
			{
				var m = ms[i], mNext = ms[i + 1];
				if (m * mNext <= 0)
				{
					c1s.push(0);
				}
				else
				{
					var dx_ = dxs[i], dxNext = dxs[i + 1], common = dx_ + dxNext;
					c1s.push(3 * common / ((common + dxNext) / m + (common + dx_) / mNext));
				}
			}
			c1s.push(ms[ms.length - 1]);
			 */
			var c1s = new double[dxs.Length + 1];
			Debug.Log($"c1s.Length: {c1s.Length}");
			c1s[0] = ms[0];
			for (int i = 0; i < dxs.Length - 1; i++)
			{
				if (ms[i] * ms[i + 1] <= 0)
					c1s[i + 1] = 0;
				else
				{
					var common = dxs[i] + dxs[i + 1];
					c1s[i + 1] = 3 * common / ((common + dxs[i + 1]) / ms[i] + (common + dxs[i]) / ms[i + 1]);
				}
			}
			c1s[c1s.Length - 1] = ms[ms.Length - 1];
			#endregion
			#region Get degree-2 and degree-3 coefficients
			/*
			var c2s = [], c3s = [];
			for (i = 0; i < c1s.length - 1; i++)
			{
				var c1 = c1s[i], m_ = ms[i], invDx = 1 / dxs[i], common_ = c1 + c1s[i + 1] - m_ - m_;
				c2s.push((m_ - c1 - common_) * invDx);
				c3s.push(common_ * invDx * invDx);
			}
			 */
			var c2s = new double[c1s.Length - 1];
			Debug.Log($"c2s.Length: {c2s.Length}");
			var c3s = new double[c1s.Length - 1];
			Debug.Log($"c3s.Length: {c3s.Length}");
			for (int i = 0; i < c1s.Length - 1; i++)
			{
				var invDx = 1 / dxs[i];
				var common = c1s[i] + c1s[i + 1] - ms[i] * 2;
				c2s[i] = (ms[i] - c1s[i] - common) * invDx;
				c3s[i] = common * invDx * invDx;
			}
			#endregion
			#region Return interpolant function
			/*
			return function(x) {
				// The rightmost point in the dataset should give an exact result
				var i = xs.length - 1;
				if (x == xs[i]) { return ys[i]; }

				// Search for the interval x is in, returning the corresponding y if x is one of the original xs
				var low = 0, mid, high = c3s.length - 1;
				while (low <= high)
				{
					mid = Math.floor(0.5 * (low + high));
					var xHere = xs[mid];
					if (xHere < x) { low = mid + 1; }
					else if (xHere > x) { high = mid - 1; }
					else { return ys[mid]; }
				}
				i = Math.max(0, high);

				// Interpolate
				var diff = x - xs[i], diffSq = diff * diff;
				return ys[i] + c1s[i] * diff + c2s[i] * diffSq + c3s[i] * diff * diffSq;
			};
			*/
			return (x) =>
			{
			// The rightmost point in the dataset should give an exact result
			var i = xs.Length - 1;
				if (x == xs[i])
					return ys[i];

			// Search for the interval x is in, returning the corresponding y if x is one of the original xs
			int low = 0, mid, high = c3s.Length - 1;
				while (low <= high)
				{
					mid = (int)Math.Floor(0.5 * (low + high));
					var xHere = xs[mid];
					if (xHere < x)
						low = mid + 1;
					else if (xHere > x)
						high = mid - 1;
					else
						return ys[mid];
				}
				i = Math.Max(0, high);

			// Interpolate
			var diff = x - xs[i];
				var diffSq = diff * diff;
				return ys[i] + c1s[i] * diff + c2s[i] * diffSq + c3s[i] * diff * diffSq;
			};
			#endregion
		}

		public static Func<float, float> ConstructInterpolaterFunction(List<float> xValues, List<float> yValues)
		{
			return ConstructInterpolaterFunction(xValues.ToArray(), yValues.ToArray());
		}

		public static Func<float, float> ConstructInterpolaterFunction(Vector2[] xyValues)
		{
			var xs = new float[xyValues.Length];
			var ys = new float[xyValues.Length];
			for (int i = 0; i < xyValues.Length; i++)
			{
				xs[i] = xyValues[i].x;
				ys[i] = xyValues[i].y;
			}
			return ConstructInterpolaterFunction(xs, ys);
		}

		#region Setups
		//public static Func<float, float> CreateAimAssistAngleInterpolater(float[] xValues, float[] yValues)
		//{
		//	float[] xs;
		//	float[] ys;
		//	var output = MonotoneCubicInterpolationSetup(xValues, yValues, out xs, out ys);
		//	if (output != null)
		//		return output;
		//	int length = xValues.Length;
		//	var xsList = new List<float>(length * 2);
		//	var ysList = new List<float>(length * 2);
		//	xsList.AddRange(xs);
		//	ysList.AddRange(ys);
		//	#region Handle looping range
		//	// To handle angles < 0 and > 360, duplicate angles with offsets of +/-360
		//	for (int i = 0; i < length; i++)
		//	{
		//		xsList.Add(xsList[i] - 360f);
		//		ysList.Add(ysList[i] - 360f);
		//		xsList.Add(xsList[i] + 360f);
		//		ysList.Add(ysList[i] + 360f);
		//	}
		//	#endregion
		//	xs = xsList.ToArray();
		//	ys = ysList.ToArray();
		//	return ConstructInterpolaterFunction(xs, ys);
		//}

		//private static Func<float, float> MonotoneCubicInterpolationSetup(float[] xValues, float[] yValues, out float[] xs, out float[] ys)
		//{
		//	#region Deal with length edge cases
		//	if (xValues.Length != yValues.Length)
		//		throw new ArgumentException("xValues and yValues must have same length");
		//	int length = xValues.Length;
		//	xs = null;
		//	ys = null;
		//	if (length == 0) return (x) => { return 0; };
		//	// f(x) = y;
		//	if (length == 1)
		//	{
		//		// Precomputing the result prevents problems if yValues is mutated later and allows garbage collection of yValues
		//		var result = yValues[0];
		//		return (x) => result;
		//	}
		//	#endregion
		//	#region Rearrange xValues and yValues so that xValues is sorted
		//	// Free existing copy for GC
		//	xs = new float[length];
		//	ys = new float[length];
		//	xValues.CopyTo(xs, 0);
		//	yValues.CopyTo(ys, 0);
		//	Array.Sort(xs, ys);
		//	#endregion
		//	return null;
		//}
		#endregion
		/// <summary>
		/// 
		/// </summary>
		/// <param name="xValues"></param>
		/// <param name="yValues"></param>
		/// <returns></returns>
		/// <remarks>This is a port of https://en.wikipedia.org/wiki/Monotone_cubic_interpolation#Example_implementation to C#, with some minor tweaks.</remarks>
		public static Func<float, float> ConstructInterpolaterFunction(float[] xValues, float[] yValues/*, bool setupDone = false*/)
		{
			#region Deal with length edge cases
			if (xValues.Length != yValues.Length)
				throw new ArgumentException("xValues and yValues must have same length");
			int length = xValues.Length;
			//if (length == 0) return (x) => { return 0; };
			if (length == 0) return (x) => { return x; };
			// f(x) = y;
			if (length == 1)
			{
				// Precomputing the result prevents problems if yValues is mutated later and allows garbage collection of yValues
				var result = yValues[0];
				return (x) => result;
			}
			#endregion
			#region Rearrange xValues and yValues so that xValues is sorted
			var xs = new float[length];
			var ys = new float[length];
			xValues.CopyTo(xs, 0);
			yValues.CopyTo(ys, 0);
			Array.Sort(xs, ys);
			#endregion
			#region Use Setups
			//float[] xs;
			//float[] ys;
			//if (!setupDone)
			//	MonotoneCubicInterpolationSetup(xValues, yValues, out xs, out ys);
			//else
			//{
			//	// If a setup was already performed, then these values are safe to mutate
			//	xs = xValues;
			//	ys = yValues;
			//}
			//var length = xs.Length;
			#endregion
			#region Get consecutive differences and slopes
			var dxs = new float[length - 1];
			var dys = new float[length - 1];
			var ms = new float[length - 1];
			for (int i = 0; i < length - 1; i++)
			{
				dxs[i] = xs[i + 1] - xs[i];
				dys[i] = ys[i + 1] - ys[i];
				#region Handle duplicate x values (Original Code)
				// If there are duplicate x values, the slope will be undefined.
				if (dxs[i] == 0 && dys[i] != 0)
					throw new ArgumentException("Duplicate x values are not allowed.", "xValues");
				int safety = 0;
				while (dxs[i] == 0 && dys[i] == 0 && safety < 1000)
				{
					var xsTemp = new float[xs.Length - 1];
					var ysTemp = new float[ys.Length - 1];
					var dxsTemp = new float[dxs.Length - 1];
					var dysTemp = new float[dys.Length - 1];
					var msTemp = new float[ms.Length - 1];
					// They are sorted, so only filter the next entry, if there are more, we handle them as we find them.
					for (int j = 0; j < length - 1; j++)
					{
						if (j < i)
						{
							xsTemp[j] = xs[j];
							ysTemp[j] = ys[j];
							dxsTemp[j] = dxs[j];
							dysTemp[j] = dys[j];
							msTemp[j] = ms[j];
						}
						else
						{
							// If we're at or after the duplicate, add 1
							xsTemp[j] = xs[j + 1];
							ysTemp[j] = ys[j + 1];
						}
					}
					xs = xsTemp;
					ys = ysTemp;
					dxs = dxsTemp;
					dys = dysTemp;
					ms = msTemp;
					// TODO: Handle IndexOutOfRange here
					dxs[i] = xs[i + 1] - xs[i];
					dys[i] = ys[i + 1] - ys[i];
					length--;
					safety++;
				}
				if (safety > 500)
					Debug.LogWarning("Value pruning likely failed.");
				#endregion
				ms[i] = dys[i] / dxs[i];
				if (float.IsNaN(ms[i]))
					throw new ArgumentException("Duplicate x values are not allowed. Pruning failed", "xValues");
			}
			#endregion
			#region Get degree-1 coefficients
			var c1s = new float[length];
			c1s[0] = ms[0];
			for (int i = 0; i < length - 2; i++)
			{
				// Interpolant section: #3
				if (ms[i] * ms[i + 1] <= 0)
					c1s[i + 1] = 0;
				else
				{
					var common = dxs[i] + dxs[i + 1];
					c1s[i + 1] = 3 * common / ((common + dxs[i + 1]) / ms[i] + (common + dxs[i]) / ms[i + 1]);
				}
			}
			c1s[^1] = ms[^1];
			#endregion
			#region Get degree-2 and degree-3 coefficients
			var c2s = new float[c1s.Length - 1];
			var c3s = new float[c1s.Length - 1];
			for (int i = 0; i < c1s.Length - 1; i++)
			{
				var invDx = 1 / dxs[i];
				var common = c1s[i] + c1s[i + 1] - ms[i] * 2;
				c2s[i] = (ms[i] - c1s[i] - common) * invDx;
				c3s[i] = common * invDx * invDx;
			}
			#endregion
			#region Return interpolant function
			return (x) =>
			{
				Debug.Assert(!float.IsNaN(x));
			// The rightmost point in the dataset should give an exact result
			var i = xs.Length - 1;
				if (x == xs[i])
					return ys[i];

			// Search for the interval x is in, returning the corresponding y if x is one of the original xs
			int low = 0, mid, high = c3s.Length - 1;
				while (low <= high)
				{
					mid = (int)Math.Floor(0.5 * (low + high));
					var xHere = xs[mid];
					if (xHere < x)
						low = mid + 1;
					else if (xHere > x)
						high = mid - 1;
					else
						return ys[mid];
				}
				i = Math.Max(0, high);

			// Interpolate
			var diff = x - xs[i];
				var diffSq = diff * diff;
				return ys[i] + c1s[i] * diff + c2s[i] * diffSq + c3s[i] * diff * diffSq;
			};
			#endregion
		}
		#endregion

		#region Wrap Value
		public static float Wrap(float value, float minInclusive = 0, float maxExclusive = 1)
		{
			if (value < minInclusive)
				value += maxExclusive - minInclusive;
			else if (value >= maxExclusive)
				value -= maxExclusive - minInclusive;
			return value;
		}

		public static float Wrap(float value, int minInclusive, int maxInclusive)
		{
			if (value < minInclusive)
				value += maxInclusive - minInclusive;
			else if (value > maxInclusive)
				value -= maxInclusive - minInclusive;
			return value;
		}
		#endregion

		#region Round Vector
		public static Vector2 Round(Vector2 input, bool roundX = true, bool roundY = true)
		{
			if (roundX) input.x = Mathf.Round(input.x);
			if (roundY) input.y = Mathf.Round(input.y);
			return input;
		}

		public static Vector3 Round(Vector3 input, bool roundX = true, bool roundY = true, bool roundZ = true)
		{
			if (roundX) input.x = Mathf.Round(input.x);
			if (roundY) input.y = Mathf.Round(input.y);
			if (roundZ) input.z = Mathf.Round(input.z);
			return input;
		}
		#endregion

		public static Vector2[] ComputeCircle(
			float radius, 
			int totalPoints = 32, 
			Vector2? origin = null)
		{
			// r² = (x - origin.x)² + (y - origin.y)²
			// r² - (x - origin.x)² = (y - origin.y)²
			// y - origin.y = √(r² - (x - origin.x)²)
			// y = √(r² - (x - origin.x)²) + origin.y
			Vector2 originValue = origin ?? Vector2.zero;
			var remainder = totalPoints % 4;
			var trueTotalPoints = totalPoints - remainder;
			if (trueTotalPoints <= 3)
				trueTotalPoints = 4;
			var points = new Vector2[trueTotalPoints];
			var stepAngle = (Mathf.PI * 2f) / (float)points.Length;
			// TODO: Remove preOrigins
			//var preOrigins = new Vector2[trueTotalPoints];
			for (int i = 0; i <= points.Length / 4; i++)
			{
				var x = Mathf.Cos(stepAngle * i) * radius;
				var y = Mathf.Sqrt(radius * radius - x * x);
				//preOrigins[i].x = x;
				//preOrigins[i].y = y;
				points[i] = new Vector2(+x + originValue.x, +y + originValue.y);
				if (i != 0)
					points[^i] = new Vector2(+x + originValue.x, -y + originValue.y);
				if (i != points.Length / 4)
				{
					int farIndex = points.Length / 2 - i;
					//preOrigins[farIndex].x = -preOrigins[i].x;
					//preOrigins[farIndex].y = preOrigins[i].y;
					points[farIndex] = new Vector2(-/*preOrigins[i].*/x, /*preOrigins[i].*/y)/*preOrigins[farIndex]*/ + originValue;
					int fartherIndex = (points.Length / 2) + i;
					points[fartherIndex] = new Vector2(-/*preOrigins[i].*/x, -/*preOrigins[i].*/y) + originValue;
				}
			}
			return points;
		}

		//public static ComputeArc(float fromAngle, float toAngle, )

		public static Func<float, bool, float> EquationOfACircle(float radius, Vector2 origin)
		{
			float rSqr = radius * radius;
			return (x, getPositiveValue) =>
			{
				return Mathf.Sqrt(rSqr - Mathf.Pow(x - origin.x, 2)) * (getPositiveValue ? 1 : -1) + origin.y;
			};
		}
		public static Func<float, bool, float> EquationOfAnEllipse(float xAxisLength, float yAxisLength, Vector2 origin)
		{
			// https://en.wikipedia.org/wiki/Ellipse#In_Cartesian_coordinates
			float a = xAxisLength / 2f,
				  b = yAxisLength / 2f,
				  val = a * a * b * b;
			return (x, getPositiveValue) =>
			{
				return Mathf.Sqrt(val - Mathf.Pow(x - origin.x, 2)) * (getPositiveValue ? 1 : -1) + origin.y;
			};
		}

		public static void interpolationTESTER(float[] xs = null, float[] ys = null)
		{
			#region JS
			/*
			var createInterpolant = function(xs, ys) {
			var i, length = xs.length;

			// Deal with length issues
			if (length != ys.length) { throw 'Need an equal count of xs and ys.'; }
			if (length === 0) { return function(x) { return 0; }; }
			if (length === 1) {
				// Impl: Precomputing the result prevents problems if ys is mutated later and allows garbage collection of ys
				// Impl: Unary plus properly converts values to numbers
				var result = +ys[0];
				return function(x) { return result; };
			}

			// Rearrange xs and ys so that xs is sorted
			var indexes = [];
			for (i = 0; i < length; i++) { indexes.push(i); }
			indexes.sort(function(a, b) { return xs[a] < xs[b] ? -1 : 1; });
			var oldXs = xs, oldYs = ys;
			// Impl: Creating new arrays also prevents problems if the input arrays are mutated later
			xs = []; ys = [];
			// Impl: Unary plus properly converts values to numbers
			for (i = 0; i < length; i++) { xs.push(+oldXs[indexes[i]]); ys.push(+oldYs[indexes[i]]); }

			// Get consecutive differences and slopes
			var dys = [], dxs = [], ms = [];
			for (i = 0; i < length - 1; i++) {
				var dx = xs[i + 1] - xs[i], dy = ys[i + 1] - ys[i];
				dxs.push(dx); dys.push(dy); ms.push(dy/dx);
			}
			console.log(`dxs.length:${dxs.length}`);
			console.log(`dys.length:${dys.length}`);
			console.log(`ms.length:${ms.length}`);
			// Get degree-1 coefficients
			var c1s = [ms[0]];
			for (i = 0; i < dxs.length - 1; i++) {
				var m = ms[i], mNext = ms[i + 1];
				if (m*mNext <= 0) {
					c1s.push(0);
				} else {
					var dx_ = dxs[i], dxNext = dxs[i + 1], common = dx_ + dxNext;
					c1s.push(3*common/((common + dxNext)/m + (common + dx_)/mNext));
				}
			}
			c1s.push(ms[ms.length - 1]);
			console.log(`c1s.length:${c1s.length}`);

			// Get degree-2 and degree-3 coefficients
			var c2s = [], c3s = [];
			for (i = 0; i < c1s.length - 1; i++) {
				var c1 = c1s[i], m_ = ms[i], invDx = 1/dxs[i], common_ = c1 + c1s[i + 1] - m_ - m_;
				c2s.push((m_ - c1 - common_)*invDx); c3s.push(common_*invDx*invDx);
			}
			console.log(`c2s.length:${c2s.length}`);
			console.log(`c3s.length:${c3s.length}`);

			// Return interpolant function
			return function(x) {
				// The rightmost point in the dataset should give an exact result
				var i = xs.length - 1;
				if (x == xs[i]) { return ys[i]; }

				// Search for the interval x is in, returning the corresponding y if x is one of the original xs
				var low = 0, mid, high = c3s.length - 1;
				while (low <= high) {
					mid = Math.floor(0.5*(low + high));
					var xHere = xs[mid];
					if (xHere < x) { low = mid + 1; }
					else if (xHere > x) { high = mid - 1; }
					else { return ys[mid]; }
				}
				i = Math.max(0, high);

				// Interpolate
				var diff = x - xs[i], diffSq = diff*diff;
				return ys[i] + c1s[i]*diff + c2s[i]*diffSq + c3s[i]*diff*diffSq;
			};
		};
			 */
			#endregion
			xs ??= new float[] { 5, 20, 8, 70 };
			ys ??= new float[] { 3, 7.5f, 32, 11 };
			// var testInterp = createInterpolant([ 5, 20, 8, 70 ], [ 3, 7.5, 32, 11 ]);
			var testInterp = ConstructInterpolaterFunction(xValues: xs, yValues: ys);
			//var testArray = new float[20];
			for (var i = 0; i < 40; i++)
			{
				//testArray[i] = i * 2.5f;
				// console.log(`${i * 2.5} -> ${testInterp(i * 2.5)}`);
				Debug.Log($"{i * 2.5f} -> {testInterp(i * 2.5f)}");
			}
		}
	}

	#region Vector2 Comparers
	// TODO: Add Vector3.
	// TODO: Add additional checks if x/y values are equal.
	public class CompareVector2ByX : IComparer<Vector2>
	{
		public int Compare(Vector2 x, Vector2 y)
		{
			//if (x.x == y.x)
			//	return 0;
			//return (x.x > y.x) ? 1 : -1;

			//return (int)(x.x - y.x);

			return x.x.CompareTo(y.x);
		}
	}

	public class CompareVector2ByY : IComparer<Vector2>
	{
		public int Compare(Vector2 x, Vector2 y)
		{
			//if (x.y == y.y)
			//	return 0;
			//return (x.y > y.y) ? 1 : -1;

			//return (int)(x.y - y.y);

			return x.y.CompareTo(y.y);
		}
	}
	#endregion 
}