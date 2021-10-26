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
        if (!panel.HasChildren)
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
        rootPanel = InstantiateLayoutRecursive(layout, ViewportTransform);
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
        var GO = Instantiate(dynamicPanelTemplate, parent);
        var panelComponent = GO.GetComponent<DynamicPanel>();
        if (layoutTree.IsLeaf)
        {
            foreach (var tabIdentifier in layoutTree.dockedTabs)
            {
                var editorTab = InstantiateEditorTab(tabIdentifier, panelComponent);
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

    private EditorTab InstantiateEditorTab(string identifier, DynamicPanel panel)
    {
        var editorTabPrefab = EditorTabRegistry.GetPrefab(identifier);
        var editorTabComponent = InstantiateEditorTabContent(editorTabPrefab, panel);

        var editorTabDescriptor = EditorTabRegistry.GetDescriptor(identifier);
        var editorTabLabelInstance = InstantiateEditorTabLabel(editorTabDescriptor, panel);
        return editorTabComponent;
    }
    private EditorTab InstantiateEditorTabContent(GameObject editorPrefab, DynamicPanel panel)
    {
        if (editorPrefab == null)
            return null;
        var editorInstance = GameObject.Instantiate(editorPrefab, panel.editorTabContentContainer.transform);
        return editorInstance.GetComponent<EditorTab>();
    }
    private GameObject InstantiateEditorTabLabel((string, Sprite) tabInfo, DynamicPanel panel)
    {
        if (tabInfo.Item1 == null)
            return null;
        var tabLabelInstance = Instantiate(editorTabLabelTemplate, panel.editorTabLabelContainer.transform);
        var nameTextComponent = tabLabelInstance.GetComponentsInChildren<ThemedText>().FirstOrDefault((x => x.gameObject != tabLabelInstance));
        nameTextComponent.text = tabInfo.Item1;
        var iconImageComponent = tabLabelInstance.GetComponentsInChildren<ThemedImage>().FirstOrDefault((x => x.gameObject != tabLabelInstance));
        iconImageComponent.sprite = tabInfo.Item2;
        if (panel == null)
            return tabLabelInstance;
        var tabLabelEventTrigger = tabLabelInstance.GetComponent<EventTrigger>();
        var tabLabelRectTransform = tabLabelInstance.GetComponent<RectTransform>();
        var beginDrag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.BeginDrag
        };
        beginDrag.callback.AddListener((x) => panel.BeginTabLabelDrag(x as PointerEventData, tabLabelRectTransform));

        var drag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.Drag
        };
        drag.callback.AddListener((x) => panel.DragTabLabel(x as PointerEventData, tabLabelRectTransform));

        var endDrag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.EndDrag
        };
        endDrag.callback.AddListener((x) => panel.EndTabLabelDrag(x as PointerEventData, tabLabelRectTransform));

        tabLabelEventTrigger.triggers.Add(beginDrag);
        tabLabelEventTrigger.triggers.Add(drag);
        tabLabelEventTrigger.triggers.Add(endDrag);
        return tabLabelInstance;
    }
}