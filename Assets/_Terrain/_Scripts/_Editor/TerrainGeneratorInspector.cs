using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var script = (TerrainGenerator)target;

        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        if (EditorGUI.EndChangeCheck())
        {
            if (script.autoUpdate && !Application.isPlaying)
            {
                script.GenerateEditorTerrain();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            script.GenerateEditorTerrain();
        }
    }

    public void OnValidate()
    {
        var script = (TerrainGenerator)target;

    }
}
