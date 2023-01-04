# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added

### Fixed

### Changed

### Removed


## [2.0.1] - 2023-1-3
### Added
- Added `Util` class for non-math, non-extension methods.
- Added `GetMemberName` & related methods to help me make code more resilient to refactoring.

## [2.0.0] - 2023-1-2
### Added
- Added [PIDController](https://en.wikipedia.org/wiki/PID_controller) for self-correcting smooth adjustments to an output value based on a desired value. - 2022-12-11
- Added `public static int Wrap(int value, int minInclusive, int maxInclusive)`. - 2022-12-15
- `public static bool Validate(ref float value, float substitute = 0f)` and `public static float Validate(float value, float substitute = 0f)` to `ExtensionMethods` and `MyMath`. MUST DETERMINE BEST PLACE & VIABILITY. - 2022-12-15
- `public static bool IsValid(this float value, float minInclusive = float.MinValue, float maxInclusive = float.MaxValue)` to `ExtensionMethods`. MUST DETERMINE BEST PLACE. - 2022-12-15
- `public static bool Approximately` to `ExtensionMethods`. MUST UNIT TEST AND DETERMINE BEST PLACE. - 2022-12-17
- Added `To<T>` and `As<T>` to `ExtensionMethods` for convenient casting. - 2022-12-31

### Fixed
- Modified `ConstructInterpolatorFunction(float[] xValues, float[] yValues)` to preserve monotonicity by modifying the conditional check at coefficient 1 initialization. REQUIRES TESTING. STILL MAY NOT ENSURE MONOTONICITY. - 2022-12-11
- Modified `Wrap` functions to guarantee value will fall within specified range. NEEDS UNIT TESTING. - 2022-12-15
- Changed all functions to the correct spelling of interpolator.

### Changed
- Modified `ConstructInterpolatorFunction(float[] xValues, float[] yValues)` to accept an optional parameter defining the desired slope direction of the resultant function, with a value of 0 (the default) or NaN returning defaulting to the first signed slope found in the inputs. REQUIRES TESTING. STILL MAY NOT ENSURE MONOTONICITY. - 2022-12-12
- Changed `ConstructInterpolatorFunction(float[] xValues, float[] yValues)` to `ConstructInterpolatorFunction(IList<float> xValues, IList<float> yValues)` for compatibility w/ Lists, Arrays, etc and removed `ConstructInterpolatorFunction(List<float> xValues, List<float> yValues)`. - 2022-12-22
- Changed `ConstructInterpolatorFunctionDouble(double[] xValues, double[] yValues)` to `ConstructInterpolatorFunction(double[] xValues, double[] yValues)`. - 2022-12-23
- Changed `ConstructInterpolatorFunction(double[] xValues, double[] yValues)` to `ConstructInterpolatorFunction(IList<double> xValues, IList<double> yValues)` for compatibility w/ Lists, Arrays, etc. - 2022-12-22
- Updated `ConstructInterpolatorFunction(double[] xValues, double[] yValues)` to achieve parity w/ `ConstructInterpolatorFunction(IList<float> xValues, IList<float> yValues)`. - 2022-12-22
- Changed `ConstructInterpolatorFunction(Vector2[] xyValues)` to `ConstructInterpolatorFunction(IList<Vector2> xyValues)` for compatibility w/ Lists, Arrays, etc. - 2022-12-22

### Removed
- `public static float Wrap(float value, int minInclusive, int maxInclusive)`, as it's functionally impossible (i.e. wrapping 2 to the range [0,1] would yield 1, but 2.̅01 would be 0. Functionally, 1 of the bounds must be exclusive for float values); replaced with `public static int Wrap(int value, int minInclusive, int maxInclusive)`. - 2022-12-15
- Removed `ConstructInterpolatorFunction(List<float> xValues, List<float> yValues)`; Use `ConstructInterpolatorFunction(IList<float> xValues, IList<float> yValues)`. - 2022-12-22

## [1.0.3] - 2022-10-24
### Added
- New Array manipulation extension methods RemoveAt

### Fixed
- Fixed bug in SlideElementsUp due to bad conditional check.