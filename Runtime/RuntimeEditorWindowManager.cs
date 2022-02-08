using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public class RuntimeEditorWindowManager : MonoBehaviour
{
    private static RuntimeEditorWindowManager singleton;
    public static RuntimeEditorWindowManager Singleton { get { return singleton; } }

    public GameObject dynamicPanelTemplate;
    public GameObject editorTabLabelTemplate;
    public GameObject viewportContainer;
    public Transform ViewportTransform { get { return viewportContainer ? viewportContainer.transform : transform; } }
    private DynamicPanel rootPanel;

    #region tab dragging
    private Vector3 dragOffset;
    public GameObject editorTabLabel;
    #endregion

    #region (de)serialization
    public static void LoadFromFile(string path)
    {
        if (!SerializationManager.TryDeserialize<EditorLayoutTree>(path, out EditorLayoutTree layout))
            return;
        singleton?.SetLayoutFromLayoutTree(layout);
    }

    public static void SaveToFile(string path)
    {
        if (!singleton)
            return;
        SerializationManager.Serialize(GetLayoutFromDynamicPanel(singleton.rootPanel), path);
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
        DestroyPanelInstances();
        rootPanel = InstantiateLayoutRecursive(layout, ViewportTransform);
    }

    private DynamicPanel InstantiateLayoutRecursive(EditorLayoutTree layoutTree, Transform parent = null)
    {
        var GO = Instantiate(dynamicPanelTemplate, parent);
        var panelComponent = GO.GetComponent<DynamicPanel>();
        if (layoutTree.IsLeaf)
        {
            foreach (var tabIdentifier in layoutTree.dockedTabs)
            {
                var editorTab = InstantiateEditorTab(tabIdentifier, panelComponent);
                panelComponent.AppendTab(editorTab);
                Debug.Log($"Instantiate tab {tabIdentifier}");
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

    void OnEnable() => singleton = this;
    void OnDisable() => singleton = singleton == this ? null : singleton;


    #region Tab Creation/Destruction
    public void DestroyPanelInstances()
    {
        if (!rootPanel)
            return;
        Destroy(rootPanel.gameObject);
        rootPanel = null;
    }

    private EditorTab InstantiateEditorTab(string identifier, DynamicPanel panel)
    {
        //Instantiate Editor
        var editorTabPrefab = EditorTabRegistry.GetPrefab(identifier);
        if (editorTabPrefab == null)
            return null;
        var editorInstance = GameObject.Instantiate(editorTabPrefab, panel.editorTabContentContainer.transform);
        var editorTabComponent = editorInstance.GetComponent<EditorTab>();

        //Instantiate Label
        var editorTabDescriptor = EditorTabRegistry.GetDescriptor(identifier);
        editorTabComponent.labelGO = InstantiateEditorTabLabel(editorTabDescriptor, editorTabComponent, panel);

        return editorTabComponent;
    }

    private GameObject InstantiateEditorTabLabel((string, Sprite) tabInfo, EditorTab tab, DynamicPanel panel)
    {
        if (tabInfo.Item1 == null)
            return null;
        var tabLabelInstance = Instantiate(editorTabLabelTemplate, panel.editorTabLabelContainer.transform);
        var nameTextComponent = tabLabelInstance.GetComponentsInChildren<ThemedText>().FirstOrDefault((x => x.gameObject.name.ToLower().Contains("name")));
        nameTextComponent.text = tabInfo.Item1;
        var iconImageComponent = tabLabelInstance.GetComponentsInChildren<ThemedImage>().FirstOrDefault((x => x.gameObject.name.ToLower().Contains("icon")));
        iconImageComponent.sprite = tabInfo.Item2;
        if (panel == null)
            return tabLabelInstance;
        var tabLabelEventTrigger = tabLabelInstance.GetComponent<EventTrigger>();
        var tabLabelRectTransform = tabLabelInstance.GetComponent<RectTransform>();

        var onClick = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.PointerClick
        };
        onClick.callback.AddListener((x) => tab.CurrentPanel.SelectTab(tab));

        var beginDrag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.BeginDrag
        };
        beginDrag.callback.AddListener((x) => tab.CurrentPanel.BeginTabLabelDrag(x as PointerEventData, tabLabelRectTransform));

        var drag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.Drag
        };
        drag.callback.AddListener((x) => tab.CurrentPanel.DragTabLabel(x as PointerEventData, tabLabelRectTransform));

        var endDrag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.EndDrag
        };
        endDrag.callback.AddListener((x) => tab.CurrentPanel.EndTabLabelDrag(x as PointerEventData, tabLabelRectTransform));

        tabLabelEventTrigger.triggers.Add(onClick);
        tabLabelEventTrigger.triggers.Add(beginDrag);
        tabLabelEventTrigger.triggers.Add(drag);
        tabLabelEventTrigger.triggers.Add(endDrag);

        return tabLabelInstance;
    }
    #endregion
}