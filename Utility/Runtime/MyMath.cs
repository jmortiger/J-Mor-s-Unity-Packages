using System;
using System.Collections.Generic;
using UnityEngine;

namespace JMor.Utility
{
	// TODO: Housekeeping
	// TODO: Slim comments w/ inheritdoc
	public static class MyMath
	{
		// TODO: SignedAngle does not return a signed angle; rename.
		#region SignedAngle
		// public static float SignedAngle(Vector2 from, Vector2 to) => SignedAngleAbsolute(from, to);
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
		
		#region Interpolator Stuff
		#region Interpolator Constructors
		public static Func<double, double> ConstructInterpolatorFunction(IList<double> xValues, IList<double> yValues, double desiredSlopeSign = double.NaN)
		{
			#region Deal with length edge cases
			if (xValues.Count != yValues.Count)
				throw new ArgumentException("xValues and yValues must have same length");
			int length = xValues.Count;
			Func<double, double> CheckEdgeCases()
			{
				if (length == 0) // f(x) = x || 0
				{
					Debug.LogWarning($"Received empty input array, returning f(x) = x.");//0.");
					return x => x;//0;
				}
				if (length == 1) // f(x) = constant
				{
					var result = yValues[0]; // Precomputing the result prevents problems if yValues is mutated later and allows garbage collection of yValues
					Debug.LogWarning($"Received input array with 1 element, returning f(x) = {result}.");
					return x => result;
				}
				return null;
			};
			var earlyOut = CheckEdgeCases();
			if (earlyOut != null) return earlyOut;
			#endregion
			#region Rearrange xValues and yValues so that xValues is sorted
			var xs = new double[length];
			var ys = new double[length];
			xValues.CopyTo(xs, 0);
			yValues.CopyTo(ys, 0);
			Array.Sort(xs, ys);
			#endregion
			#region Get consecutive differences and slopes
			var dxs = new double[length - 1];
			var dys = new double[length - 1];
			var ms = new double[length - 1];
			for (int i = 0; i < length - 1; i++)
			{
				dxs[i] = xs[i + 1] - xs[i];
				dys[i] = ys[i + 1] - ys[i];
				#region Handle duplicate x values
				// If there are duplicate x values, the slope will be undefined.
				if (dxs[i] == 0 && dys[i] != 0)
					throw new ArgumentException("Duplicate x values are not allowed.", "xValues");
				// If both the xs and ys are duplicated, attempt to prune duplicates and continue as normal.
				int safety = 0;
				while (dxs[i] == 0 && dys[i] == 0 && safety < 500)
				{
					var xsTemp = new double[xs.Length - 1];
					var ysTemp = new double[ys.Length - 1];
					var dxsTemp = new double[dxs.Length - 1];
					var dysTemp = new double[dys.Length - 1];
					var msTemp = new double[ms.Length - 1];
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
					length--;
					if (i < dxs.Length)
					{
						// TODO: Test IndexOutOfRange here
						dxs[i] = xs[i + 1] - xs[i];
						dys[i] = ys[i + 1] - ys[i];
					}
					safety++;
				}
				if (safety != 0)
				{
					earlyOut = CheckEdgeCases();
					if (earlyOut != null) return earlyOut;
					if (safety > 250) Debug.LogWarning("Value pruning likely failed.");
					if (i < dxs.Length)
					{
						// TODO: Test IndexOutOfRange here
						ms[i] = dys[i] / dxs[i];
						if (double.IsNaN(ms[i]))
							throw new ArgumentException("Duplicate x values are not allowed. Pruning failed", "xValues");
					}
				}
				#endregion
				else
				{
					// If safety was unchanged, then there was no alteration to lengths; range check unnecessary.
					ms[i] = dys[i] / dxs[i];
					if (double.IsNaN(ms[i]))
						throw new ArgumentException("Duplicate x values are not allowed. Pruning failed", "xValues");
				}
			}
			#endregion
			#region Get degree-1 coefficients
			var c1s = new double[length];
			c1s[0] = ms[0];
			// desiredSlopeSign = double.IsNaN(desiredSlopeSign) ? 0 : desiredSlopeSign; // Uncomment to force monotonicity checks.
			// TODO: Test following alterations
			for (int i = 0; i < length - 2; i++)
			{
				desiredSlopeSign = (desiredSlopeSign == 0 && ms[i] != 0) ? ((ms[i] < 0) ? -1 : 1) : desiredSlopeSign;
				// Interpolate section: #3
				if (!double.IsNaN(desiredSlopeSign) && desiredSlopeSign * ms[i + 1] <= 0)// if (ms[i] * ms[i + 1] <= 0)
					c1s[i + 1] = 0;
				else
				{
					var common = dxs[i] + dxs[i + 1];
					c1s[i + 1] = 3d * common / ((common + dxs[i + 1]) / ms[i] + (common + dxs[i]) / ms[i + 1]);
				}
			}
			c1s[^1] = (desiredSlopeSign * ms[^1] <= 0) ? 0 : ms[^1];// c1s[^1] = ms[^1];
			#endregion
			#region Get degree-2 and degree-3 coefficients
			var c2s = new double[c1s.Length - 1];
			var c3s = new double[c1s.Length - 1];
			for (int i = 0; i < c1s.Length - 1; i++)
			{
				var invDx = 1d / dxs[i];
				var common = c1s[i] + c1s[i + 1] - ms[i] * 2d;
				c2s[i] = (ms[i] - c1s[i] - common) * invDx;
				c3s[i] = common * invDx * invDx;
			}
			#endregion
			#region Return interpolating function
			return x =>
			{
				Debug.Assert(!double.IsNaN(x));
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
					else// if (xHere == x)
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

		public static Func<float, float> ConstructInterpolatorFunction(IList<Vector2> xyValues)
		{
			var xs = new float[xyValues.Count];
			var ys = new float[xyValues.Count];
			for (int i = 0; i < xyValues.Count; i++)
			{
				xs[i] = xyValues[i].x;
				ys[i] = xyValues[i].y;
			}
			return ConstructInterpolatorFunction(xs, ys);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="xValues"></param>
		/// <param name="yValues"></param>
		/// <param name="desiredSlopeSign">The desired slope sign for a monotone interpolator. If NaN, function will not force monotonicity. If 0, function will take the sign of the first nonzero slope of the sorted inputs.</param>
		/// <returns></returns>
		/// <remarks>This is a port of https://en.wikipedia.org/wiki/Monotone_cubic_interpolation#Example_implementation to C#, with some minor tweaks.</remarks>
		// UGLY: Clean up
		// TODO: Unit test. Monotonicity fix only directly applies to first coefficients, might need to be added to later ones.
		// TODO: Change name to reflect the potential to create a non-monotone cubic interpolator.
		public static Func<float, float> ConstructInterpolatorFunction(IList<float> xValues, IList<float> yValues, float desiredSlopeSign = float.NaN)
		{
			#region Deal with length edge cases
			if (xValues.Count != yValues.Count)
				throw new ArgumentException("xValues and yValues must have same length");
			int length = xValues.Count;
			Func<float, float> CheckEdgeCases()
			{
				if (length == 0) // f(x) = x || 0
				{
					Debug.LogWarning($"Received empty input array, returning f(x) = x.");//0.");
					return x => x;//0;
				}
				if (length == 1) // f(x) = constant
				{
					var result = yValues[0]; // Precomputing the result prevents problems if yValues is mutated later and allows garbage collection of yValues
					Debug.LogWarning($"Received input array with 1 element, returning f(x) = {result}.");
					return x => result;
				}
				return null;
			};
			var earlyOut = CheckEdgeCases();
			if (earlyOut != null) return earlyOut;
			#endregion
			#region Rearrange xValues and yValues so that xValues is sorted
			var xs = new float[length];
			var ys = new float[length];
			xValues.CopyTo(xs, 0);
			yValues.CopyTo(ys, 0);
			Array.Sort(xs, ys);
			#endregion
			#region Get consecutive differences and slopes
			var dxs = new float[length - 1];
			var dys = new float[length - 1];
			var ms = new float[length - 1];
			for (int i = 0; i < length - 1; i++)
			{
				dxs[i] = xs[i + 1] - xs[i];
				dys[i] = ys[i + 1] - ys[i];
				#region Handle duplicate x values
				// If there are duplicate x values, the slope will be undefined.
				if (dxs[i] == 0 && dys[i] != 0)
					throw new ArgumentException("Duplicate x values are not allowed.", "xValues");
				// If both the xs and ys are duplicated, attempt to prune duplicates and continue as normal.
				int safety = 0;
				while (dxs[i] == 0 && dys[i] == 0 && safety < 500)
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
					length--;
					if (i < dxs.Length)
					{
						// TODO: Test IndexOutOfRange here
						dxs[i] = xs[i + 1] - xs[i];
						dys[i] = ys[i + 1] - ys[i];
					}
					safety++;
				}
				if (safety != 0)
				{
					earlyOut = CheckEdgeCases();
					if (earlyOut != null) return earlyOut;
					if (safety > 250) Debug.LogWarning("Value pruning likely failed.");
					if (i < dxs.Length)
					{
						// TODO: Test IndexOutOfRange here
						ms[i] = dys[i] / dxs[i];
						if (float.IsNaN(ms[i]))
							throw new ArgumentException("Duplicate x values are not allowed. Pruning failed", "xValues");
					}
				}
				#endregion
				else
				{
					// If safety was unchanged, then there was no alteration to lengths; range check unnecessary.
					ms[i] = dys[i] / dxs[i];
					if (float.IsNaN(ms[i]))
						throw new ArgumentException("Duplicate x values are not allowed. Pruning failed", "xValues");
				}
			}
			#endregion
			#region Get degree-1 coefficients
			var c1s = new float[length];
			c1s[0] = ms[0];
			// desiredSlopeSign = float.IsNaN(desiredSlopeSign) ? 0 : desiredSlopeSign; // Uncomment to force monotonicity checks.
			// TODO: Test following alterations
			for (int i = 0; i < length - 2; i++)
			{
				desiredSlopeSign = (desiredSlopeSign == 0 && ms[i] != 0) ? ((ms[i] < 0) ? -1 : 1) : desiredSlopeSign;
				// Interpolate section: #3
				if (!float.IsNaN(desiredSlopeSign) && desiredSlopeSign * ms[i + 1] <= 0)// if (ms[i] * ms[i + 1] <= 0)
					c1s[i + 1] = 0;
				else
				{
					var common = dxs[i] + dxs[i + 1];
					c1s[i + 1] = 3f * common / ((common + dxs[i + 1]) / ms[i] + (common + dxs[i]) / ms[i + 1]);
				}
			}
			c1s[^1] = (desiredSlopeSign * ms[^1] <= 0) ? 0 : ms[^1];// c1s[^1] = ms[^1];
			#endregion
			#region Get degree-2 and degree-3 coefficients
			var c2s = new float[c1s.Length - 1];
			var c3s = new float[c1s.Length - 1];
			for (int i = 0; i < c1s.Length - 1; i++)
			{
				var invDx = 1f / dxs[i];
				var common = c1s[i] + c1s[i + 1] - ms[i] * 2f;
				c2s[i] = (ms[i] - c1s[i] - common) * invDx;
				c3s[i] = common * invDx * invDx;
			}
			#endregion
			#region Return interpolating function
			return x =>
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
					else// if (xHere == x)
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

		#region Interpolation Testing
		public static List<Vector2> TestInterpolator(Func<float, float> interpolator, Vector2 rangeOfInputsToTest, float step = 1)
		{
			if (rangeOfInputsToTest.x > rangeOfInputsToTest.y)
			{
				var t = rangeOfInputsToTest.x;
				rangeOfInputsToTest.x = rangeOfInputsToTest.y;
				rangeOfInputsToTest.y = t;
			}
			var results = new List<Vector2>((int)((rangeOfInputsToTest.y - rangeOfInputsToTest.x) % step));
			for (float input = rangeOfInputsToTest.x; input < rangeOfInputsToTest.y; input += step)
				results.Add(new(input, interpolator(input)));
			return results;
		}
		public static List<Vector2> TestInterpolator(IList<float> inputs, IList<float> outputs, Vector2 rangeOfInputsToTest, float step = 1) => TestInterpolator(ConstructInterpolatorFunction(inputs, outputs), rangeOfInputsToTest, step);
		public static void interpolationTESTER(float[] xs = null, float[] ys = null)
		{
			#region JS
			/*
			var createInterpolator = function(xs, ys) {
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

			// Return interpolating function
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
			// var testInterpol = createInterpolator([ 5, 20, 8, 70 ], [ 3, 7.5, 32, 11 ]);
			var testInterpol = ConstructInterpolatorFunction(xValues: xs, yValues: ys);
			//var testArray = new float[20];
			for (var i = 0; i < 40; i++)
			{
				//testArray[i] = i * 2.5f;
				// console.log(`${i * 2.5} -> ${testInterpol(i * 2.5)}`);
				Debug.Log($"{i * 2.5f} -> {testInterpol(i * 2.5f)}");
			}
		}
		#endregion
		#endregion
		
		#region Wrap Value
		public static float Wrap(float value, float minInclusive = 0, float maxExclusive = 1)
		{
			// if (value < minInclusive)
			// 	value = maxExclusive - (minInclusive - value) % (maxExclusive - minInclusive);
			// else if (value > maxExclusive)
			// 	value = minInclusive - (value - minInclusive) % (maxExclusive - minInclusive);
			// return value == maxExclusive ? minInclusive : value;
			return Mathf.Repeat(value - minInclusive, maxExclusive - minInclusive) + minInclusive;
		}
		public static float Wrap_DEBUG(float value, float minInclusive = 0, float maxExclusive = 1)
		{
			if (value < minInclusive)
			{
				Debug.Log($"value({value}) < minInclusive({minInclusive})");
				Debug.Log($"maxExclusive - (minInclusive - value) % (maxExclusive - minInclusive)");
				Debug.Log($"{maxExclusive} - ({minInclusive} - {value}) % ({maxExclusive} - {minInclusive})");
				Debug.Log($"{maxExclusive} - {(minInclusive - value)} % {(maxExclusive - minInclusive)}");
				Debug.Log($"{maxExclusive} - {(minInclusive - value) % (maxExclusive - minInclusive)}");
				Debug.Log($"{maxExclusive - (minInclusive - value) % (maxExclusive - minInclusive)}");
				value = maxExclusive - (minInclusive - value) % (maxExclusive - minInclusive);
			}
			else if (value >= maxExclusive)
			{
				Debug.Log($"value({value}) >= maxExclusive({maxExclusive})");
				Debug.Log($"minInclusive - (value - minInclusive) % (maxExclusive - minInclusive)");
				Debug.Log($"{minInclusive} - ({value} - {minInclusive}) % ({maxExclusive} - {minInclusive})");
				Debug.Log($"{minInclusive} - {(value - minInclusive)} % {(maxExclusive - minInclusive)}");
				Debug.Log($"{minInclusive} - {(value - minInclusive) % (maxExclusive - minInclusive)}");
				Debug.Log($"{minInclusive - (value - minInclusive) % (maxExclusive - minInclusive)}");
				value = minInclusive - (value - minInclusive) % (maxExclusive - minInclusive);
			}
			Debug.Log($"Return Value: {value}");
			return value;
		}
		public static int Wrap(int value, int minInclusive, int maxInclusive)
		{
			return (int)Wrap(value, minInclusive, ++maxInclusive);
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

		#region Geometric Shapes
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
		#endregion

		#region Validation
		public static bool Validate(ref float value, float substitute = 0f)
		{
			if (float.IsFinite(value))
				return true;
			else
			{
				value = substitute;
				return false;
			}
		}
		public static float Validate(float value, float substitute = 0f) => float.IsFinite(value) ? value : substitute;
		public static int CompareMinAndMax<T>(ref T assumedMin, ref T assumedMax) where T : struct, IComparable, IComparable<T>, IEquatable<T>, IFormattable
		{
			if (assumedMin.CompareTo(assumedMax) < 0)
			{
				var t = assumedMin;
				assumedMin = assumedMax;
				assumedMax = t;
			}
			return assumedMin.CompareTo(assumedMax);
		}
		public static bool IsMinAndMax<T>(ref T assumedMin, ref T assumedMax) where T : struct, IComparable, IComparable<T>, IEquatable<T>, IFormattable
		{
			if (assumedMin.CompareTo(assumedMax) < 0)
			{
				var t = assumedMin;
				assumedMin = assumedMax;
				assumedMax = t;
			}
			return assumedMin.CompareTo(assumedMax) > 0;
		}
		#endregion
		
		#region Old
		// // https://en.wikipedia.org/wiki/PID_controller#Pseudocode
		// // UGLY: Clean up alternative forms.
		// // UGLY: Kill priorError, use priorErrors exclusively.
		// public struct PIDController
		// {
		// 	#region State
		// 	public float priorError;
		// 	public float accumulatedError;
		// 	public float priorEstimateForDerivative;
		// 	public float[] priorErrors;
		// 	#endregion
		// 	#region Settings
		// 	public float gainProportional;
		// 	public float gainIntegral;
		// 	public float gainDerivative;
		// 	public float maxAccumulatedError;
		// 	public float minAccumulatedError;
		// 	/// <summary>
		// 	/// A value of 0 means there is no filtering of instantaneous error (jittery inputs are preserved); A value of 1 means there is no reaction to instantaneous error.
		// 	/// </summary>
		// 	private float derivativeFilterPercentage;
		// 	#endregion
		// 	#region Properties
		// 	/// <summary>
		// 	/// A value clamped from 0 to 1 that controls the low pass filter for the derivative.
		// 	/// As the value approaches 0, current instantaneous error change rate has a greater impact than prior instantaneous error change rate.
		// 	/// As the value approaches 1, current instantaneous error change rate has a lesser impact than prior instantaneous error change rate.
		// 	/// A value of 0 means there is no derivative filtering of instantaneous error (jittery inputs are preserved).
		// 	/// A value of 1 means there is no derivative reaction to instantaneous error.
		// 	/// </summary>
		// 	/// <remarks>
		// 	/// If the desired behaviour is for the derivative to not affect the output, achieve it by setting <see cref="gainDerivative"/> to zero instead of setting this to 1.
		// 	/// Changing DerivativeFilterPercentage to 1 means any stored change will be continuously applied, easily leading to drastic and unpredictable behaviour.
		// 	/// Any attempts to set this value to 1 will be circumvented.
		// 	/// </remarks>
		// 	public float DerivativeFilterPercentage
		// 	{
		// 		get => derivativeFilterPercentage;
		// 		set => derivativeFilterPercentage = Mathf.Abs((value >= 1f && value < 100f) || (value <= -1f && value > -100f) ? value / 100f : value % 1f);
		// 		// set {
		// 		// 	value >= 1f && value < 100f ? value / 100f : Mathf.Clamp01(value);
		// 		// 	if (derivativeFilterPercentage == 1f)
		// 		// 		derivativeFilterPercentage = 0f;
		// 		// }
		// 	}
		// 	public float TimeDerivative
		// 	{
		// 		get => gainDerivative / gainProportional;
		// 		set => gainDerivative = gainProportional * value;
		// 	}
		// 	public float TimeIntegral
		// 	{
		// 		get => gainProportional / gainIntegral;
		// 		set => gainIntegral = gainProportional / value;
		// 	}
		// 	// public float GainDerivative {
		// 	// 	get => gainProportional * timeDerivative;
		// 	// 	set => timeDerivative = value / gainProportional;
		// 	// }
		// 	// public float GainIntegral {
		// 	// 	get => gainProportional / timeIntegral;
		// 	// 	set => timeIntegral = gainProportional / value;
		// 	// }
		// 	public bool IsDerivativeFiltered => derivativeFilterPercentage != 0;
		// 	public bool IsDerivativeApplied => gainDerivative != 0;
		// 	public bool IsIntegralApplied => gainIntegral != 0;
		// 	#endregion
		// 	#region Ctors
		// 	public PIDController(
		// 		float gainProportional = 1f,
		// 		float gainIntegral = 1f,
		// 		float gainDerivative = 1f,
		// 		float maxAccumulatedError = float.PositiveInfinity,
		// 		float minAccumulatedError = float.NaN,
		// 		float derivativeFilterPercentage = .8f)
		// 	{
		// 		this.gainProportional = gainProportional;
		// 		this.gainIntegral = gainIntegral;
		// 		this.gainDerivative = gainDerivative;
		// 		this.maxAccumulatedError = maxAccumulatedError;
		// 		this.minAccumulatedError = float.IsNaN(minAccumulatedError) ? -maxAccumulatedError : minAccumulatedError;
		// 		// this.derivativeFilterPercentage	= derivativeFilterPercentage >= 1f && derivativeFilterPercentage <= 100f ? derivativeFilterPercentage / 100f : Mathf.Clamp01(derivativeFilterPercentage);
		// 		this.derivativeFilterPercentage = Mathf.Abs((derivativeFilterPercentage >= 1f && derivativeFilterPercentage < 100f) || (derivativeFilterPercentage <= -1f && derivativeFilterPercentage > -100f) ? derivativeFilterPercentage / 100f : derivativeFilterPercentage % 1f);
		// 		if (this.minAccumulatedError > this.maxAccumulatedError)
		// 		{
		// 			var temp = this.maxAccumulatedError;
		// 			this.maxAccumulatedError = this.minAccumulatedError;
		// 			this.minAccumulatedError = temp;
		// 		}
		// 		this.priorError = this.accumulatedError = this.priorEstimateForDerivative = 0f;
		// 		this.priorErrors = new float[3];
		// 	}
		// 	public PIDController(
		// 		PIDController source,
		// 		float priorError = float.NaN,
		// 		float accumulatedError = float.NaN,
		// 		float priorEstimateForDerivative = float.NaN,

		// 		float gainProportional = float.NaN,
		// 		float gainIntegral = float.NaN,
		// 		float gainDerivative = float.NaN,
		// 		float maxAccumulatedError = float.NaN,
		// 		float minAccumulatedError = float.NaN,
		// 		float derivativeFilterPercentage = float.NaN)
		// 	{
		// 		this.priorError = float.IsNaN(priorError) ? source.priorError : priorError;
		// 		this.accumulatedError = float.IsNaN(accumulatedError) ? source.accumulatedError : accumulatedError;
		// 		this.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? source.priorEstimateForDerivative : priorEstimateForDerivative;
		// 		this.priorErrors = new float[3];

		// 		this.gainProportional = float.IsNaN(gainProportional) ? source.gainProportional : gainProportional;
		// 		this.gainIntegral = float.IsNaN(gainIntegral) ? source.gainIntegral : gainIntegral;
		// 		this.gainDerivative = float.IsNaN(gainDerivative) ? source.gainDerivative : gainDerivative;
		// 		this.maxAccumulatedError = float.IsNaN(maxAccumulatedError) ? source.maxAccumulatedError : maxAccumulatedError;
		// 		this.minAccumulatedError = float.IsNaN(minAccumulatedError) ? source.minAccumulatedError : minAccumulatedError;
		// 		this.derivativeFilterPercentage = float.IsNaN(derivativeFilterPercentage) ? source.derivativeFilterPercentage : derivativeFilterPercentage;
		// 	}
		// 	public static void ReassignPIDController(
		// 		ref PIDController source,
		// 		float priorError = float.NaN,
		// 		float accumulatedError = float.NaN,
		// 		float priorEstimateForDerivative = float.NaN,
		// 		float[] priorErrors = null,
		// 		float gainProportional = float.NaN,
		// 		float gainIntegral = float.NaN,
		// 		float gainDerivative = float.NaN,
		// 		float maxAccumulatedError = float.NaN,
		// 		float minAccumulatedError = float.NaN,
		// 		float derivativeFilterPercentage = float.NaN)
		// 	{
		// 		source.priorError = float.IsNaN(priorError) ? source.priorError : priorError;
		// 		source.accumulatedError = float.IsNaN(accumulatedError) ? source.accumulatedError : accumulatedError;
		// 		source.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? source.priorEstimateForDerivative : priorEstimateForDerivative;
		// 		source.priorErrors = priorErrors ?? source.priorErrors;
		// 		source.gainProportional = float.IsNaN(gainProportional) ? source.gainProportional : gainProportional;
		// 		source.gainIntegral = float.IsNaN(gainIntegral) ? source.gainIntegral : gainIntegral;
		// 		source.gainDerivative = float.IsNaN(gainDerivative) ? source.gainDerivative : gainDerivative;
		// 		source.maxAccumulatedError = float.IsNaN(maxAccumulatedError) ? source.maxAccumulatedError : maxAccumulatedError;
		// 		source.minAccumulatedError = float.IsNaN(minAccumulatedError) ? source.minAccumulatedError : minAccumulatedError;
		// 		source.derivativeFilterPercentage = float.IsNaN(derivativeFilterPercentage) ? source.derivativeFilterPercentage : derivativeFilterPercentage;
		// 	}
		// 	#endregion
		// 	public void ResetControllerState(
		// 		float priorError = float.NaN,
		// 		float accumulatedError = float.NaN,
		// 		float priorEstimateForDerivative = float.NaN,
		// 		float[] priorErrors = null)
		// 	{
		// 		this.priorError = float.IsNaN(priorError) ? 0f : priorError;
		// 		this.accumulatedError = float.IsNaN(accumulatedError) ? 0f : accumulatedError;
		// 		this.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? 0f : priorEstimateForDerivative;
		// 		this.priorErrors = priorErrors ?? new float[3] { 0, 0, 0 };
		// 	}
		// 	// TODO: Isn't really dependant on Ideal form from caller's perspective, refactor.
		// 	public float CalculateIdealForm(
		// 		float currentValue,
		// 		float desiredValue,
		// 		float dt,
		// 		Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null,
		// 		bool useDiscreteVelocityFormForIntegral = false,
		// 		bool correctTimeScaleForIntegral = true,
		// 		float timeScalerForIntegral = 1000f,
		// 		bool correctTimeScaleForDerivative = true,
		// 		float timeScalerForDerivative = 1000f,
		// 		bool useAlternateDerivativeForm = false)
		// 	{
		// 		var currentError = GetIdealFormTermP(currentValue, desiredValue);
		// 		if (valueAdjuster != null)
		// 			(priorErrors[0], priorErrors[1], priorErrors[2], _) = valueAdjuster(currentError, priorErrors[0], priorErrors[1], priorErrors[2]);
		// 		var finalIntegral = useDiscreteVelocityFormForIntegral ?
		// 			GetIdealFormTermI(dt, priorErrors[0], 0, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral) :
		// 			GetIdealFormTermI(dt, priorErrors[0], ref accumulatedError, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral);
		// 		var finalDerivative = GetIdealFormTermD(dt, priorErrors[0], priorErrors[1], priorErrors[2], ref priorEstimateForDerivative, gainDerivative, derivativeFilterPercentage, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm);
		// 		return gainProportional * currentError + finalIntegral + finalDerivative;
		// 	}
		// 	// public static CalculateIdealForm(
		// 	// 	float currentValue, 
		// 	// 	float desiredValue, 
		// 	// 	float dt, 
		// 	// 	Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null, 
		// 	// 	bool useDiscreteVelocityFormForIntegral = false,
		// 	// 	bool correctTimeScaleForIntegral = true,
		// 	// 	float timeScalerForIntegral = 1000f,
		// 	// 	bool correctTimeScaleForDerivative = true,
		// 	// 	float timeScalerForDerivative = 1000f,
		// 	// 	bool useAlternateDerivativeForm = false)
		// 	// {
		// 	// 	var currentError = GetIdealFormTermP(currentValue, desiredValue);
		// 	// 	if (valueAdjuster != null)
		// 	// 		(priorErrors[0], priorErrors[1], priorErrors[2], _) = valueAdjuster(currentError, priorErrors[0], priorErrors[1], priorErrors[2]);
		// 	// 	var finalIntegral = useDiscreteVelocityFormForIntegral ? 
		// 	// 		GetIdealFormTermI(dt, priorErrors[0], 0, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral) : 
		// 	// 		GetIdealFormTermI(dt, priorErrors[0], ref accumulatedError, gainIntegral, minAccumulatedError, maxAccumulatedError, correctTimeScaleForIntegral, timeScalerForIntegral);
		// 	// 	var finalDerivative = GetIdealFormTermD(dt, priorErrors[0], priorErrors[1], priorErrors[2], ref priorEstimateForDerivative, gainDerivative, derivativeFilterPercentage, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm);
		// 	// 	return gainProportional * currentError + finalIntegral + finalDerivative;
		// 	// }
		// 	#region Get Ideal Form Components
		// 	private static float GetIdealFormTermP(
		// 		float currentValue,
		// 		float desiredValue,
		// 		Func<float, float> valueAdjuster = null,
		// 		float gainProportional = 1f)
		// 	{
		// 		var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
		// 		Validate(ref currentError);
		// 		return gainProportional * currentError;
		// 	}
		// 	#region GetIdealFormTermI
		// 	/// <summary>
		// 	/// Solves 𝑲ᵢ*₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ
		// 	/// </summary>
		// 	/// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		// 	/// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		// 	/// <param name="accumulatedError">Equivalent to ₀∫ᵗ𝓮(𝒂)𝒅𝒂 where 𝒂 = 𝒕ₙ-Δ𝒕. To emulate a discrete velocity integral, pass 0.</param>
		// 	/// <param name="gainIntegral">Equivalent to 𝑲ᵢ. If the base term is desired (e.g. to store the accumulated error), a value of 1 will result in the base value.</param>
		// 	/// <param name="minAccumulatedError">The minimum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the lower end, a value of -Infinity will result in the unaltered value.</param>
		// 	/// <param name="maxAccumulatedError">The maximum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the upper end, a value of +Infinity will result in the unaltered value.</param>
		// 	/// <param name="assumeTimeLessThan1"><paramref name="dt"> appears to need to be equal to, or greater than one. Assuming <paramref name="dt"> is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". Theoretically, this could affect integral as well. To resolve this, <paramref name="dt"> is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, <paramref name="errorAtTMinus0"/> is divided (instead of multiplied) by <paramref name="dt">. If this behaviour is undesirable, pass in false to disable it.</param>
		// 	/// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of <paramref name="dt"> far less than 1. If equal to NaN || +/-Infinity, <paramref name="dt"> is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		// 	/// <returns></returns>
		// 	private static float GetIdealFormTermI(
		// 		float dt,
		// 		float errorAtTMinus0,
		// 		float accumulatedError,
		// 		// Func<float, float> valueAdjuster = null,
		// 		float gainIntegral = 1f,
		// 		float minAccumulatedError = float.NegativeInfinity,
		// 		float maxAccumulatedError = float.PositiveInfinity,
		// 		bool assumeTimeLessThan1 = false,
		// 		float scaleDTBy = 1000f)
		// 	{
		// 		var integral = accumulatedError;// + errorAtTMinus0 * dt * (assumeTimeLessThan1 ? 1000f : 1f);
		// 		if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
		// 		if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) integral += errorAtTMinus0 / dt;
		// 		else integral += errorAtTMinus0 * dt;
		// 		Validate(ref integral);
		// 		integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
		// 		return gainIntegral * integral;
		// 	}
		// 	/// <summary>
		// 	/// Solves 𝑲ᵢ*₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ
		// 	/// </summary>
		// 	/// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		// 	/// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		// 	/// <param name="accumulatedError">Equivalent to ₀∫ᵗ𝓮(𝒂)𝒅𝒂 where 𝒂 = 𝒕ₙ-Δ𝒕; will be updated to curr+=<paramref name="errorAtTMinus0"/> * dt * (<paramref name="assumeTimeLessThan1"/> ? <paramref name="scaleDTBy"/> : 1f)</param>
		// 	/// <param name="gainIntegral">Equivalent to 𝑲ᵢ. If the base term is desired (i.e. to store the accumulated error), a value of 1 will result in the base value.</param>
		// 	/// <param name="minAccumulatedError">The minimum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the lower end, a value of -Infinity will result in the unaltered value.</param>
		// 	/// <param name="maxAccumulatedError">The maximum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the upper end, a value of +Infinity will result in the unaltered value.</param>
		// 	/// <param name="assumeTimeLessThan1"><paramref name="dt"> appears to need to be equal to, or greater than one. Assuming <paramref name="dt"> is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". Theoretically, this could affect integral as well. To resolve this, <paramref name="dt"> is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, <paramref name="errorAtTMinus0"/> is divided (instead of multiplied) by <paramref name="dt">. If this behaviour is undesirable, pass in false to disable it.</param>
		// 	/// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of <paramref name="dt"> far less than 1. If equal to NaN || +/-Infinity, <paramref name="dt"> is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		// 	/// <returns></returns>
		// 	private static float GetIdealFormTermI(
		// 		float dt,
		// 		float errorAtTMinus0,
		// 		ref float accumulatedError,
		// 		// Func<float, float> valueAdjuster = null,
		// 		float gainIntegral = 1f,
		// 		float minAccumulatedError = float.NegativeInfinity,
		// 		float maxAccumulatedError = float.PositiveInfinity,
		// 		bool assumeTimeLessThan1 = false,
		// 		float scaleDTBy = 1000f)
		// 	{
		// 		// accumulatedError += errorAtTMinus0 * dt * (assumeTimeLessThan1 ? 1000f : 1f);
		// 		if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
		// 		if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) accumulatedError += errorAtTMinus0 / dt;
		// 		else accumulatedError += errorAtTMinus0 * dt;
		// 		Validate(ref accumulatedError);
		// 		accumulatedError = Mathf.Clamp(accumulatedError, minAccumulatedError, maxAccumulatedError);
		// 		return gainIntegral * accumulatedError;
		// 	}
		// 	#endregion
		// 	#region GetIdealFormTermD
		// 	/// <summary>
		// 	/// Solves 𝑲d*(𝒅𝓮(𝒕ₙ))/(𝒅𝒕ₙ)
		// 	/// </summary>
		// 	/// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		// 	/// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		// 	/// <param name="errorAtTMinus1">Equivalent to 𝓮(𝒕ₙ₋₁); the prior error</param>
		// 	/// <param name="errorAtTMinus2">Equivalent to 𝓮(𝒕ₙ₋₂); the error before last</param>
		// 	/// <param name="priorEstimateForDerivative">Equivalent to (𝒅𝓮(𝒕ₙ₋₁))/(𝒅𝒕ₙ₋₁) filtered for high frequency noise. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		// 	/// <param name="gainDerivative">Equivalent to 𝑲d. If the base term is desired (i.e. to store the filtered derivative for filtering), a value of 1 will result in the base value.</param>
		// 	/// <param name="derivativeFilterPercentage">The percentage for filtering derivative noise. Corresponds to <see cref="PIDController.DerivativeFilterPercentage"/>. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		// 	/// <param name="assumeTimeLessThan1">dt appears to need to be equal to, or greater than one. Assuming dt is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". To resolve this, dt is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. If this behaviour is undesirable, pass in false to disable it.</param>
		// 	/// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of dt far less than 1. If equal to NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		// 	/// <param name="useAlternateForm">This implementation was derived from the Wikipedia entry on PID Controllers. However, it appears as though their pseudocode incorrectly calculates this term (the correct form seems to be 𝑲d * (𝓮(𝒕ₙ) - 2*𝓮(𝒕ₙ₋₁) + 𝓮(𝒕ₙ₋₂)) / Δ𝒕, but their corresponding pseudocode uses 𝑲d * 𝓮(𝒕ₙ) − 𝓮(𝒕ₙ₋₁)) / Δ𝒕. To use this (assumedly) incorrect calculation, pass in true. Note that this will render <paramref name="errorAtTMinus2"/> useless.</param>
		// 	/// <returns></returns>
		// 	private static float GetIdealFormTermD(
		// 		float dt,
		// 		float errorAtTMinus0,
		// 		float errorAtTMinus1,
		// 		float errorAtTMinus2,
		// 		float priorEstimateForDerivative = 0f,
		// 		float gainDerivative = 1f,
		// 		float derivativeFilterPercentage = 0f,
		// 		bool assumeTimeLessThan1 = true,
		// 		float scaleDTBy = 1000f,
		// 		bool useAlternateForm = false)
		// 	{
		// 		// var derivative = useAlternateForm ? (errorAtTMinus0 - errorAtTMinus1) / dt : (errorAtTMinus0 - 2f * errorAtTMinus1 + errorAtTMinus2) / dt;
		// 		var derivative = (errorAtTMinus0 - (useAlternateForm ? errorAtTMinus1 : 2f * errorAtTMinus1 - errorAtTMinus2));
		// 		if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
		// 		if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) derivative *= dt;
		// 		else derivative /= dt;
		// 		Validate(ref derivative);
		// 		var filteredDerivative = derivativeFilterPercentage * priorEstimateForDerivative + (1f - derivativeFilterPercentage) * derivative;
		// 		Validate(ref filteredDerivative);
		// 		return gainDerivative * filteredDerivative;
		// 	}
		// 	/// <summary>
		// 	/// Solves 𝑲d*(𝒅𝓮(𝒕ₙ))/(𝒅𝒕ₙ)
		// 	/// </summary>
		// 	/// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		// 	/// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		// 	/// <param name="errorAtTMinus1">Equivalent to 𝓮(𝒕ₙ₋₁); the prior error</param>
		// 	/// <param name="errorAtTMinus2">Equivalent to 𝓮(𝒕ₙ₋₂); the error before last</param>
		// 	/// <param name="priorEstimateForDerivative">Equivalent to (𝒅𝓮(𝒕ₙ₋₁))/(𝒅𝒕ₙ₋₁) filtered for high frequency noise. If not filtering the derivative, a value of 0 will result in the unfiltered value. Will be updated.</param>
		// 	/// <param name="gainDerivative">Equivalent to 𝑲d. If the base term is desired (i.e. to store the filtered derivative for filtering), a value of 1 will result in the base value.</param>
		// 	/// <param name="derivativeFilterPercentage">The percentage for filtering derivative noise. Corresponds to <see cref="PIDController.DerivativeFilterPercentage"/>. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		// 	/// <param name="assumeTimeLessThan1">dt appears to need to be equal to, or greater than one. Assuming dt is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". To resolve this, dt is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. If this behaviour is undesirable, pass in false to disable it.</param>
		// 	/// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of dt far less than 1. If equal to NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		// 	/// <param name="useAlternateForm">This implementation was derived from the Wikipedia entry on PID Controllers. However, it appears as though their pseudocode incorrectly calculates this term (the correct form seems to be 𝑲d * (𝓮(𝒕ₙ) - 2*𝓮(𝒕ₙ₋₁) + 𝓮(𝒕ₙ₋₂)) / Δ𝒕, but their corresponding pseudocode uses 𝑲d * 𝓮(𝒕ₙ) − 𝓮(𝒕ₙ₋₁)) / Δ𝒕. To use this (assumedly) incorrect calculation, pass in true. Note that this will render <paramref name="errorAtTMinus2"/> useless.</param>
		// 	/// <returns></returns>
		// 	private static float GetIdealFormTermD(
		// 		float dt,
		// 		float errorAtTMinus0,
		// 		float errorAtTMinus1,
		// 		float errorAtTMinus2,
		// 		ref float priorEstimateForDerivative,
		// 		float gainDerivative = 1f,
		// 		float derivativeFilterPercentage = 0f,
		// 		bool assumeTimeLessThan1 = true,
		// 		float scaleDTBy = 1000f,
		// 		bool useAlternateForm = false)
		// 	{
		// 		// var derivative = useAlternateForm ? (errorAtTMinus0 - errorAtTMinus1) / dt : (errorAtTMinus0 - 2f * errorAtTMinus1 + errorAtTMinus2) / dt;
		// 		var derivative = (errorAtTMinus0 - (useAlternateForm ? errorAtTMinus1 : 2f * errorAtTMinus1 - errorAtTMinus2));
		// 		if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
		// 		if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) derivative *= dt;
		// 		else derivative /= dt;
		// 		Validate(ref derivative);
		// 		var filteredDerivative = derivativeFilterPercentage * priorEstimateForDerivative.Validate() + (1f - derivativeFilterPercentage) * derivative;
		// 		Validate(ref filteredDerivative);
		// 		return gainDerivative * filteredDerivative;
		// 	}
		// 	#endregion
		// 	#endregion
		// 	#region Convert Between Gains And Time
		// 	public static float SolveForGainDerivative(float gainProportional, float timeDerivative) => gainProportional * timeDerivative;
		// 	public static float SolveForTimeDerivative(float gainProportional, float gainDerivative) => gainDerivative / gainProportional;
		// 	public static float SolveForGainIntegral(float gainProportional, float timeIntegral) => gainProportional / timeIntegral;
		// 	public static float SolveForTimeIntegral(float gainProportional, float gainIntegral) => gainProportional / gainIntegral;
		// 	#endregion
		// 	// TODO: Obsolete, parse through and trim waaaaay down all below.
		// 	#region Old Calculate Forms
		// 	[Obsolete("Use CalculateIdealForm instead.", true)]
		// 	public float Calculate_DEBUG(float currentValue, float desiredValue, float dt, Func<float, float> valueAdjuster = null, bool updateMembers = true)
		// 	{
		// 		var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
		// 		Debug.Log($"currentError: {currentError}");
		// 		if (!Validate(ref currentError))
		// 			Debug.Log($"Corrected currentError: {currentError}");
		// 		var integral = accumulatedError + currentError * dt;
		// 		Debug.Log($"integral: {integral}");
		// 		Validate(ref integral);
		// 		integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
		// 		Debug.Log($"Corrected integral: {integral}");
		// 		var derivative = (currentError - 2f * priorError + priorErrors[1]) / dt;// var derivative = (currentError - priorError) / dt;
		// 		Debug.Log($"derivative: {derivative}");
		// 		Validate(ref derivative);
		// 		// derivative = Mathf.Clamp(derivative, -500f, 500f);
		// 		Debug.Log($"Corrected derivative: {derivative}");
		// 		var filteredDerivative = (derivativeFilterPercentage * priorEstimateForDerivative) + (1f - derivativeFilterPercentage) * derivative;
		// 		Debug.Log($"filteredDerivative: {filteredDerivative}");
		// 		Validate(ref filteredDerivative);
		// 		Debug.Log($"Corrected filteredDerivative: {filteredDerivative}");
		// 		Debug.Log($"priorError: {priorError}");
		// 		Debug.Log($"priorIntegralComponent: {accumulatedError}");
		// 		if (updateMembers)
		// 		{
		// 			priorErrors[2] = priorErrors[1];
		// 			priorErrors[1] = priorErrors[0];
		// 			priorErrors[0] = currentError;
		// 			priorError = currentError;
		// 			accumulatedError = integral;
		// 			priorEstimateForDerivative = filteredDerivative;
		// 		}
		// 		return Wrap(gainProportional * currentError + gainIntegral * integral + gainDerivative * filteredDerivative, 0f, 360f);// TODO: Wrapping is for aim assist, but it shouldn't be done here, at least not statically. It should be changed to be implementation-agnostic. Maybe use the same solution as the error adjustment?
		// 	}
		// 	[Obsolete("Use CalculateIdealForm instead.", true)]
		// 	public float Calculate(float currentValue, float desiredValue, float dt, Func<float, float> valueAdjuster = null, bool updateMembers = true)
		// 	{
		// 		var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
		// 		Validate(ref currentError);
		// 		var integral = accumulatedError + currentError * dt;
		// 		Validate(ref integral);
		// 		integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
		// 		var derivative = (currentError - 2f * priorError + priorErrors[1]) / dt;// var derivative = (currentError - priorError) / dt;
		// 		Validate(ref derivative);
		// 		// derivative = Mathf.Clamp(derivative, -500f, 500f);
		// 		var filteredDerivative = (derivativeFilterPercentage * priorEstimateForDerivative) + (1f - derivativeFilterPercentage) * derivative;
		// 		Validate(ref filteredDerivative);
		// 		if (updateMembers)
		// 		{
		// 			priorErrors[2] = priorErrors[1];
		// 			priorErrors[1] = priorErrors[0];
		// 			priorErrors[0] = currentError;
		// 			priorError = currentError;
		// 			accumulatedError = integral;
		// 			priorEstimateForDerivative = filteredDerivative;
		// 		}
		// 		return Wrap(gainProportional * currentError + gainIntegral * integral + gainDerivative * filteredDerivative, 0f, 360f);// TODO: Wrapping is for aim assist, but it shouldn't be done here, at least not statically. It should be changed to be implementation-agnostic. Maybe use the same solution as the error adjustment?
		// 	}
		// 	[Obsolete("Use CalculateIdealForm instead.", true)]
		// 	public static float Calculate(
		// 		float currentValue,
		// 		float desiredValue,
		// 		float dt,
		// 		float gainProportional,
		// 		float gainIntegral,
		// 		float gainDerivative,
		// 		ref float errorAtTMinus0,
		// 		ref float errorAtTMinus1,
		// 		ref float errorAtTMinus2,
		// 		ref float accumulatedError,
		// 		ref float priorEstimateForDerivative,
		// 		float derivativeFilterPercentage = 0f,
		// 		float minAccumulatedError = float.NegativeInfinity,
		// 		float maxAccumulatedError = float.PositiveInfinity,
		// 		Func<float, float> valueAdjuster = null)
		// 	{
		// 		var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
		// 		Validate(ref currentError);
		// 		errorAtTMinus2 = errorAtTMinus1;
		// 		errorAtTMinus1 = errorAtTMinus0;
		// 		errorAtTMinus0 = currentError;
		// 		var integral = accumulatedError + currentError * dt;
		// 		Validate(ref integral);
		// 		integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
		// 		accumulatedError = integral;
		// 		var derivative = (currentError - 2f * errorAtTMinus0 + errorAtTMinus1) / dt;// var derivative = (currentError - priorError) / dt;
		// 		Validate(ref derivative);
		// 		// derivative = Mathf.Clamp(derivative, -500f, 500f);
		// 		var filteredDerivative = (derivativeFilterPercentage * priorEstimateForDerivative) + (1f - derivativeFilterPercentage) * derivative;
		// 		Validate(ref filteredDerivative);
		// 		priorEstimateForDerivative = filteredDerivative;
		// 		return gainProportional * currentError + gainIntegral * integral + gainDerivative * filteredDerivative;
		// 	}
		// 	[Obsolete("Use CalculateIdealForm instead.", true)]
		// 	public float Calculate2(float currentValue, float desiredValue, float dt, Func<float, float> valueAdjuster = null)
		// 	{
		// 		var currentError = /*valueAdjuster?.Invoke(desiredValue - currentValue) ?? */desiredValue - currentValue;
		// 		Validate(ref currentError);
		// 		priorErrors[2] = priorErrors[1];
		// 		priorErrors[1] = priorErrors[0];
		// 		priorErrors[0] = currentError;
		// 		// return Wrap(GetStandardForm_Term1(dt) * priorErrors[0] + GetStandardForm_Term2(dt) * priorErrors[1] + GetStandardForm_Term3(dt) * priorErrors[2], 0f, 360f);// TODO: Wrapping is for aim assist, but it shouldn't be done here, at least not statically. It should be changed to be implementation-agnostic. Maybe use the same solution as the error adjustment?
		// 		return GetDiscreteForm_Term1(dt) * priorErrors[0] + GetDiscreteForm_Term2(dt) * priorErrors[1] + GetDiscreteForm_Term3(dt) * priorErrors[2];
		// 	}
		// 	[Obsolete("Use CalculateIdealForm instead.", true)]
		// 	public float Calculate2_DEBUG(float currentValue, float desiredValue, float dt, Func<float, float> valueAdjuster = null)
		// 	{
		// 		Debug.Log($"currentError: {desiredValue - currentValue}");
		// 		var currentError = /*valueAdjuster?.Invoke(desiredValue - currentValue) ?? */desiredValue - currentValue;
		// 		Debug.Log($"currentErrorAdjusted: {currentError}");
		// 		Validate(ref currentError);
		// 		Debug.Log($"Corrected currentError: {currentError}");
		// 		priorErrors[2] = priorErrors[1];
		// 		priorErrors[1] = priorErrors[0];
		// 		priorErrors[0] = currentError;
		// 		Debug.Log($"priorErrors: [{priorErrors[0]}, {priorErrors[1]}, {priorErrors[2]}]");
		// 		// return Wrap(GetStandardForm_Term1(dt) * priorErrors[0] + GetStandardForm_Term2(dt) * priorErrors[1] + GetStandardForm_Term3(dt) * priorErrors[2], 0f, 360f);// TODO: Wrapping is for aim assist, but it shouldn't be done here, at least not statically. It should be changed to be implementation-agnostic. Maybe use the same solution as the error adjustment?
		// 		Debug.Log($"{GetDiscreteForm_Term1(dt)} * {priorErrors[0]} + {GetDiscreteForm_Term2(dt)} * {priorErrors[1]} + {GetDiscreteForm_Term3(dt)} * {priorErrors[2]}");
		// 		return GetDiscreteForm_Term1(dt) * priorErrors[0] + GetDiscreteForm_Term2(dt) * priorErrors[1] + GetDiscreteForm_Term3(dt) * priorErrors[2];
		// 	}
		// 	#region GetDiscreteForm
		// 	private float GetDiscreteForm_Term1(float dt) => gainProportional + gainIntegral * dt + gainDerivative / dt;
		// 	private float GetDiscreteForm_Term2(float dt) => -gainProportional - 2f * gainDerivative / dt;
		// 	private float GetDiscreteForm_Term3(float dt) => gainDerivative / dt;
		// 	#endregion
		// 	[Obsolete("Use CalculateIdealForm instead.", true)]
		// 	public static float CalculateStandardForm(float currentValue, float desiredValue, float dt, float timeIntegral, float timeDerivative, float gainProportional, ref float errorAtTMinus0, ref float errorAtTMinus1, ref float errorAtTMinus2, Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null, bool updateMembers = true)
		// 	{
		// 		var currentError = desiredValue - currentValue;
		// 		Validate(ref currentError);
		// 		var output =
		// 				GetStandardForm_Term1(dt, timeIntegral, timeDerivative, gainProportional, currentError) +
		// 				GetStandardForm_Term2(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus0) +
		// 				GetStandardForm_Term3(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus1);
		// 		if (updateMembers)
		// 		{
		// 			errorAtTMinus2 = errorAtTMinus1;
		// 			errorAtTMinus1 = errorAtTMinus0;
		// 			errorAtTMinus0 = currentError;
		// 		}
		// 		return output;
		// 	}
		// 	[Obsolete("Use CalculateIdealForm instead.", true)]
		// 	public static float CalculateStandardForm_DEBUG(float currentValue, float desiredValue, float dt, float timeIntegral, float timeDerivative, float gainProportional, ref float errorAtTMinus0, ref float errorAtTMinus1, ref float errorAtTMinus2, Func<float, float, float, float, Tuple<float, float, float, float>> valueAdjuster = null, bool updateMembers = true)
		// 	{
		// 		var currentError = desiredValue - currentValue;
		// 		Debug.Log($"currentError: {currentError}");
		// 		Validate(ref currentError);
		// 		Debug.Log($"Corrected currentError: {currentError}");
		// 		Debug.Log($"current & priorErrors: {currentError} & [{errorAtTMinus0}, {errorAtTMinus1}, {errorAtTMinus2}]");
		// 		(currentError, errorAtTMinus0, errorAtTMinus1, errorAtTMinus2) = valueAdjuster?.Invoke(currentError, errorAtTMinus0, errorAtTMinus1, errorAtTMinus2);
		// 		Debug.Log($"Adjusted current & priorErrors: {currentError} & [{errorAtTMinus0}, {errorAtTMinus1}, {errorAtTMinus2}]");
		// 		var output =
		// 				GetStandardForm_Term1(dt, timeIntegral, timeDerivative, gainProportional, currentError) +
		// 				GetStandardForm_Term2(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus0) +
		// 				GetStandardForm_Term3(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus1);
		// 		Debug.Log("" +
		// 			$"{gainProportional} * {GetStandardForm_Term1(dt, timeIntegral, timeDerivative)} * {currentError} + " +
		// 			$"{gainProportional} * {GetStandardForm_Term2(dt, timeIntegral, timeDerivative)} * {errorAtTMinus0} + " +
		// 			$"{gainProportional} * {GetStandardForm_Term3(dt, timeIntegral, timeDerivative)} * {errorAtTMinus1} = " +
		// 			$"{GetStandardForm_Term1(dt, timeIntegral, timeDerivative, gainProportional, currentError)} + " +
		// 			$"{GetStandardForm_Term2(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus0)} + " +
		// 			$"{GetStandardForm_Term3(dt, timeIntegral, timeDerivative, gainProportional, errorAtTMinus1)} = " +
		// 			$"{output}");
		// 		if (updateMembers)
		// 		{
		// 			errorAtTMinus2 = errorAtTMinus1;
		// 			errorAtTMinus1 = errorAtTMinus0;
		// 			errorAtTMinus0 = currentError;
		// 			Debug.Log($"new priorErrors: [{errorAtTMinus0}, {errorAtTMinus1}, {errorAtTMinus2}]");
		// 		}
		// 		else Debug.Log($"temp priorErrors: [{currentError}, {errorAtTMinus0}, {errorAtTMinus1}]");
		// 		return output;
		// 	}
		// 	#region GetStandardForm Terms
		// 	private static float GetStandardForm_Term1(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional = 1f,
		// 		float error = 1f) => (1f + (timeIntegral != 0 ? dt / timeIntegral : 0) + timeDerivative / dt) * gainProportional * error;
		// 	private static float GetStandardForm_Term2(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional = 1f,
		// 		float error = 1f) => (-1f - 2f * timeDerivative / dt) * gainProportional * error;
		// 	private static float GetStandardForm_Term3(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional = 1f,
		// 		float error = 1f) => (timeDerivative / dt) * gainProportional * error;
		// 	private static float GetStandardForm_TermPriorIntegral(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional = 1f,
		// 		float accumulatedError = 1f) => ((timeIntegral != 0 ? dt / timeIntegral : 0)) * gainProportional * accumulatedError;
		// 	#region Coefficients
		// 	private static float GetStandardForm_CoefficientErrorAt0(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional) => (1f + (timeIntegral != 0 ? dt / timeIntegral : 0) + timeDerivative / dt) * gainProportional;
		// 	private static float GetStandardForm_CoefficientErrorAt1(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional) => (-1f - 2f * timeDerivative / dt) * gainProportional;
		// 	private static float GetStandardForm_CoefficientErrorAt2(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional) => (timeDerivative / dt) * gainProportional;
		// 	private static float GetStandardForm_CoefficientPriorIntegral(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional) => ((timeIntegral != 0 ? dt / timeIntegral : 0)) * gainProportional;
		// 	private static float GetStandardForm_CoefficientProportional(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional) => gainProportional;
		// 	private static float GetStandardForm_CoefficientIntegral(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional) => ((timeIntegral != 0 ? dt / timeIntegral : 0)) * gainProportional;
		// 	private static float GetStandardForm_CoefficientDerivative(
		// 		float dt,
		// 		float timeIntegral,
		// 		float timeDerivative,
		// 		float gainProportional) => (timeDerivative / dt) * gainProportional;
		// 	#endregion
		// 	#endregion
		// 	#endregion
		// }
		#endregion
	}

	// https://en.wikipedia.org/wiki/PID_controller#Pseudocode
	// UGLY: Clean up alternative forms.
	// UGLY: Kill priorError, use priorErrors exclusively.
	// TODO: Unity threw a fit about the name `PIDController`, therefore, `PIDControllerTemp`. Fix and rename.
	[Serializable]
	[Obsolete("Use PIDControllerState, IPIDSettings, & PIDControllerBehaviour")]
	public struct PIDControllerTemp
	{
		#region State
		public float priorError;
		public float accumulatedError;
		public float priorEstimateForDerivative;
		public float[] priorErrors;
		#endregion
		#region Settings
		public float gainProportional;
		public float gainIntegral;
		public float gainDerivative;
		public float maxAccumulatedError;
		public float minAccumulatedError;
		/// <summary>
		/// A value of 0 means there is no filtering of instantaneous error (jittery inputs are preserved); A value of 1 means there is no reaction to instantaneous error.
		/// </summary>
		private float derivativeFilterPercentage;
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
		public PIDControllerTemp(
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
		public PIDControllerTemp(
			PIDControllerTemp source,
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
			ref PIDControllerTemp source,
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
		/// <param name="minAccumulatedError">The minimum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the lower end, a value of -Infinity will result in the unaltered value.</param>
		/// <param name="maxAccumulatedError">The maximum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the upper end, a value of +Infinity will result in the unaltered value.</param>
		/// <param name="assumeTimeLessThan1"><paramref name="dt"/> appears to need to be equal to, or greater than one. Assuming <paramref name="dt"/> is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". Theoretically, this could affect integral as well. To resolve this, <paramref name="dt"/> is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, <paramref name="errorAtTMinus0"/> is divided (instead of multiplied) by <paramref name="dt"/>. If this behaviour is undesirable, pass in false to disable it.</param>
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
		/// <param name="derivativeFilterPercentage">The percentage for filtering derivative noise. Corresponds to <see cref="PIDControllerObj.DerivativeFilterPercentage"/>. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		/// <param name="assumeTimeLessThan1">dt appears to need to be equal to, or greater than one. Assuming dt is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". To resolve this, dt is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. If this behaviour is undesirable, pass in false to disable it.</param>
		/// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of dt far less than 1. If equal to NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		/// <param name="useAlternateForm">This implementation was derived from the Wikipedia entry on PID Controllers. However, it appears as though their pseudocode incorrectly calculates this term (the correct form seems to be 𝑲d * (𝓮(𝒕ₙ) - 2*𝓮(𝒕ₙ₋₁) + 𝓮(𝒕ₙ₋₂)) / Δ𝒕, but their corresponding pseudocode uses 𝑲d * 𝓮(𝒕ₙ) − 𝓮(𝒕ₙ₋₁)) / Δ𝒕. To use this (assumedly) incorrect calculation, pass in true. Note that this will render <paramref name="errorAtTMinus2"/> useless.</param>
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

		// #region Get Ideal Form Components
		// private static float GetIdealFormTermP(
		// 	float currentValue,
		// 	float desiredValue,
		// 	Func<float, float> valueAdjuster = null,
		// 	float gainProportional = 1f)
		// {
		// 	var currentError = valueAdjuster?.Invoke(desiredValue - currentValue) ?? desiredValue - currentValue;
		// 	MyMath.Validate(ref currentError);
		// 	return gainProportional * currentError;
		// }
		// #region GetIdealFormTermI
		// /// <summary>
		// /// Solves 𝑲ᵢ*₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ
		// /// </summary>
		// /// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		// /// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		// /// <param name="accumulatedError">Equivalent to ₀∫ᵗ𝓮(𝒂)𝒅𝒂 where 𝒂 = 𝒕ₙ-Δ𝒕. To emulate a discrete velocity integral, pass 0.</param>
		// /// <param name="gainIntegral">Equivalent to 𝑲ᵢ. If the base term is desired (e.g. to store the accumulated error), a value of 1 will result in the base value.</param>
		// /// <param name="minAccumulatedError">The minimum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the lower end, a value of -Infinity will result in the unaltered value.</param>
		// /// <param name="maxAccumulatedError">The maximum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the upper end, a value of +Infinity will result in the unaltered value.</param>
		// /// <param name="assumeTimeLessThan1"><paramref name="dt"> appears to need to be equal to, or greater than one. Assuming <paramref name="dt"> is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". Theoretically, this could affect integral as well. To resolve this, <paramref name="dt"> is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, <paramref name="errorAtTMinus0"/> is divided (instead of multiplied) by <paramref name="dt">. If this behaviour is undesirable, pass in false to disable it.</param>
		// /// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of <paramref name="dt"> far less than 1. If equal to NaN || +/-Infinity, <paramref name="dt"> is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		// /// <returns></returns>
		// private static float GetIdealFormTermI(
		// 	float dt,
		// 	float errorAtTMinus0,
		// 	float accumulatedError,
		// 	// Func<float, float> valueAdjuster = null,
		// 	float gainIntegral = 1f,
		// 	float minAccumulatedError = float.NegativeInfinity,
		// 	float maxAccumulatedError = float.PositiveInfinity,
		// 	bool assumeTimeLessThan1 = false,
		// 	float scaleDTBy = 1000f)
		// {
		// 	var integral = accumulatedError;// + errorAtTMinus0 * dt * (assumeTimeLessThan1 ? 1000f : 1f);
		// 	if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
		// 	if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) integral += errorAtTMinus0 / dt;
		// 	else integral += errorAtTMinus0 * dt;
		// 	MyMath.Validate(ref integral);
		// 	integral = Mathf.Clamp(integral, minAccumulatedError, maxAccumulatedError);
		// 	return gainIntegral * integral;
		// }
		// /// <summary>
		// /// Solves 𝑲ᵢ*₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ
		// /// </summary>
		// /// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		// /// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		// /// <param name="accumulatedError">Equivalent to ₀∫ᵗ𝓮(𝒂)𝒅𝒂 where 𝒂 = 𝒕ₙ-Δ𝒕; will be updated to curr+=<paramref name="errorAtTMinus0"/> * dt * (<paramref name="assumeTimeLessThan1"/> ? <paramref name="scaleDTBy"/> : 1f)</param>
		// /// <param name="gainIntegral">Equivalent to 𝑲ᵢ. If the base term is desired (i.e. to store the accumulated error), a value of 1 will result in the base value.</param>
		// /// <param name="minAccumulatedError">The minimum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the lower end, a value of -Infinity will result in the unaltered value.</param>
		// /// <param name="maxAccumulatedError">The maximum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the upper end, a value of +Infinity will result in the unaltered value.</param>
		// /// <param name="assumeTimeLessThan1"><paramref name="dt"> appears to need to be equal to, or greater than one. Assuming <paramref name="dt"> is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". Theoretically, this could affect integral as well. To resolve this, <paramref name="dt"> is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, <paramref name="errorAtTMinus0"/> is divided (instead of multiplied) by <paramref name="dt">. If this behaviour is undesirable, pass in false to disable it.</param>
		// /// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of <paramref name="dt"> far less than 1. If equal to NaN || +/-Infinity, <paramref name="dt"> is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		// /// <returns></returns>
		// private static float GetIdealFormTermI(
		// 	float dt,
		// 	float errorAtTMinus0,
		// 	ref float accumulatedError,
		// 	// Func<float, float> valueAdjuster = null,
		// 	float gainIntegral = 1f,
		// 	float minAccumulatedError = float.NegativeInfinity,
		// 	float maxAccumulatedError = float.PositiveInfinity,
		// 	bool assumeTimeLessThan1 = false,
		// 	float scaleDTBy = 1000f)
		// {
		// 	// accumulatedError += errorAtTMinus0 * dt * (assumeTimeLessThan1 ? 1000f : 1f);
		// 	if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
		// 	if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) accumulatedError += errorAtTMinus0 / dt;
		// 	else accumulatedError += errorAtTMinus0 * dt;
		// 	MyMath.Validate(ref accumulatedError);
		// 	accumulatedError = Mathf.Clamp(accumulatedError, minAccumulatedError, maxAccumulatedError);
		// 	return gainIntegral * accumulatedError;
		// }
		// #endregion
		// #region GetIdealFormTermD
		// /// <summary>
		// /// Solves 𝑲d*(𝒅𝓮(𝒕ₙ))/(𝒅𝒕ₙ)
		// /// </summary>
		// /// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		// /// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		// /// <param name="errorAtTMinus1">Equivalent to 𝓮(𝒕ₙ₋₁); the prior error</param>
		// /// <param name="errorAtTMinus2">Equivalent to 𝓮(𝒕ₙ₋₂); the error before last</param>
		// /// <param name="priorEstimateForDerivative">Equivalent to (𝒅𝓮(𝒕ₙ₋₁))/(𝒅𝒕ₙ₋₁) filtered for high frequency noise. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		// /// <param name="gainDerivative">Equivalent to 𝑲d. If the base term is desired (i.e. to store the filtered derivative for filtering), a value of 1 will result in the base value.</param>
		// /// <param name="derivativeFilterPercentage">The percentage for filtering derivative noise. Corresponds to <see cref="PIDControllerTemp.DerivativeFilterPercentage"/>. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		// /// <param name="assumeTimeLessThan1">dt appears to need to be equal to, or greater than one. Assuming dt is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". To resolve this, dt is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. If this behaviour is undesirable, pass in false to disable it.</param>
		// /// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of dt far less than 1. If equal to NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		// /// <param name="useAlternateForm">This implementation was derived from the Wikipedia entry on PID Controllers. However, it appears as though their pseudocode incorrectly calculates this term (the correct form seems to be 𝑲d * (𝓮(𝒕ₙ) - 2*𝓮(𝒕ₙ₋₁) + 𝓮(𝒕ₙ₋₂)) / Δ𝒕, but their corresponding pseudocode uses 𝑲d * 𝓮(𝒕ₙ) − 𝓮(𝒕ₙ₋₁)) / Δ𝒕. To use this (assumedly) incorrect calculation, pass in true. Note that this will render <paramref name="errorAtTMinus2"/> useless.</param>
		// /// <returns></returns>
		// private static float GetIdealFormTermD(
		// 	float dt,
		// 	float errorAtTMinus0,
		// 	float errorAtTMinus1,
		// 	float errorAtTMinus2,
		// 	float priorEstimateForDerivative = 0f,
		// 	float gainDerivative = 1f,
		// 	float derivativeFilterPercentage = 0f,
		// 	bool assumeTimeLessThan1 = true,
		// 	float scaleDTBy = 1000f,
		// 	bool useAlternateForm = false)
		// {
		// 	// var derivative = useAlternateForm ? (errorAtTMinus0 - errorAtTMinus1) / dt : (errorAtTMinus0 - 2f * errorAtTMinus1 + errorAtTMinus2) / dt;
		// 	var derivative = (errorAtTMinus0 - (useAlternateForm ? errorAtTMinus1 : 2f * errorAtTMinus1 - errorAtTMinus2));
		// 	if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
		// 	if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) derivative *= dt;
		// 	else derivative /= dt;
		// 	MyMath.Validate(ref derivative);
		// 	var filteredDerivative = derivativeFilterPercentage * priorEstimateForDerivative + (1f - derivativeFilterPercentage) * derivative;
		// 	MyMath.Validate(ref filteredDerivative);
		// 	return gainDerivative * filteredDerivative;
		// }
		// /// <summary>
		// /// Solves 𝑲d*(𝒅𝓮(𝒕ₙ))/(𝒅𝒕ₙ)
		// /// </summary>
		// /// <param name="dt">Equivalent to Δ𝒕 = 𝒕ₙ - 𝒕ₚ, where 𝒕ₙ = the current time and 𝒕ₚ = the time when the algorithm was previously ran</param>
		// /// <param name="errorAtTMinus0">Equivalent to 𝓮(𝒕ₙ); the current error</param>
		// /// <param name="errorAtTMinus1">Equivalent to 𝓮(𝒕ₙ₋₁); the prior error</param>
		// /// <param name="errorAtTMinus2">Equivalent to 𝓮(𝒕ₙ₋₂); the error before last</param>
		// /// <param name="priorEstimateForDerivative">Equivalent to (𝒅𝓮(𝒕ₙ₋₁))/(𝒅𝒕ₙ₋₁) filtered for high frequency noise. If not filtering the derivative, a value of 0 will result in the unfiltered value. Will be updated.</param>
		// /// <param name="gainDerivative">Equivalent to 𝑲d. If the base term is desired (i.e. to store the filtered derivative for filtering), a value of 1 will result in the base value.</param>
		// /// <param name="derivativeFilterPercentage">The percentage for filtering derivative noise. Corresponds to <see cref="PIDControllerTemp.DerivativeFilterPercentage"/>. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		// /// <param name="assumeTimeLessThan1">dt appears to need to be equal to, or greater than one. Assuming dt is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". To resolve this, dt is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. If this behaviour is undesirable, pass in false to disable it.</param>
		// /// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of dt far less than 1. If equal to NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		// /// <param name="useAlternateForm">This implementation was derived from the Wikipedia entry on PID Controllers. However, it appears as though their pseudocode incorrectly calculates this term (the correct form seems to be 𝑲d * (𝓮(𝒕ₙ) - 2*𝓮(𝒕ₙ₋₁) + 𝓮(𝒕ₙ₋₂)) / Δ𝒕, but their corresponding pseudocode uses 𝑲d * 𝓮(𝒕ₙ) − 𝓮(𝒕ₙ₋₁)) / Δ𝒕. To use this (assumedly) incorrect calculation, pass in true. Note that this will render <paramref name="errorAtTMinus2"/> useless.</param>
		// /// <returns></returns>
		// private static float GetIdealFormTermD(
		// 	float dt,
		// 	float errorAtTMinus0,
		// 	float errorAtTMinus1,
		// 	float errorAtTMinus2,
		// 	ref float priorEstimateForDerivative,
		// 	float gainDerivative = 1f,
		// 	float derivativeFilterPercentage = 0f,
		// 	bool assumeTimeLessThan1 = true,
		// 	float scaleDTBy = 1000f,
		// 	bool useAlternateForm = false)
		// {
		// 	// var derivative = useAlternateForm ? (errorAtTMinus0 - errorAtTMinus1) / dt : (errorAtTMinus0 - 2f * errorAtTMinus1 + errorAtTMinus2) / dt;
		// 	var derivative = (errorAtTMinus0 - (useAlternateForm ? errorAtTMinus1 : 2f * errorAtTMinus1 - errorAtTMinus2));
		// 	if (assumeTimeLessThan1 && float.IsFinite(scaleDTBy)) dt *= scaleDTBy;
		// 	if (assumeTimeLessThan1 && !float.IsFinite(scaleDTBy)) derivative *= dt;
		// 	else derivative /= dt;
		// 	MyMath.Validate(ref derivative);
		// 	var filteredDerivative = derivativeFilterPercentage * priorEstimateForDerivative.Validate() + (1f - derivativeFilterPercentage) * derivative;
		// 	MyMath.Validate(ref filteredDerivative);
		// 	return gainDerivative * filteredDerivative;
		// }
		// #endregion
		// #endregion
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
	[Serializable]
	public struct PIDControllerState
	{
		#region State
		public float accumulatedError;
		public float priorEstimateForDerivative;
		public float[] priorErrors;
		public float CurrentError { get => priorErrors[0]; set => priorErrors[0] = value; }
		public float PriorError { get => priorErrors[1]; set => priorErrors[1] = value; }
		public float ErrorBeforeLast { get => priorErrors[2]; set => priorErrors[2] = value; }
		public float ErrorAtTMinus0 { get => priorErrors[0]; set => priorErrors[0] = value; }
		public float ErrorAtTMinus1 { get => priorErrors[1]; set => priorErrors[1] = value; }
		public float ErrorAtTMinus2 { get => priorErrors[2]; set => priorErrors[2] = value; }
		public float this[int index] { get => priorErrors[index]; set => priorErrors[index] = value; }
		#endregion
		#region Ctors
		// public PIDControllerState()
		// {
		// 	this.priorError = this.accumulatedError = this.priorEstimateForDerivative = 0f;
		// 	this.priorErrors = new float[3];
		// }
		public PIDControllerState(
			float[] priorErrors = null,
			float accumulatedError = float.NaN,
			float priorEstimateForDerivative = float.NaN)
		{
			this.accumulatedError = float.IsNaN(accumulatedError) ? 0f : accumulatedError;
			this.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? 0f : priorEstimateForDerivative;
			this.priorErrors = priorErrors ?? new float[3];
		}
		public PIDControllerState(
			PIDControllerState source,
			float[] priorErrors = null,
			float accumulatedError = float.NaN,
			float priorEstimateForDerivative = float.NaN)
		{
			this.priorErrors = priorErrors ?? source.priorErrors;
			this.accumulatedError = float.IsNaN(accumulatedError) ? source.accumulatedError : accumulatedError;
			this.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? source.priorEstimateForDerivative : priorEstimateForDerivative;
		}
		public PIDControllerState(
			PIDControllerTemp source,
			float[] priorErrors = null,
			float accumulatedError = float.NaN,
			float priorEstimateForDerivative = float.NaN)
		{
			this.priorErrors = priorErrors ?? source.priorErrors;
			this.accumulatedError = float.IsNaN(accumulatedError) ? source.accumulatedError : accumulatedError;
			this.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? source.priorEstimateForDerivative : priorEstimateForDerivative;
		}
		#endregion
		public void ResetControllerState(
			float[] priorErrors = null,
			float accumulatedError = float.NaN,
			float priorEstimateForDerivative = float.NaN)
		{
			this.priorErrors = priorErrors ?? new float[3] { 0, 0, 0 };
			this.accumulatedError = float.IsNaN(accumulatedError) ? 0f : accumulatedError;
			this.priorEstimateForDerivative = float.IsNaN(priorEstimateForDerivative) ? 0f : priorEstimateForDerivative;
		}
	}
	public interface IPIDSettings
	{
		#region Settings
		float MaxAccumulatedError { get; set; }
		float MinAccumulatedError { get; set; }
		float GainDerivative { get; set; }
		float GainIntegral { get; set; }
		float GainProportional { get; set; }
		// /// <summary>
		// /// A value of 0 means there is no filtering of instantaneous error (jittery inputs are preserved); A value of 1 means there is no reaction to instantaneous error.
		// /// </summary>
		// private float derivativeFilterPercentage;
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
		/// Changing <seealso cref="DerivativeFilterPercentage"/> to 1 means any stored change will be continuously applied, easily leading to drastic and unpredictable behaviour.
		/// Any attempts to set this value to 1 will be circumvented.
		/// </remarks>
		float DerivativeFilterPercentage { get; set; }
		// {
		// 	get => derivativeFilterPercentage;
		// 	set => derivativeFilterPercentage = Mathf.Abs((value >= 1f && value < 100f) || (value <= -1f && value > -100f) ? value / 100f : value % 1f);
		// 	// set {
		// 	// 	value >= 1f && value < 100f ? value / 100f : Mathf.Clamp01(value);
		// 	// 	if (derivativeFilterPercentage == 1f)
		// 	// 		derivativeFilterPercentage = 0f;
		// 	// }
		// }
		static readonly Func<float, float, float> defaultDerivativeFilterPercentageSetter = (newValue, oldValue) => Mathf.Abs((newValue >= 1f && newValue < 100f) || (newValue <= -1f && newValue > -100f) ? newValue / 100f : newValue % 1f);
		float TimeDerivative
		{
			get => (GainProportional == 0) ? 0 : GainDerivative / GainProportional;
			set => GainDerivative = GainProportional * value;
		}
		float TimeIntegral
		{
			get => (GainIntegral == 0) ? 0 : GainProportional / GainIntegral;
			set => GainIntegral = (value == 0) ? 0 : GainProportional / value;
		}
		Func<float, float, float, float, Tuple<float, float, float, float>> JointErrorAdjusterOld { get; set; }
		Func<float, float, float, Tuple<float, float, float>> JointErrorAdjuster { get; set; }
		Func<float, float> IndividualErrorAdjuster { get; set; }
		Func<float, float, float, float, float, Tuple<float, float, float, float, float>> JointAccumulatedErrorAdjusterOld { get; set; }
		Func<float, float, float, float, Tuple<float, float, float, float>> JointAccumulatedErrorAdjuster { get; set; }
		Func<float, float> AccumulatedErrorAdjuster { get; set; }
		// public enum ErrorAdjuster
		// {
		// 	None = 0,
		// 	Joint = 1,
		// 	JointOld = 2,
		// }
		// abstract public ErrorAdjuster[] CachedErrorAdjuster { get; set; }
		// abstract public ErrorAdjuster[] AccumulatedErrorAdjuster { get; set; }
		bool IsDerivativeFiltered => DerivativeFilterPercentage != 0;
		bool IsDerivativeApplied => GainDerivative != 0;
		bool IsIntegralApplied => GainIntegral != 0;
		#endregion
		float Calculate(
			PIDControllerState state,
			float currentValue,
			float desiredValue,
			float dt,
			bool useDiscreteVelocityFormForIntegral = false,
			bool correctTimeScaleForIntegral = true,
			float timeScalerForIntegral = 1000f,
			bool correctTimeScaleForDerivative = true,
			float timeScalerForDerivative = 1000f,
			bool useAlternateDerivativeForm = false) =>
			PIDControllerBehaviour.CalculateIdealForm(state, this, currentValue, desiredValue, dt, useDiscreteVelocityFormForIntegral, correctTimeScaleForIntegral, timeScalerForIntegral, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm);
		#region Default Error Adjusters
		public float WrapAngularError(float angularError)
		{
			if (angularError <= 180 && angularError >= -180)
				return angularError;
			angularError %= 360f;
			if (angularError < 0)
				angularError += 360;
			if (angularError > 180)
				return angularError - 360;
			// // Should be unnecessary, as the above code should ensure 0 <= angularError < 360.
			// if (angularError < -180)
			// 	return angularError + 360;
			if (!float.IsFinite(angularError))
				return 0;
			else if (angularError > 180 || angularError < -180)
				Debug.LogWarning($"Something's wrong with this function that should be perfect; {angularError} > 180 || {angularError} < -180; it shouldn't be.");
			return angularError;
		}
		public Tuple<float, float, float> WrapAngularError(float errorAtTMinus0, float errorAtTMinus1, float errorAtTMinus2)
		{
			errorAtTMinus0 = WrapAngularError(errorAtTMinus0);
			errorAtTMinus1 = WrapAngularError(errorAtTMinus1);
			errorAtTMinus2 = WrapAngularError(errorAtTMinus2);
			return new Tuple<float, float, float>(errorAtTMinus0, errorAtTMinus1, errorAtTMinus2);
		}
		public Tuple<float, float, float, float> WrapAngularError(float errorAtTMinus0, float errorAtTMinus1, float errorAtTMinus2, float errorAtTMinus3)
		{
			errorAtTMinus0 = WrapAngularError(errorAtTMinus0);
			errorAtTMinus1 = WrapAngularError(errorAtTMinus1);
			errorAtTMinus2 = WrapAngularError(errorAtTMinus2);
			errorAtTMinus3 = WrapAngularError(errorAtTMinus3);
			return new Tuple<float, float, float, float>(errorAtTMinus0, errorAtTMinus1, errorAtTMinus2, errorAtTMinus3);
		}
		#endregion
	}
	public interface IPIDSettingsFull : IPIDSettings
	{
		#region Settings
		bool UseDiscreteVelocityFormForIntegral { get; set; }
		bool CorrectTimeScaleForIntegral { get; set; }
		float TimeScalerForIntegral { get; set; }
		bool CorrectTimeScaleForDerivative { get; set; }
		float TimeScalerForDerivative { get; set; }
		bool UseAlternateDerivativeForm { get; set; }
		#endregion
		#region Properties
		// public enum ErrorAdjuster
		// {
		// 	None = 0,
		// 	Joint = 1,
		// 	JointOld = 2,
		// }
		// ErrorAdjuster[] CachedErrorAdjuster { get; set; }
		// ErrorAdjuster[] AccumulatedErrorAdjuster { get; set; }
		#endregion
		float Calculate(
			PIDControllerState state,
			float currentValue,
			float desiredValue,
			float dt) =>
			PIDControllerBehaviour.CalculateIdealForm(state, this, currentValue, desiredValue, dt, UseDiscreteVelocityFormForIntegral, CorrectTimeScaleForIntegral, TimeScalerForIntegral, CorrectTimeScaleForDerivative, TimeScalerForDerivative, UseAlternateDerivativeForm);
	}
	public static class PIDControllerBehaviour
	{
		// TODO: Isn't really dependant on Ideal form from caller's perspective, refactor.
		public static float CalculateIdealForm(
			ref PIDControllerState state,
			IPIDSettings ini,
			float currentValue,
			float desiredValue,
			float dt,
			bool useDiscreteVelocityFormForIntegral = false,
			bool correctTimeScaleForIntegral = true,
			float timeScalerForIntegral = 1000f,
			bool correctTimeScaleForDerivative = true,
			float timeScalerForDerivative = 1000f,
			bool useAlternateDerivativeForm = false)
		{
			var currentError = GetIdealFormTermP(currentValue, desiredValue);
			if (ini.JointErrorAdjusterOld != null)
				(state[0], state[1], state[2], _) = ini.JointErrorAdjusterOld?.Invoke(currentError, state[0], state[1], state[2]) ?? new Tuple<float, float, float, float>(state[0], state[1], state[2], float.NaN);
			else if (ini.JointErrorAdjuster != null)
				(state[0], state[1], state[2]) = ini.JointErrorAdjuster?.Invoke(currentError, state[0], state[1]) ?? new Tuple<float, float, float>(state[0], state[1], state[2]);
			else
			{
				state[2] = ini.IndividualErrorAdjuster?.Invoke(state[1]) ?? state[1];
				state[1] = ini.IndividualErrorAdjuster?.Invoke(state[0]) ?? state[0];
				state[0] = ini.IndividualErrorAdjuster?.Invoke(currentError) ?? currentError;
			}
			var finalIntegral = useDiscreteVelocityFormForIntegral ?
				GetIdealFormTermI(
					dt,
					state[0],
					0,
					ini.GainIntegral,
					ini.MinAccumulatedError,
					ini.MaxAccumulatedError,
					correctTimeScaleForIntegral,
					timeScalerForIntegral) :
				GetIdealFormTermI(
					dt,
					state[0],
					ref state.accumulatedError,
					ini.GainIntegral,
					ini.MinAccumulatedError,
					ini.MaxAccumulatedError,
					correctTimeScaleForIntegral,
					timeScalerForIntegral);
			var finalDerivative = GetIdealFormTermD(
				dt,
				state[0],
				state[1],
				state[2],
				ref state.priorEstimateForDerivative,
				ini.GainDerivative,
				ini.DerivativeFilterPercentage,
				correctTimeScaleForDerivative,
				timeScalerForDerivative,
				useAlternateDerivativeForm);
			return ini.GainProportional * state[0] + finalIntegral + finalDerivative;
		}
		public static float CalculateIdealForm(
			PIDControllerState state,
			IPIDSettings ini,
			float currentValue,
			float desiredValue,
			float dt,
			bool useDiscreteVelocityFormForIntegral = false,
			bool correctTimeScaleForIntegral = true,
			float timeScalerForIntegral = 1000f,
			bool correctTimeScaleForDerivative = true,
			float timeScalerForDerivative = 1000f,
			bool useAlternateDerivativeForm = false) => CalculateIdealForm(ref state, ini, currentValue, desiredValue, dt, useDiscreteVelocityFormForIntegral, correctTimeScaleForIntegral, timeScalerForIntegral, correctTimeScaleForDerivative, timeScalerForDerivative, useAlternateDerivativeForm);
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
		/// <param name="minAccumulatedError">The minimum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the lower end, a value of -Infinity will result in the unaltered value.</param>
		/// <param name="maxAccumulatedError">The maximum value of ₀∫ˣ𝓮(𝒕ₙ)𝒅𝒕ₙ. If not bounding the upper end, a value of +Infinity will result in the unaltered value.</param>
		/// <param name="assumeTimeLessThan1"><paramref name="dt"/> appears to need to be equal to, or greater than one. Assuming <paramref name="dt"/> is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". Theoretically, this could affect integral as well. To resolve this, <paramref name="dt"/> is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, <paramref name="errorAtTMinus0"/> is divided (instead of multiplied) by <paramref name="dt"/>. If this behaviour is undesirable, pass in false to disable it.</param>
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
		/// <param name="derivativeFilterPercentage">The percentage for filtering derivative noise. Corresponds to <see cref="PIDControllerObj.DerivativeFilterPercentage"/>. If not filtering the derivative, a value of 0 will result in the unfiltered value.</param>
		/// <param name="assumeTimeLessThan1">dt appears to need to be equal to, or greater than one. Assuming dt is in seconds, using values like .01666 (for 60 fps) gives extremely large derivative values which are many orders of magnitude larger than "proportional" or "integral". To resolve this, dt is multiplied by the value of <paramref name="scaleDTBy"/> to convert the seconds back to the base unit (assumedly milliseconds). If <paramref name="scaleDTBy"/> = NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. If this behaviour is undesirable, pass in false to disable it.</param>
		/// <param name="scaleDTBy">The value to scale <paramref name="dt"/> by to resolve errors with values of dt far less than 1. If equal to NaN || +/-Infinity, dt is multiplied to the numerator instead of dividing. Does nothing unless <paramref name="assumeTimeLessThan1"/> is true.</param>
		/// <param name="useAlternateForm">This implementation was derived from the Wikipedia entry on PID Controllers. However, it appears as though their pseudocode incorrectly calculates this term (the correct form seems to be 𝑲d * (𝓮(𝒕ₙ) - 2*𝓮(𝒕ₙ₋₁) + 𝓮(𝒕ₙ₋₂)) / Δ𝒕, but their corresponding pseudocode uses 𝑲d * 𝓮(𝒕ₙ) − 𝓮(𝒕ₙ₋₁)) / Δ𝒕. To use this (assumedly) incorrect calculation, pass in true. Note that this will render <paramref name="errorAtTMinus2"/> useless.</param>
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

		#region Obsolete
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