using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EditorWindowManager : MonoBehaviour
{
    private static EditorWindowManager singleton;
    public static EditorWindowManager Singleton { get { return singleton; } }

    public GameObject DynamicPanelTemplate;
    private DynamicPanel rootPanel;

    public static void LoadFromFile(string path)
    {
        if (!SerializationManager.TryDeserialize<EditorLayoutTree>(path, out EditorLayoutTree layout))
            return;
        singleton?.SetLayout(layout);
    }

    public static void SaveToFile(string path)
    {
        if (!singleton)
            return;
        SerializationManager.Serialize(LayoutFromDynamicPanel(singleton.rootPanel), path);
    }

    public static EditorLayoutTree LayoutFromDynamicPanel(DynamicPanel panel)
    {
        var layoutTree = new EditorLayoutTree();
        layoutTree.splitOrientation = panel.SplitOrientation;
        layoutTree.splitPosition = panel.SplitPercent;
        if (layoutTree.IsLeaf)
            layoutTree.dockedTabs = panel.TabTypes;
        else
        {
            layoutTree.childA = LayoutFromDynamicPanel(panel.ChildA);
            layoutTree.childB = LayoutFromDynamicPanel(panel.ChildB);
        }
        return layoutTree;
    }

    void OnEnable() => singleton = this;
    void OnDisable() => singleton = singleton == this ? null : singleton;




    public void RegisterEditorWindowInstance(EditorTab editorTab, string identifier)
    {

    }

    public void SetLayout(EditorLayoutTree layout)
    {
        DestroyPanelInstances();
        rootPanel = InstantiateLayoutRecursive(layout, transform);
    }


    public void DestroyPanelInstances()
    {
        if (!rootPanel)
            return;
        Destroy(rootPanel.gameObject);
        rootPanel = null;
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