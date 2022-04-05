using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainMaterialProgrammer))]
public class TerrainMaterialProgrammerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        base.OnInspectorGUI();

        if (GUILayout.Button("Program settings") || EditorGUI.EndChangeCheck())
        {
            var script = (TerrainMaterialProgrammer)target;

            script.Program();
        }
    }
}
