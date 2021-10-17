using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public static class RuntimeEditorAssetContextMenuExtensions
{
    private static GameObject MenuBarTemplate = Resources.Load<GameObject>("RuntimeEditor/MenuBarTemplate");

    [MenuItem("GameObject/UI/MenuBar")]
    public static void InstantiateMenuBarTemplate()
    {
        var selections = Selection.GetFiltered<GameObject>(SelectionMode.Editable);
        Undo.SetCurrentGroupName("Menu bar creations");

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
            Undo.RegisterCreatedObjectUndo(instance, "create menuBar");
        };

        if (selections.Length > 0)
            foreach (var go in selections)
                createMenuBarFunction.Invoke(go.transform);
        else
            createMenuBarFunction.Invoke(null);
        Undo.IncrementCurrentGroup();
    }
}
