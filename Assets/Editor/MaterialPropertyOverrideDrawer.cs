using UnityEditor;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

[CustomPropertyDrawer(typeof(MaterialPropertyOverride))]
public class MaterialPropertyOverrideDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        SerializedProperty propertyName = property.FindPropertyRelative("propertyName");
        SerializedProperty propertyType = property.FindPropertyRelative("propertyType");
        SerializedProperty colorValue = property.FindPropertyRelative("colorValue");
        SerializedProperty floatValue = property.FindPropertyRelative("floatValue");
        SerializedProperty intValue = property.FindPropertyRelative("intValue");
        SerializedProperty vectorValue = property.FindPropertyRelative("vectorValue");
        SerializedProperty targetStructures = property.FindPropertyRelative("targetStructures");

        EditorGUI.PropertyField(line, propertyName);
        line.y += line.height + spacing;

        EditorGUI.PropertyField(line, propertyType);
        line.y += line.height + spacing;
        
        ShaderPropertyType type = (ShaderPropertyType)propertyType.enumValueIndex;

        switch (type)
        {
            case ShaderPropertyType.Color:
                EditorGUI.PropertyField(line, colorValue);
                line.y += line.height + spacing;
                break;

            case ShaderPropertyType.Float:
                EditorGUI.PropertyField(line, floatValue);
                line.y += line.height + spacing;
                break;
            
            case ShaderPropertyType.Range:
                EditorGUI.PropertyField(line, floatValue);
                line.y += line.height + spacing;
                break;

            case ShaderPropertyType.Int:
                EditorGUI.PropertyField(line, intValue);
                line.y += line.height + spacing;
                break;

            case ShaderPropertyType.Vector:
                EditorGUI.PropertyField(line, vectorValue);
                line.y += line.height + spacing;
                break;
        }

        EditorGUI.PropertyField(line, targetStructures, true);

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty propertyType = property.FindPropertyRelative("propertyType");
        SerializedProperty targetStructures = property.FindPropertyRelative("targetStructures");

        int lines = 2;

        ShaderPropertyType type = (ShaderPropertyType)propertyType.enumValueIndex;
        if (type != ShaderPropertyType.Texture)
            lines++;

        float height = lines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
        height += EditorGUI.GetPropertyHeight(targetStructures, true);

        return height;
    }
}
