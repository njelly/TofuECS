using Tofunaut.TofuECS.Math;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tofunaut.TofuECS.Unity.Editor
{
    [CustomPropertyDrawer(typeof(SerializableFixVector2))]
    public class SerializableFixVector2Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) 
        {
            EditorGUI.BeginProperty( position, label, property );

            var longValueXProperty = property.FindPropertyRelative("RawValueX");
            var longValueYProperty = property.FindPropertyRelative("RawValueY");
            var vector2Value =
                EditorGUI.Vector2Field(position, property.displayName,
                    new Vector2((float)Fix64.FromRaw(longValueXProperty.longValue),
                        (float)Fix64.FromRaw(longValueYProperty.longValue)));
            longValueXProperty.longValue = Fix64.FROM_FLOAT_UNSAFE(vector2Value.x).RawValue;
            longValueYProperty.longValue = Fix64.FROM_FLOAT_UNSAFE(vector2Value.y).RawValue;

            EditorGUI.EndProperty();
        }
    }
}