using Tofunaut.TofuECS.Math;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tofunaut.TofuECS.Unity.Editor
{
    [CustomPropertyDrawer(typeof(SerializableFix64))]
    public class SerializableFix64Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
        {
            EditorGUI.BeginProperty( position, label, property );

            var longValueProperty = property.FindPropertyRelative("RawValue");
            var floatValue =
                EditorGUI.FloatField(position, property.displayName, (float)Fix64.FromRaw(longValueProperty.longValue));
            longValueProperty.longValue = Fix64.FROM_FLOAT_UNSAFE(floatValue).RawValue;

            EditorGUI.EndProperty();
        }
    }
}