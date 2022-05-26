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
    public DynamicPanel SelectedLeafPanel { get { return m_selectedLeafPanel = (m_selectedLeafPanel && m_selectedLeafPanel.IsLeaf) ? m_selectedLeafPanel : null; } set { m_selectedLeafPanel = value && value.IsLeaf ? value : m_selectedLeafPanel; } }

    void Start() { rootPanel = InstantiateLayoutRecursive(new EditorLayoutTree(), ViewportTransform); SelectedLeafPanel = rootPanel; }

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
            layoutTree.dockedTabs = panel.DockedTabTypes;
        else
        {
            layoutTree.childA = GetLayoutFromDynamicPanel(panel.ChildA);
            layoutTree.childB = GetLayoutFromDynamicPanel(panel.ChildB);
        }
        return layoutTree;
    }

    private void SetLayoutFromLayoutTree(EditorLayoutTree layout)
    {
        DestroyAllPanelInstances();
        rootPanel = InstantiateLayoutRecursive(layout, ViewportTransform);
    }

    public void SplitPanelInTwo(DynamicPanel panel, EditorTab[] panelATabs, EditorTab[] panelBTabs, float splitPercent, SplitOrientation splitOrientation)
    {
        
    }

    public void MergePanels(DynamicPanel A, DynamicPanel B)
    {
        if (A.Parent != B.Parent)
            return;
        var parent = A.Parent;
        SelectedLeafPanel = SelectedLeafPanel == A || SelectedLeafPanel == B ? parent : SelectedLeafPanel;
        parent.MergeChildren();
    }

    private DynamicPanel InstantiateLayoutRecursive(EditorLayoutTree layoutTree, Transform parent = null)
    {
        var GO = Instantiate(dynamicPanelTemplate, parent);
        var panelComponent = GO.GetComponent<DynamicPanel>();
        if (layoutTree.IsLeaf)
        {
            foreach (var editorTabType in layoutTree.dockedTabs)
            {
                var editorTab = InstantiateEditorTab(editorTabType, panelComponent);
                if (!editorTab) Debug.LogError($"Failed to create {editorTabType} tab. Check the Prefab for a missing {editorTabType} component");
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
    public EditorTab GetOrCreateTab(Type editorTabType)
    {
        var tab = rootPanel?.GetEditorTabInChildren(editorTabType);
        if (tab) return tab;
        var containerPanel = SelectedLeafPanel;
        if (!containerPanel) return null; //exit if no leaf panel is selected
        tab = InstantiateEditorTab(editorTabType, containerPanel);
        return tab;
    }

    public void DestroyAllPanelInstances()
    {
        if (!rootPanel)
            return;
        Destroy(rootPanel.gameObject);
        rootPanel = null;
    }


    #endregion

    #region Prefab_Instantiation    
    public DynamicPanel InstantiateDynamicPanel(Transform parent) => Instantiate(dynamicPanelTemplate, parent).GetComponent<DynamicPanel>();
    public EditorTab InstantiateEditorTab(Type editorTabType, DynamicPanel panel)
    {
        //Instantiate Editor
        
        var editorTabPrefab = EditorTabPrefabLibrary.Instance?[editorTabType];
        if (editorTabPrefab == null)
            return null;
        var editorInstance = GameObject.Instantiate(editorTabPrefab);        
        var editorTab = editorInstance.GetComponent<EditorTab>();

        //Instantiate Label
        var editorTabIcon = EditorTabIconLibrary.Instance?[editorTabType];        
        editorTab.RegisterTabLabel(InstantiateEditorTabLabel(editorTabType.Name, editorTabIcon, editorTab));
        panel.MoveEditorTabToThis(editorTab);
        return editorTab;
    }
    private EditorTabLabel InstantiateEditorTabLabel(string tabName, Sprite tabIcon, EditorTab tab)
    {
        if (tabIcon == null)
            return null;
        var tabLabelInstance = Instantiate(editorTabLabelTemplate);
        var tabLabel = tabLabelInstance.GetComponent<EditorTabLabel>();
        if (!tabLabel)
            return null;
        var nameTextComponent = tabLabelInstance.GetComponentsInChildren<ThemedText>().FirstOrDefault((x => x.gameObject.name.ToLower().Contains("name")));
        nameTextComponent.text = tabName;
        var iconImageComponent = tabLabelInstance.GetComponentsInChildren<ThemedImage>().FirstOrDefault((x => x.gameObject.name.ToLower().Contains("icon")));
        iconImageComponent.sprite = tabIcon;
        return tabLabel;
    }
    #endregion
}