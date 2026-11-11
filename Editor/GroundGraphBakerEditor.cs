#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[CustomEditor(typeof(GroundGraphBaker))]
public class GroundGraphBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        GroundGraphBaker baker =(GroundGraphBaker)target;

        if (GUILayout.Button("Bake Ground Graph"))
        {
            baker.Bake();
        }
    }
}

#endif