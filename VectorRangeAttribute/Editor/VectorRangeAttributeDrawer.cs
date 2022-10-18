/*
Vector Range Attribute by Just a Pixel (Danny Goodayle @DGoodayle) - http://www.justapixel.co.uk
Copyright (c) 2015
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
USAGE
[VectorRange(minX, maxX, minY, maxY, clamped)]
public Vector2 yourVector;
*/
// TAKEN FROM https://gist.github.com/DGoodayle/69c9c06eb0a277d833c5
using UnityEngine;
using UnityEditor;
using JMor.Utility;

namespace JMor.EditorScripts.Utility
{
// TODO: Add to Unity Packages
// TODO: Refactor to derive from Vector?
[CustomPropertyDrawer(typeof(VectorRangeAttribute))]
public class VectorRangeAttributeDrawer : PropertyDrawer
{
    const int HELP_HEIGHT = 32;
    const int TEXT_HEIGHT = 16;
    const int BASE_HEIGHT = 18;
	// 345 comes from https://answers.unity.com/questions/1844022/how-to-get-width-at-which-inspector-does-line-brea.html
	// 365 comes from my personal 'Edge of Control' project, which is likely different because it uses UI Toolkit and a custom style.
	const int VECTOR_LINE_BREAK_WIDTH = 345/*365*/;
    VectorRangeAttribute RangeAttribute { get { return (VectorRangeAttribute)attribute; } }
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Color previous = GUI.color;
        GUI.color = !IsValid(property) ? Color.red : Color.white;
        Rect textFieldPosition = position;
        // textFieldPosition.width = position.width;
        // textFieldPosition.height = position.height;
        EditorGUI.BeginChangeCheck();
        if (RangeAttribute.IsVector3())
        {
            Vector3 val = EditorGUI.Vector3Field(textFieldPosition, label, property.vector3Value);
            if (EditorGUI.EndChangeCheck())
            {
                if (RangeAttribute.bClamp)
                {
                    val.x = Mathf.Clamp(val.x, RangeAttribute.fMinX, RangeAttribute.fMaxX);
                    val.y = Mathf.Clamp(val.y, RangeAttribute.fMinY, RangeAttribute.fMaxY);
                    val.z = Mathf.Clamp(val.z, RangeAttribute.fMinZ, RangeAttribute.fMaxZ);
                }
                property.vector3Value = val;
            }
        }
		else
        {
            Vector2 val = EditorGUI.Vector2Field(textFieldPosition, label, property.vector2Value);
            if (EditorGUI.EndChangeCheck())
            {
                if (RangeAttribute.bClamp)
                {
                    val.x = Mathf.Clamp(val.x, RangeAttribute.fMinX, RangeAttribute.fMaxX);
                    val.y = Mathf.Clamp(val.y, RangeAttribute.fMinY, RangeAttribute.fMaxY);
                }
                property.vector2Value = val;
            }
        }
        Rect helpPosition = position;
        helpPosition.y += TEXT_HEIGHT;
        helpPosition.height = TEXT_HEIGHT;
        DrawHelpBox(helpPosition, property);
        GUI.color = previous;
    }
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)// => (!IsValid(property)) ? HELP_HEIGHT : base.GetPropertyHeight(property, label);
	{
		var baseHeight = base.GetPropertyHeight(property, label);
		return !EditorGUIUtility.wideMode ? baseHeight * 2 : baseHeight;
	}
    void DrawHelpBox(Rect position, SerializedProperty prop)
    {
        // No need for a help box if the pattern is valid.
        if (IsValid(prop))
            return;
        var str = (RangeAttribute.IsVector3()) ? string.Format("Invalid Range X [{0}]-[{1}] Y [{2}]-[{3}] Z [{4}]-[{5}]", RangeAttribute.fMinX, RangeAttribute.fMaxX, RangeAttribute.fMinY, RangeAttribute.fMaxY, RangeAttribute.fMinZ, RangeAttribute.fMaxZ) : string.Format("Invalid Range X [{0}]-[{1}] Y [{2}]-[{3}]", RangeAttribute.fMinX, RangeAttribute.fMaxX, RangeAttribute.fMinY, RangeAttribute.fMaxY);
        EditorGUI.HelpBox(position, str, MessageType.Error);
    }
    bool IsValid(SerializedProperty prop)
    {
        if (RangeAttribute.IsVector3())
        {
            Vector3 vector = prop.vector3Value;
            return vector.x >= RangeAttribute.fMinX && vector.x <= RangeAttribute.fMaxX && vector.y >= RangeAttribute.fMinY && vector.y <= RangeAttribute.fMaxY && vector.z >= RangeAttribute.fMinZ && vector.z <= RangeAttribute.fMaxZ;
        }
		else
		{
            Vector2 vector = prop.vector2Value;
            return vector.x >= RangeAttribute.fMinX && vector.x <= RangeAttribute.fMaxX && vector.y >= RangeAttribute.fMinY && vector.y <= RangeAttribute.fMaxY;
		}
    }
}
}