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

### Troubleshooting
If used carelessly, this has a very real potential to wreak havoc on your variables. The limitations are not tips, please consider them. If something seems wrong, or you'd just like more insight, there is debug info.

To show debug info in the inspector, right-click on a property represented by the PropertyInspectorObject and select the "Toggle Debug" option.

This will add 3 things to the inspector:

1. A Debug? button which toggles hiding and showing all debug-related things (yes, it's redundant).
2. A Super Debug button which toggles verbose console logging statements.
3. For each property, adds an entry that allow you to see and directly edit the serialized version of the property (this can cause problems if you enter bad values, be careful).

To remove them all, right-click and deselect the "Toggle Debug" option.

## Limitations

* Currently, the only way to modify values affected by the property in the inspector is using the property. Otherwise they will get overwritten by the property.
* Currently, only properties w/ getters and setters work.
* The way this works is by serializing the property's getter output, putting that in the inspector, serializing and storing changes to that value in the inspector, and deserializing the value and passing it the property's setter. This happens frequently. As a result, properties that don't directly correspond to a value can fail catastrophically (e.g. a property that adds to its associated data instead of directly assigning it will have any change happen again and again). Be very careful of what your property does before putting it in the inspector. If you see crazy, unexplained behavior after adding this to a class, take a close look at how your property assigns and retrieves data.

[^1]: Theoretically, if you created a native C++ side for a class and set things up correctly, you could. If you'd like to have fun figuring out how to do that (and if you even can), I wish you the best. For our purposes, you can't.