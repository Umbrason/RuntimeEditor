using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EditorWindowManager : MonoBehaviour
{
    private static EditorWindowManager singleton;
    public static EditorWindowManager Singleton { get { return singleton; } }
    private EditorLayoutTree activeLayout;

    public GameObject DynamicPanelTemplate;

    private GameObject rootPanelLayout;

    void OnEnable() => singleton = this;
    void OnDisable() => singleton = singleton == this ? null : singleton;


    public void RegisterEditorWindowInstance(EditorTab editorTab, string identifier)
    {

    }
   
    public static void LoadFromFile(string path)
    {
        if (!SerializationManager.TryDeserialize<EditorLayoutTree>(path, out EditorLayoutTree layout))
            return;
        singleton?.SetLayout(layout);
    }

    public void SetLayout(EditorLayoutTree layout)
    {
        activeLayout = layout;
        DestroyPanelInstances();
        rootPanelLayout = InstantiateLayoutRecursive(layout, transform).gameObject;
    }

    public void DestroyPanelInstances()
    {
        if (!rootPanelLayout)
            return;
        Destroy(rootPanelLayout);
        rootPanelLayout = null;
    }

    public DynamicPanel InstantiateLayoutRecursive(EditorLayoutTree layoutTree, Transform parent = null)
    {
        var GO = Instantiate(DynamicPanelTemplate, parent);
        var panelComponent = GO.GetComponent<DynamicPanel>();
        if (layoutTree.IsLeaf)
        {
            foreach (var tabIdentifier in layoutTree.dockedTabs)
            {
                var editorTab = EditorTabRegistry.InstantiateTabContent(tabIdentifier);
            }
        }
        else if (layoutTree.childA != null && layoutTree.childB != null) //Instantiate children recursive
        {
            var splitPercent = layoutTree.splitPosition;
            var splitOrientation = layoutTree.splitOrientation;
            var A = InstantiateLayoutRecursive(layoutTree.childA);
            var B = InstantiateLayoutRecursive(layoutTree.childB);
            panelComponent.SetChildren(A, B, layoutTree.splitOrientation, layoutTree.splitPosition);
        }
        return panelComponent;
    }
}