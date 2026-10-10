using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Draws a level's obstacle entry showing only the fields its prefab type declares in DataFields.
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
                row.height = EditorGUI.GetPropertyHeight(field);
                EditorGUI.PropertyField(row, field);
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
                height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(field);
        }
        return height;
    }

    static IEnumerable<SerializedProperty> VisibleFields(SerializedProperty property)
    {
        yield return property.FindPropertyRelative(nameof(ObstacleData.distance));
        yield return property.FindPropertyRelative(nameof(ObstacleData.prefab));

        LevelObstacle prefab = Prefab(property);
        if (prefab == null)
            yield break;

        foreach (string name in prefab.DataFields)
        {
            SerializedProperty field = property.FindPropertyRelative(name);
            if (field != null)
                yield return field;
        }
    }

    // e.g. "Door at 10" instead of "Element 0"
    static GUIContent Title(SerializedProperty property)
    {
        LevelObstacle prefab = Prefab(property);
        float distance = property.FindPropertyRelative(nameof(ObstacleData.distance)).floatValue;
        return new GUIContent($"{(prefab != null ? prefab.name : "No prefab")} at {distance}");
    }

    static LevelObstacle Prefab(SerializedProperty property)
    {
        return property.FindPropertyRelative(nameof(ObstacleData.prefab)).objectReferenceValue as LevelObstacle;
    }
}
