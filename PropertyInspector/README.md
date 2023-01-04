# Property Inspector

## Summary
Allows for (certain types of) properties to be viewed and edited in the inspector, complete with Undo support. Requires the [Utilities package](https://github.com/jmortiger/J-Mor-s-Unity-Packages.git?path=/Utility).

Full package suite: https://github.com/jmortiger/J-Mor-s-Unity-Packages

## USAGE NOTES
To allow a MonoBehaviour/ScriptableObject class to show properties in the inspector, follow these steps:
### Class Setup
1. Add a `PropertyInspectorObject` ***instanced*** ***field*** to the class whose properties should appear in the inspector.
	* Its name and visibility doesn't (or at least shouldn't) matter, but it ***must be instanced*** (i.e. not `static`) and it ***must be a field*** (i.e. not a property nor a method).
	* Example: `public PropertyInspectorObject inspectorObject;`
2. Add a `[SerializeReference]` attribute to this field.
	* Unity, by default, doesn't serialize classes that don't inherit from `UnityEngine.Object` by reference. However, [only built-in classes (and structs?) can inherit from `UnityEngine.Object`](http://answers.unity.com/answers/646791/view.html).[^1] As a result, I can't simply have `PropertyInspectorObject` inherit from `UnityEngine.Object`. So we'll have to use `[SerializeReference]`. This will allow us to serialize this field as a managed reference.
	* This has the added benefit of allowing this field to be non-public if you so choose; this attribute will serialize it (and let the property drawer reference it later) regardless.
	* Example: `[SerializeReference] public PropertyInspectorObject inspectorObject;` or
		```cs
		[SerializeReference]
		public PropertyInspectorObject inspectorObject;
		```
3. Add the `OnValidate` Unity message to the class.
	* This was chosen because:
		* This is available on both `MonoBehaviour`s and `ScriptableObject`s.
		* This will be called between inspector updates.
4. Anywhere in the `OnValidate` method, check if the `PropertyInspectorObject` is `null`, and initialize it if it is, passing the object it's a field on (e.g. `this`) and the type of that object (e.g. `this.GetType()`) to the constructor.
	* Example: 
		```cs
		[SerializeReference]
		public PropertyInspectorObject inspectorObject;
		void OnValidate()
		{
			if (inspectorObject == null)
				inspectorObject = new PropertyInspectorObject(this, this.GetType());
		}
		```
5. Still in `OnValidate`, if the `PropertyInspectorObject` was initialized, then check if its `container` is `null`, and set it equal to the object it's a field on (e.g. `this`) if it is.
	* Example: 
		```cs
		[SerializeReference]
		public PropertyInspectorObject inspectorObject;
		void OnValidate()
		{
			if (inspectorObject == null)
				inspectorObject = new PropertyInspectorObject(this, this.GetType());
			else if (inspectorObject.container == null)
				inspectorObject.container = this;
		}
		```
6. Though it shouldn't be necessary, you can also check that the type is correctly assigned as well.
	* If this falls out of sync, it's likely an indicator of bigger problems or refactoring (e.g. renaming the class).
	* Example:
		```cs
		[SerializeReference]
		public PropertyInspectorObject inspectorObject;
		void OnValidate()
		{
			if (inspectorObject == null)
				inspectorObject = new PropertyInspectorObject(this, this.GetType());
			else
			{
				if (inspectorObject.container == null)
					inspectorObject.container = this;
				if (inspectorObject.containerType != this.GetType())
					inspectorObject.containerType = this.GetType();
			}
		}
		```

Your class should now be correctly set up for PropertyInspectors. The `PropertyInspectorObject` and its corresponding `PropertyInspectorObjectDrawer` takes care of everything else. The only thing you need to do is tell it what you want in the inspector using attributes.

### Property Setup

For each property you want in the inspector, do the following: 

1. Make sure it should work correctly with the inspector.
	* Example: 
		```cs
		private float derivativeFilterPercentage = 0f; // Not serialized/in inspector
		/// <summary>
		/// Clamps values between 0 and 1, excluding 1.
		/// If a negative value is entered, takes the absolute value.
		/// If a value >= 1 and < 100 is entered, assumes percentage, takes value divided by 100.
		/// </summary>
		public float DerivativeFilterPercentage
		{
			get => derivativeFilterPercentage;
			set => derivativeFilterPercentage = Mathf.Abs((value >= 1f && value < 100f) || (value <= -1f && value > -100f) ? value / 100f : value % 1f);
		}
		```
2. Add the `[PropertyInspector]` attribute to the property to tell the `PropertyInspectorObject` it should be included.
	* Example:
		```cs
		private float derivativeFilterPercentage = 0f; // Not serialized/in inspector
		/// <summary>
		/// Clamps values between 0 and 1, excluding 1.
		/// If a negative value is entered, takes the absolute value.
		/// If a value >= 1 and greater than 100 is entered, assumes percentage, takes value divided by 100.
		/// </summary>
		[PropertyInspector]
		public float DerivativeFilterPercentage
		{
			get => derivativeFilterPercentage;
			set => derivativeFilterPercentage = Mathf.Abs((value >= 1f && value < 100f) || (value <= -1f && value > -100f) ? value / 100f : value % 1f);
		}
		```
3. For now, that's it. There are plans for things like showing labels for readonly/getter-only properties that will require more, but for now, that's it.

### Using Built-in UnityEngine attributes
The following UnityEngine attributes show what are and are not supported by the Property Inspector Drawer.

Legend:

| Yes | No | Maybe/Not Yet |
| :---: | :---: | :---: |
| ✔️ | ❌ | ❓ |

| Attribute | Support Status | Planned Support Status | Allowed on Properties Directly |  Allowed on Properties Using Workaround | Notes |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **[AddComponentMenu](https://docs.unity3d.com/ScriptReference/AddComponentMenu)** | Unsupported❌ | Unsupported❌ | ✔️ | ❌ | Only affects classes by default. No effect on properties. |
| **[AssemblyIsEditorAssembly](https://docs.unity3d.com/ScriptReference/AssemblyIsEditorAssembly)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on assemblies by default. No effect on properties. |
| **[BeforeRenderOrderAttribute](https://docs.unity3d.com/ScriptReference/BeforeRenderOrderAttribute)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on methods by default. No effect on properties. |
| **[ColorUsageAttribute](https://docs.unity3d.com/ScriptReference/ColorUsageAttribute)** | Unsupported❌ | Supported✔️ | ❌ | ❓ | Only valid on fields by default. |
| **[ContextMenu](https://docs.unity3d.com/ScriptReference/ContextMenu)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on methods by default. No effect on properties. |
| **[ContextMenuItemAttribute](https://docs.unity3d.com/ScriptReference/ContextMenuItemAttribute)** | Unsupported❌ | Supported✔️ | ❌ | ❓ | Only valid on fields by default. Might require more work on user's end than the default. |
| **[CreateAssetMenuAttribute](https://docs.unity3d.com/ScriptReference/CreateAssetMenuAttribute)** | Unsupported❌ | Maybe❓ | ❌ | ❓ | Only valid on classes by default. No effect on properties. Might be used to create scriptable objects? Definitely won't work like the default. |
| **[CustomGridBrushAttribute](https://docs.unity3d.com/ScriptReference/CustomGridBrushAttribute)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on classes by default. No effect on properties. |
| **[DelayedAttribute](https://docs.unity3d.com/ScriptReference/DelayedAttribute)** | Unsupported❌ | Maybe❓ | ❌ | ❓ | Only valid on fields by default. Requires testing |
| **[DisallowMultipleComponent](https://docs.unity3d.com/ScriptReference/DisallowMultipleComponent)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on classes by default. No effect on properties. |
| **[ExcludeFromObjectFactoryAttribute](https://docs.unity3d.com/ScriptReference/ExcludeFromObjectFactoryAttribute)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on classes by default. No effect on properties. |
| **[ExcludeFromPresetAttribute](https://docs.unity3d.com/ScriptReference/ExcludeFromPresetAttribute)** | Unsupported❌ | Maybe❓ | ❌ | ❓ | Only valid on classes by default. No effect on properties. Might use to exclude fields from presets? |
| **[ExecuteAlways](https://docs.unity3d.com/ScriptReference/ExecuteAlways)** | IDK❓ | IDK❓ | ✔️ | ❌ | Requires testing. |
| **[ExecuteInEditMode](https://docs.unity3d.com/ScriptReference/ExecuteInEditMode)** | IDK❓ | IDK❓ | ✔️ | ❌ | Requires testing. |
| **[GradientUsageAttribute](https://docs.unity3d.com/ScriptReference/GradientUsageAttribute)** | Unsupported❌ | Supported✔️ | ❌ | ❓ | Only valid on classes by default. No effect on properties. |
| **[GUITargetAttribute](https://docs.unity3d.com/ScriptReference/GUITargetAttribute)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on methods by default. No effect on properties. |
| **[HeaderAttribute](https://docs.unity3d.com/ScriptReference/HeaderAttribute)** | Supported✔️ | Supported✔️ | ❌ | ✔️ | Only valid on fields by default. |
| **[HelpURLAttribute](https://docs.unity3d.com/ScriptReference/HelpURLAttribute)** | Unsupported❌ | Maybe❓ | ❌ | ❓ | Only valid on classes by default. No effect on properties. Might allow? |
| **[HideInInspector](https://docs.unity3d.com/ScriptReference/HideInInspector)** | Supported✔️ | Supported✔️ | ✔️ | ❓ | - |
| **[IconAttribute](https://docs.unity3d.com/ScriptReference/IconAttribute)** | Unsupported❌ | Maybe❓ | ❌ | ❓ | Only valid on classes by default. No effect on properties. |
| **[ImageEffectAfterScale](https://docs.unity3d.com/ScriptReference/ImageEffectAfterScale)** | Unsupported❌ | Supported✔️ | ✔️ | ❓ | No effect on properties. |
| **[ImageEffectAllowedInSceneView](https://docs.unity3d.com/ScriptReference/ImageEffectAllowedInSceneView)** | Unsupported❌ | Supported✔️ | ✔️ | ❓ | - |
| **[ImageEffectOpaque](https://docs.unity3d.com/ScriptReference/ImageEffectOpaque)** | Unsupported❌ | Supported✔️ | ✔️ | ❓ | No effect on properties. |
| **[ImageEffectTransformsToLDR](https://docs.unity3d.com/ScriptReference/ImageEffectTransformsToLDR)** | Unsupported❌ | Supported✔️ | ✔️ | ❓ | - |
| **[ImageEffectUsesCommandBuffer](https://docs.unity3d.com/ScriptReference/ImageEffectUsesCommandBuffer)** | Unsupported❌ | Supported✔️ | ❌ | ❌ | Only valid on method declarations. |
| **[InspectorNameAttribute](https://docs.unity3d.com/ScriptReference/InspectorNameAttribute)** | Supported✔️ | Supported✔️ | ❌ | ✔️ | Only valid on fields by default. |
| **[MinAttribute](https://docs.unity3d.com/ScriptReference/MinAttribute)** | Unsupported❌ | Supported✔️ | ❌ | ❓ | Only valid on fields by default. |
| **[MultilineAttribute](https://docs.unity3d.com/ScriptReference/MultilineAttribute)** | Unsupported❌ | Supported✔️ | ❌ | ❓ | Only valid on fields by default. |
| **[NonReorderableAttribute](https://docs.unity3d.com/ScriptReference/NonReorderableAttribute)** | Unsupported❌ | Supported✔️ | ❌ | ❓ | Only valid on fields by default. |
| **[PreferBinarySerialization](https://docs.unity3d.com/ScriptReference/PreferBinarySerialization)** | Unsupported❌ | Maybe❓ | ❌ | Not | Only valid on classes by default. No effect on properties. Use to change internally from string/JSON representation to binary? |
| **[PropertyAttribute](https://docs.unity3d.com/ScriptReference/PropertyAttribute)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Abstract base class. No effect on properties. |
| **[RangeAttribute](https://docs.unity3d.com/ScriptReference/RangeAttribute)** | Unsupported❌ | Supported✔️ | ❌ | ❓ | Only valid on fields by default. |
| **[RequireComponent](https://docs.unity3d.com/ScriptReference/RequireComponent)** | Unsupported❌ | Maybe❓ | ❌ | ❓ | Only valid on classes by default. No effect on properties. Use to ensure prop is always in valid state? Won't have original effect. |
| **[RPC](https://docs.unity3d.com/ScriptReference/RPC)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | DEPRECATED BY UNITY. |
| **[RuntimeInitializeOnLoadMethodAttribute](https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute)** | Unsupported❌ | Maybe❓ | ❌ | ❓ | Only valid on methods by default. No effect on properties. I'll have to see, but probably not; this functionality is probably best achieved through MonoBehaviour.Awake. |
| **[SelectionBaseAttribute](https://docs.unity3d.com/ScriptReference/SelectionBaseAttribute)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on classes by default. No effect on properties. |
| **[SerializeField](https://docs.unity3d.com/ScriptReference/SerializeField)** | Unsupported❌ | Maybe❓ | ✔️ | ❓ | Might be used as an alternative way of marking properties that should be serialized. We'll see. |
| **[SerializeReference](https://docs.unity3d.com/ScriptReference/SerializeReference)** | Unsupported❌ | Maybe❓ | ❌ | ❓ | Only valid on fields by default. No effect on properties. Might cause problems, I'll have to see. |
| **[SharedBetweenAnimatorsAttribute](https://docs.unity3d.com/ScriptReference/SharedBetweenAnimatorsAttribute)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on classes by default. No effect on properties. |
| **[SpaceAttribute](https://docs.unity3d.com/ScriptReference/SpaceAttribute)** | Supported✔️ | Supported✔️ | ❌ | ✔️ | Only valid on fields by default. |
| **[TextAreaAttribute](https://docs.unity3d.com/ScriptReference/TextAreaAttribute)** | Unsupported❌ | Supported✔️ | ❌ | ❓ | Only valid on fields by default. |
| **[TooltipAttribute](https://docs.unity3d.com/ScriptReference/TooltipAttribute)** | Supported✔️ | Supported✔️ | ✔️ | ❓ | - |
| **[UnityAPICompatibilityVersionAttribute](https://docs.unity3d.com/ScriptReference/UnityAPICompatibilityVersionAttribute)** | Unsupported❌ | Unsupported❌ | ❌ | ❌ | Only valid on assemblies by default. No effect on properties. |
| **[FormerlySerializedAsAttribute](https://docs.unity3d.com/ScriptReference/Serialization.FormerlySerializedAsAttribute.html)** | Unsupported❌ | Supported✔️ | ❓ | ❌ | - |

Note: The *Allowed on Properties Directly* column was tested on version 2021.3.4f1. Things might have changed since then. For the best compatibility, if there is a workaround listed, I'd recommend using that instead.

Note: All aliases now derived from the type they are aliases of. This makes supporting them easier, as there is no need to specify the derived type. However, if Unity changes the underlying types in some large way (e.g. deleting them outright), these will cease to function.

To use attributes marked as *Supported* in the *Support Status* column, follow the table below.

| Allowed on Properties Directly | Allowed on Properties Using Workaround | How to use |
| :---: | :---: | --- |
| ✔️ | ❌ | Place the attribute on the property just like you would on a field. |
| ❌ | ✔️ | Prepend the attribute with `JMor.Utility.Inspector.Alias.` like this: `[JMor.Utility.Inspector.Alias.AttributeName]`. This will use an alias class in the `JMor.Utility.Inspector.Alias` namespace. I'd recommend using an alias for the namespace (e.g. `using PIA = JMor.Utility.Inspector.Alias;`) to keep it from bloating the attribute (e.g. `[PIA.AttributeName]`). |
| ✔️ | ✔️ | Either previously mentioned method will work. I'd recommend using this aliases, as those are less likely to change between Unity releases. |
| ❌ | ❌ | This is completely unsupported. |

If something says it's unsupported, but you can put it on a property without causing a compiler error, it will most likely just fail silently (both with and without the `PropertyInspectorObject`). It's also possible that it will just work. That being said, it's more likely that it will noisily fail, and it's possible it'll completely break things (but probably not). I'd recommend not using anything not listed as currently supported above, as I've yet to test those myself.

### Troubleshooting
If used carelessly, this has a very real potential to wreak havoc on your variables. The limitations are not tips, please consider them. If something seems wrong, or you'd just like more insight, there is debug info.

To show debug info in the inspector, right-click on a property represented by the PropertyInspectorObject and select the "Toggle Debug" option.

This will add 3 things to the inspector:

1. A Debug? button which toggles hiding and showing all debug-related things (yes, it's redundant).
2. A Super Debug button which toggles verbose console logging statements.
3. For each property, adds an entry that allow you to see and directly edit the serialized version of the property (this can cause problems if you enter bad values, be careful).

To remove them all, right-click and deselect the "Toggle Debug" option.

## Limitations

* Currently, only properties w/ getters and setters work. This includes auto properties.
* The way this works is by serializing the property's getter output, putting that in the inspector, serializing and storing changes to that value in the inspector, and deserializing the value and passing it the property's setter. This happens frequently. As a result, properties that don't directly correspond to a value can fail catastrophically (e.g. a property that adds to its associated data instead of directly assigning it will have any change happen again and again). Be very careful of what your property does before putting it in the inspector. If you see crazy, unexplained behavior after adding this to a class, take a close look at how your property assigns and retrieves data.
* Inspector only updates to match changed property positions/values after interaction. It doesn't break anything, but it's a bit startling (and irritating).

[^1]: Theoretically, if you created a native C++ side for a class and set things up correctly, you could. If you'd like to have fun figuring out how to do that (and if you even can), I wish you the best. For our purposes, you can't.