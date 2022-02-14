using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
using System;

public class RuntimeEditorWindowManager : MonoBehaviour
{
    #region Singleton Pattern
    private static RuntimeEditorWindowManager m_singleton;
    public static RuntimeEditorWindowManager Singleton { get { return m_singleton; } }
    void OnEnable() => m_singleton = this;
    void OnDisable() => m_singleton = m_singleton == this ? null : m_singleton;
    #endregion

    [SerializeField] private GameObject dynamicPanelTemplate;
    [SerializeField] private GameObject editorTabLabelTemplate;
    [SerializeField] private GameObject viewportContainer;
    public Transform ViewportTransform { get { return viewportContainer ? viewportContainer.transform : transform; } }
    private DynamicPanel rootPanel;
    private DynamicPanel m_selectedLeafPanel;
    public DynamicPanel SelectedLeafPanel { get { return m_selectedLeafPanel = (m_selectedLeafPanel && m_selectedLeafPanel.IsLeaf) ? m_selectedLeafPanel : null; } set { m_selectedLeafPanel = value.IsLeaf ? value : m_selectedLeafPanel; } }

    #region tab dragging
    private Vector3 dragOffset;
    public GameObject editorTabLabel;
    #endregion

    #region (de)serialization
    public static void LoadFromFile(string path)
    {
        if (!SerializationManager.TryDeserialize<EditorLayoutTree>(path, out EditorLayoutTree layout))
            return;
        m_singleton?.SetLayoutFromLayoutTree(layout);
    }

    public static void SaveToFile(string path)
    {
        if (!m_singleton)
            return;
        SerializationManager.Serialize(GetLayoutFromDynamicPanel(m_singleton.rootPanel), path);
    }

    private static EditorLayoutTree GetLayoutFromDynamicPanel(DynamicPanel panel)
    {
        var layoutTree = new EditorLayoutTree();
        layoutTree.splitOrientation = panel.SplitOrientation;
        layoutTree.splitPosition = panel.SplitPercent;
        if (!panel.HasChildren)
            layoutTree.dockedTabs = panel.DockedTabNames;
        else
        {
            layoutTree.childA = GetLayoutFromDynamicPanel(panel.ChildA);
            layoutTree.childB = GetLayoutFromDynamicPanel(panel.ChildB);
        }
        return layoutTree;
    }
    private void SetLayoutFromLayoutTree(EditorLayoutTree layout)
    {
        DestroyPanelInstances();
        rootPanel = InstantiateLayoutRecursive(layout, ViewportTransform);
    }

    private DynamicPanel InstantiateLayoutRecursive(EditorLayoutTree layoutTree, Transform parent = null)
    {
        var GO = Instantiate(dynamicPanelTemplate, parent);
        var panelComponent = GO.GetComponent<DynamicPanel>();
        if (layoutTree.IsLeaf)
        {
            foreach (var editorTabName in layoutTree.dockedTabs)
            {
                var editorTab = CreateEditorTab(editorTabName, panelComponent);
                if (!editorTab) Debug.LogError($"Failed to create {editorTabName} tab. Check the Prefab for a missing {editorTabName} component");
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
    #endregion

    #region Tab Creation/Destruction
    public EditorTab GetOrCreateTab(string editorTabTypeName)
    {
        var tab = rootPanel?.GetEditorTabInChildren(editorTabTypeName);
        if (tab) return tab;
        var containerPanel = SelectedLeafPanel;
        if (!containerPanel) return null; //exit if no leaf panel is selected
        tab = CreateEditorTab(editorTabTypeName, containerPanel);
        return tab;
    }

    public void DestroyPanelInstances()
    {
        if (!rootPanel)
            return;
        Destroy(rootPanel.gameObject);
        rootPanel = null;
    }

    private EditorTab CreateEditorTab(string editorTabName, DynamicPanel panel)
    {
        //Instantiate Editor
        var editorTabPrefab = EditorTabRegistry.GetPrefab(editorTabName);
        if (editorTabPrefab == null)
            return null;
        var editorInstance = GameObject.Instantiate(editorTabPrefab, panel.editorTabContentContainer.transform);
        var editorTab = editorInstance.GetComponent<EditorTab>();

        //Instantiate Label
        var editorTabDescriptor = EditorTabRegistry.GetDescriptor(editorTabName);
        editorTab.RegisterTabLabel(InstantiateEditorTabLabel(editorTabDescriptor, editorTab, panel));
        panel.AppendTab(editorTab);
        return editorTab;
    }

    private EditorTabLabel InstantiateEditorTabLabel((string, Sprite) tabInfo, EditorTab tab, DynamicPanel panel)
    {
        if (tabInfo.Item1 == null || !panel)
            return null;
        var tabLabelInstance = Instantiate(editorTabLabelTemplate, panel.editorTabLabelContainer.transform);
        var tabLabel = tabLabelInstance.GetComponent<EditorTabLabel>();
        if (!tabLabel)
            return null;
        var nameTextComponent = tabLabelInstance.GetComponentsInChildren<ThemedText>().FirstOrDefault((x => x.gameObject.name.ToLower().Contains("name")));
        nameTextComponent.text = tabInfo.Item1;
        var iconImageComponent = tabLabelInstance.GetComponentsInChildren<ThemedImage>().FirstOrDefault((x => x.gameObject.name.ToLower().Contains("icon")));
        iconImageComponent.sprite = tabInfo.Item2;
        return tabLabel;
    }
    #endregion
}