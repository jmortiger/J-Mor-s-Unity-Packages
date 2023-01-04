# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.1]
### Added
 - Cached versions of the 3 core fields that run `PropertyInspectorObject`. This allows disasters to be recovered from (assuming you accidentally saved already), although it will be quite tedious.
## [0.2.0]
### Added
 - `InspectorNameAttribute` alias.
 - `HeaderAttribute` alias.
 - Support for both `HeaderAttribute` & `InspectorNameAttribute` aliases.
 - Added `tooltip` to `HeaderAttribute` alias and support for it in the property drawer.
 - Dependencies to package.json.
 - Added complete replacement versions of all currently aliased attributes. These don't rely on inheritance, and thus are more stable. Currently unsupported.
### Changed
 - All aliases now derived from the type they are aliases of.
 - Changed how `GetPropertyHeight` determines property height.
### Fixed
 - `SpaceAttribute` support: multiple instances on 1 member were ignored, now they aren't.
 - `SpaceAttribute` and `HeaderAttribute` now respect `PropertyAttribute.order`.
 - `EditorGUIUtility.standardVerticalSpacing` now taken into account for spacing and property height.
 - Property drawer breaking upon property order change.
### Removed
 - Removed `IHasPropertyInspectors` and `PropertyInspectorDrawer` - so much has changed, it's best to redo them entirely later than keep them as-is.

## [0.1.0]
### Added
- Support for Space, HideInInspector, and Tooltip attributes
### Fixed
 - Modifying values affected by the property in the inspector by using something other than the property will no longer get overwritten by the property.

## [0.0.1] - 2022-10-24
### Added
- Naive Implementation