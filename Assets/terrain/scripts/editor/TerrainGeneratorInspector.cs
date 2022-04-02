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
            if (script.autoUpdate)
            {
                script.Generate();
            }
        }

        if (GUILayout.Button("Generate"))
        {
            script.Generate();
        }
    }

    public void OnValidate()
    {
        var script = (TerrainGenerator)target;

    }
}
