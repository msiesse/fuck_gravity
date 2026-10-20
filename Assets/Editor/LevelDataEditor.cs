using UnityEditor;
using UnityEngine;

// The usual level Inspector, with a button to edit the level visually.
[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Open in Level Editor"))
            LevelEditorWindow.Open((LevelData)target);
        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
