using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Draws a level's obstacle entry as distance, prefab, then the fields of its type's data.
[CustomPropertyDrawer(typeof(ObstacleData))]
public class ObstacleDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, Title(property), true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            foreach (SerializedProperty field in VisibleFields(property))
            {
                row.y += row.height + EditorGUIUtility.standardVerticalSpacing;
                row.height = EditorGUI.GetPropertyHeight(field, true);
                EditorGUI.PropertyField(row, field, true);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded)
        {
            foreach (SerializedProperty field in VisibleFields(property))
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(field, true);
        }
        return height;
    }

    static IEnumerable<SerializedProperty> VisibleFields(SerializedProperty property)
    {
        yield return property.FindPropertyRelative(nameof(ObstacleData.distance));
        yield return property.FindPropertyRelative(nameof(ObstacleData.prefab));

        // The type's own fields, drawn inline instead of under a nested "Data" foldout.
        SerializedProperty data = property.FindPropertyRelative(nameof(ObstacleData.data));
        SerializedProperty end = data.GetEndProperty();
        SerializedProperty field = data.Copy();
        bool more = field.NextVisible(true);
        while (more && !SerializedProperty.EqualContents(field, end))
        {
            yield return field.Copy();
            more = field.NextVisible(false);
        }
    }

    // e.g. "Door at 10" instead of "Element 0"
    static GUIContent Title(SerializedProperty property)
    {
        Object prefab = property.FindPropertyRelative(nameof(ObstacleData.prefab)).objectReferenceValue;
        float distance = property.FindPropertyRelative(nameof(ObstacleData.distance)).floatValue;
        return new GUIContent($"{(prefab != null ? prefab.name : "No prefab")} at {distance}");
    }
}
