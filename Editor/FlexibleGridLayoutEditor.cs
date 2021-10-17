using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FlexibleGridLayoutGroup))]
public class FlexibleGridLayoutEditor : Editor
{

    public override void OnInspectorGUI()
    {
        var fitTypeProperty = serializedObject.FindProperty("fitType");
        EditorGUILayout.PropertyField(fitTypeProperty);
        var enableColumns = fitTypeProperty.enumValueIndex == (int)FlexibleGridLayoutGroup.FitType.MaxColumns;
        var enableRows = fitTypeProperty.enumValueIndex == (int)FlexibleGridLayoutGroup.FitType.MaxRows;

        GUI.enabled = enableColumns;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("columns"));
        GUI.enabled = enableRows;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rows"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("spacing"));

        var freeAspectProperty = serializedObject.FindProperty("freeAspect");
        EditorGUILayout.PropertyField(freeAspectProperty);

        GUI.enabled = freeAspectProperty.boolValue;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("aspectRatio"));
        GUI.enabled = true;

        if (serializedObject.hasModifiedProperties)
            serializedObject.ApplyModifiedProperties();
    }
}

