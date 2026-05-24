using AdvancedStructureSkins.Shared.SDK;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[CustomPropertyDrawer(typeof(MaterialPropertyOverride))]
public class MaterialPropertyOverrideDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float y = position.y;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty propertyName = property.FindPropertyRelative("propertyName");
        SerializedProperty propertyType = property.FindPropertyRelative("propertyType");
        SerializedProperty colorValue = property.FindPropertyRelative("colorValue");
        SerializedProperty floatValue = property.FindPropertyRelative("floatValue");
        SerializedProperty intValue = property.FindPropertyRelative("intValue");
        SerializedProperty vectorValue = property.FindPropertyRelative("vectorValue");
        SerializedProperty targetStructures = property.FindPropertyRelative("targetStructures");

        Rect line;

        // Name
        line = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(line, propertyName);
        y += line.height + spacing;

        // Type
        line.y = y;
        EditorGUI.PropertyField(line, propertyType);
        y += line.height + spacing;

        ShaderPropertyType type = (ShaderPropertyType)propertyType.enumValueIndex;

        // Value block
        line.y = y;

        switch (type)
        {
            case ShaderPropertyType.Color:
                EditorGUI.PropertyField(line, colorValue);
                y += EditorGUI.GetPropertyHeight(colorValue) + spacing;
                break;

            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                EditorGUI.PropertyField(line, floatValue);
                y += EditorGUI.GetPropertyHeight(floatValue) + spacing;
                break;

            case ShaderPropertyType.Int:
                EditorGUI.PropertyField(line, intValue);
                y += EditorGUI.GetPropertyHeight(intValue) + spacing;
                break;

            case ShaderPropertyType.Vector:
                vectorValue.vector4Value =
                    EditorGUI.Vector4Field(line, "Vector", vectorValue.vector4Value);

                y += EditorGUI.GetPropertyHeight(vectorValue) + spacing;
                break;
        }

        // Target structures (IMPORTANT: correct height usage)
        line.y = y;
        float targetHeight = EditorGUI.GetPropertyHeight(targetStructures, true);
        EditorGUI.PropertyField(new Rect(line.x, line.y, line.width, targetHeight), targetStructures, true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty propertyName = property.FindPropertyRelative("propertyName");
        SerializedProperty propertyType = property.FindPropertyRelative("propertyType");
        SerializedProperty colorValue = property.FindPropertyRelative("colorValue");
        SerializedProperty floatValue = property.FindPropertyRelative("floatValue");
        SerializedProperty intValue = property.FindPropertyRelative("intValue");
        SerializedProperty vectorValue = property.FindPropertyRelative("vectorValue");
        SerializedProperty targetStructures = property.FindPropertyRelative("targetStructures");

        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float height = 0f;

        // name
        height += EditorGUIUtility.singleLineHeight + spacing;

        // type
        height += EditorGUIUtility.singleLineHeight + spacing;

        ShaderPropertyType type = (ShaderPropertyType)propertyType.enumValueIndex;

        // value field
        switch (type)
        {
            case ShaderPropertyType.Color:
                height += EditorGUI.GetPropertyHeight(colorValue) + spacing;
                break;

            case ShaderPropertyType.Float:
            case ShaderPropertyType.Range:
                height += EditorGUI.GetPropertyHeight(floatValue) + spacing;
                break;

            case ShaderPropertyType.Int:
                height += EditorGUI.GetPropertyHeight(intValue) + spacing;
                break;

            case ShaderPropertyType.Vector:
                height += EditorGUI.GetPropertyHeight(vectorValue) + spacing;
                break;
        }

        // target structures (foldout list)
        height += EditorGUI.GetPropertyHeight(targetStructures, true);

        return height;
    }
}
