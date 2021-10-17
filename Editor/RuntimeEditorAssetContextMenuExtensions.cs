using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public static class RuntimeEditorAssetContextMenuExtensions
{
    private static GameObject MenuBarTemplate = Resources.Load<GameObject>("RuntimeEditor/MenuBarTemplate");
    private static GameObject RuntimeEditorTemplate = Resources.Load<GameObject>("RuntimeEditor/RuntimeEditorTemplate");

    [MenuItem("GameObject/UI/MenuBar")]
    public static void InstantiateMenuBarTemplate()
    {
        var selections = Selection.GetFiltered<GameObject>(SelectionMode.Editable);
        Undo.SetCurrentGroupName("Menu bar creation");

        Action<Transform> createMenuBarFunction = (Transform t) =>
        {
            var instance = GameObject.Instantiate(MenuBarTemplate, t);
            instance.name = "MenuBar";
            foreach (var themedComponent in instance.GetComponentsInChildren<ThemedImage>())
                themedComponent.palette = ThemedUIEditorUtility.ActivePalette;
            foreach (var themedComponent in instance.GetComponentsInChildren<ThemedText>())
                themedComponent.palette = ThemedUIEditorUtility.ActivePalette;
            foreach (var themedComponent in instance.GetComponentsInChildren<ThemedTextMeshPro>())
                themedComponent.palette = ThemedUIEditorUtility.ActivePalette;
            Undo.RegisterCreatedObjectUndo(instance, "create MenuBar");
        };

        if (selections.Length > 0)
            foreach (var go in selections)
                createMenuBarFunction.Invoke(go.transform);
        else
            createMenuBarFunction.Invoke(null);
        Undo.IncrementCurrentGroup();
    }

    [MenuItem("GameObject/UI/MenuBar")]
    public static void InstantiateRuntimeEditorTemplate()
    {
        var selections = Selection.GetFiltered<GameObject>(SelectionMode.Editable);
        Undo.SetCurrentGroupName("RuntimeEditor creation");

        Action<Transform> createRuntimeEditorFunction = (Transform t) =>
        {
            var instance = GameObject.Instantiate(RuntimeEditorTemplate, t);
            instance.name = "Runtime Editor";
            foreach (var themedComponent in instance.GetComponentsInChildren<ThemedImage>())
                themedComponent.palette = ThemedUIEditorUtility.ActivePalette;
            foreach (var themedComponent in instance.GetComponentsInChildren<ThemedText>())
                themedComponent.palette = ThemedUIEditorUtility.ActivePalette;
            foreach (var themedComponent in instance.GetComponentsInChildren<ThemedTextMeshPro>())
                themedComponent.palette = ThemedUIEditorUtility.ActivePalette;
            Undo.RegisterCreatedObjectUndo(instance, "create RuntimeEditor");
        };

        if (selections.Length > 0)
            foreach (var go in selections)
                createRuntimeEditorFunction.Invoke(go.transform);
        else
            createRuntimeEditorFunction.Invoke(null);
        Undo.IncrementCurrentGroup();
    }
}
