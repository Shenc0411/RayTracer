using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(RayTracer))]
public class RayTracerEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        RayTracer RT = (RayTracer)target;
        if (GUILayout.Button("Render")) {
            RT.RayTrace();
        }
    }

}
