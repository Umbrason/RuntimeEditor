using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(RectTransform))]
public class DynamicPanel : MonoBehaviour, IDragHandler, IPointerClickHandler
{
    private const float PANEL_REGION_MARGIN = .3f;
    public enum PanelRegion
    {
        Center, Left, Top, Right, Bottom
    }
    #region Inspector References
    [SerializeField] private DropTarget tabBarDropTarget;
    [SerializeField] private DropTarget tabViewportDropTarget;
    [SerializeField] private GameObject editorTabLabelContainer;
    [SerializeField] private GameObject editorTabContentContainer;
    [SerializeField] private GameObject viewportContainer;
    [SerializeField] private GameObject childContainer;
    [SerializeField] private FlexibleGridLayoutGroup childLayout;
    [SerializeField] private FlexibleGridLayoutGroup viewportLayout;
    [SerializeField] private GameObject previewViewportPlaceholder;
    [SerializeField] private LayoutElement previewTabLabelPlaceholder;
    #endregion

    #region Cached Components 
    private LayoutElement cached_LayoutElement;
    public LayoutElement LayoutElement { get { return cached_LayoutElement ??= GetComponent<LayoutElement>(); } }

    private RectTransform cached_RectTransform;
    public RectTransform RectTransform { get { return cached_RectTransform ??= GetComponent<RectTransform>(); } }
    #endregion

    #region LayoutTree data
    private List<EditorTab> editorTabs = new List<EditorTab>();
    public EditorTab[] DockedTabs { get { return editorTabs.ToArray(); } }

    private DynamicPanel parent;
    public DynamicPanel Parent { get { return parent; } }
    #endregion

    #region Getter Properties
    public string[] DockedTabNames { get { return DockedTabs.Select((x) => x.GetType().Name).ToArray(); } }
    public DynamicPanel Sibling { get { return Parent ? Parent.ChildA == this ? Parent.ChildB : Parent.ChildA : null; } }
    private (DynamicPanel, DynamicPanel) children;
    public DynamicPanel ChildA { get { return children.Item1; } }
    public DynamicPanel ChildB { get { return children.Item2; } }
    public DynamicPanel[] LeafPanels { get { return IsLeaf ? new DynamicPanel[] { this } : ChildA.LeafPanels.Concat(ChildB.LeafPanels).ToArray(); } }
    public bool HasChildren { get { return children.Item1 != null && children.Item2 != null; } }
    public bool IsLeaf { get { return !HasChildren; } }

    #endregion

    #region Runtime Values

    private float _splitPercent = .5f;
    public float SplitPercent
    {
        get { return _splitPercent; }
        set
        {
            _splitPercent = Mathf.Clamp01(value * 1.2f - .1f) * .8f + .1f;
            if (!HasChildren)
                return;
            switch (_splitOrientation)
            {
                case SplitOrientation.Horizontal:
                    children.Item1.LayoutElement.flexibleHeight = _splitPercent * 100;
                    children.Item2.LayoutElement.flexibleHeight = (1 - _splitPercent) * 100;
                    break;
                case SplitOrientation.Vertical:
                    children.Item1.LayoutElement.flexibleWidth = _splitPercent * 100;
                    children.Item2.LayoutElement.flexibleWidth = (1 - _splitPercent) * 100;
                    break;
            }

        }
    }
    private SplitOrientation _splitOrientation;
    public SplitOrientation SplitOrientation
    {
        get { return _splitOrientation; }
        set
        {
            _splitOrientation = value;
            if (!(childLayout && HasChildren))
                return;
            switch (value)
            {
                case SplitOrientation.Horizontal:
                    childLayout.fitType = FlexibleGridLayoutGroup.FitType.MaxRows;
                    childLayout.rows = 2;
                    break;
                case SplitOrientation.Vertical:
                    childLayout.fitType = FlexibleGridLayoutGroup.FitType.MaxColumns;
                    childLayout.columns = 2;
                    break;
            }
        }
    }
    private int selectedTab;
    public EditorTab SelectedTab { get { return editorTabs.Count > selectedTab ? editorTabs[selectedTab] : null; } set { selectedTab = editorTabs.Contains(value) ? editorTabs.IndexOf(value) : selectedTab; } }
    #endregion

    #region UI events & callbacks 
    private void OnEnable()
    {
        tabViewportDropTarget.PointerEnterCallback.AddListener(OnViewportPointerEnter);
        tabViewportDropTarget.PointerMoveCallback.AddListener(OnViewportPointerMove);
        tabViewportDropTarget.PointerExitCallback.AddListener(OnViewportPointerExit);
        tabViewportDropTarget.DropCallback.AddListener(OnDropViewportArea);
        tabBarDropTarget.DropCallback.AddListener(OnDropTabLabelArea);
        tabBarDropTarget.PointerEnterCallback.AddListener(OnTabLabelAreaPointerEnter);
        tabBarDropTarget.PointerMoveCallback.AddListener(OnTabLabelAreaPointerMove);
        tabBarDropTarget.PointerExitCallback.AddListener(OnTabLabelAreaPointerExit);
    }

    private void OnDisable()
    {
        tabViewportDropTarget.PointerEnterCallback.RemoveListener(OnViewportPointerEnter);
        tabViewportDropTarget.PointerMoveCallback.RemoveListener(OnViewportPointerMove);
        tabViewportDropTarget.PointerExitCallback.RemoveListener(OnViewportPointerExit);
        tabViewportDropTarget.DropCallback.RemoveListener(OnDropViewportArea);
        tabBarDropTarget.PointerEnterCallback.RemoveListener(OnTabLabelAreaPointerEnter);
        tabBarDropTarget.PointerMoveCallback.RemoveListener(OnTabLabelAreaPointerMove);
        tabBarDropTarget.PointerExitCallback.RemoveListener(OnTabLabelAreaPointerExit);
        tabBarDropTarget.DropCallback.RemoveListener(OnDropTabLabelArea);
    }


    private void OnViewportPointerEnter(PointerEventData eventData) => TryPreviewSplit(eventData);
    private void OnViewportPointerMove(PointerEventData eventData) => TryPreviewSplit(eventData);
    private void OnViewportPointerExit(PointerEventData eventData) => StopPreviewingSplit();

    private void OnTabLabelAreaPointerEnter(PointerEventData eventData) => TryPreviewTabLabelPosition(eventData);
    private void OnTabLabelAreaPointerMove(PointerEventData eventData) => TryPreviewTabLabelPosition(eventData);
    private void OnTabLabelAreaPointerExit(PointerEventData eventData) => StopPreviewingTabLabelPosition();

    private void OnDropTabLabelArea(PointerEventData eventData)
    {
        StopPreviewingSplit();
        var tabLabel = eventData?.pointerDrag?.GetComponent<EditorTabLabel>();
        if (!tabLabel) return;
        var tab = tabLabel.Tab;
        var oldPanel = tab.CurrentPanel;
        MoveEditorTabToThis(tabLabel.Tab);
        if (!oldPanel)
            return;
        if (oldPanel.IsLeaf && oldPanel.DockedTabs.Length == 0)
            oldPanel.MergeWithSibling();
    }

    private void OnDropViewportArea(PointerEventData eventData)
    {
        StopPreviewingSplit();
        if (!TryGetSplitParameters(eventData, out EditorTab splitTab, out PanelRegion splitRegion))
            return;
        Split(splitTab, splitRegion);
    }

    public void OnPointerClick(PointerEventData eventData) => RuntimeEditorWindowManager.Singleton.SelectedLeafPanel = this.IsLeaf ? this : RuntimeEditorWindowManager.Singleton.SelectedLeafPanel;
    ///<summary>handles dragging between panels to change split postion</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (HasChildren)
        {
            var localPosition = RectTransform.worldToLocalMatrix * eventData.position;
            var normalizedPosition = localPosition / RectTransform.rect.size;
            switch (SplitOrientation)
            {
                case SplitOrientation.Horizontal:
                    SplitPercent = 1 - normalizedPosition.y;
                    break;
                case SplitOrientation.Vertical:
                    SplitPercent = normalizedPosition.x;
                    break;
            }
        }
    }
    #endregion

    #region UI previewing
    TODO>>//Mimic this in the actual drop behaviour of TabLabels on the TabLabelBar
    private void TryPreviewTabLabelPosition(PointerEventData eventData)
    {
        var label = eventData.pointerDrag?.GetComponent<EditorTabLabel>();
        if (!label) return;
        var worldSpaceRect = new Rect(RectTransform.rect.position + (Vector2)RectTransform.position, RectTransform.rect.size);
        var localPosition = (eventData.position - worldSpaceRect.position);
        var index = TabIndexFromLocalX(localPosition.x);
        var previewLabelSiblingIndex = index + (index >= label.transform.GetSiblingIndex() ? 1 : 0);
        previewTabLabelPlaceholder.preferredWidth = label.RectTransform.rect.width;
        previewTabLabelPlaceholder.transform.SetSiblingIndex(previewLabelSiblingIndex);
        previewTabLabelPlaceholder.gameObject.SetActive(false);
        previewTabLabelPlaceholder.gameObject.SetActive(true);
    }

    private void StopPreviewingTabLabelPosition()
    {
        previewTabLabelPlaceholder.gameObject.SetActive(false);
    }

    private void TryPreviewSplit(PointerEventData eventData)
    {
        if (!TryGetSplitParameters(eventData, out EditorTab splitTab, out PanelRegion splitRegion))
        {
            StopPreviewingSplit();
            return;
        }
        var splitOrientation = (splitRegion == PanelRegion.Bottom || splitRegion == PanelRegion.Top) ? SplitOrientation.Horizontal : SplitOrientation.Vertical;
        var contentIsFirstInLayout = splitRegion == PanelRegion.Right || splitRegion == PanelRegion.Bottom;
        var split = 1 - PANEL_REGION_MARGIN;

        switch (splitOrientation)
        {
            case SplitOrientation.Horizontal:
                viewportLayout.fitType = FlexibleGridLayoutGroup.FitType.MaxRows;
                viewportLayout.rows = 2;
                editorTabContentContainer.GetComponent<LayoutElement>().flexibleHeight = split * 100;
                previewViewportPlaceholder.GetComponent<LayoutElement>().flexibleHeight = (1 - split) * 100;
                viewportLayout.SetLayoutHorizontal();
                break;
            case SplitOrientation.Vertical:
                viewportLayout.fitType = FlexibleGridLayoutGroup.FitType.MaxColumns;
                viewportLayout.columns = 2;
                editorTabContentContainer.GetComponent<LayoutElement>().flexibleWidth = split * 100;
                previewViewportPlaceholder.GetComponent<LayoutElement>().flexibleWidth = (1 - split) * 100;
                viewportLayout.SetLayoutVertical();
                break;
        }
        previewViewportPlaceholder.transform.SetSiblingIndex(contentIsFirstInLayout ? 0 : 1);
        previewViewportPlaceholder.transform.SetSiblingIndex(contentIsFirstInLayout ? 1 : 0);
        previewViewportPlaceholder.SetActive(true);
    }

    private void StopPreviewingSplit()
    {
        previewViewportPlaceholder.SetActive(false);
    }
    #endregion

    #region Utility / Getters
    private bool TryGetSplitParameters(PointerEventData eventData, out EditorTab splitTargetTab, out PanelRegion splitRegion)
    {
        var tabLabel = eventData?.pointerDrag?.GetComponent<EditorTabLabel>();
        splitTargetTab = tabLabel?.Tab;
        splitRegion = GetPanelRegion(eventData.position);
        if (!splitTargetTab || splitRegion == PanelRegion.Center || DockedTabs.Where(x => x != tabLabel.Tab).Count() == 0) return false;
        return true;
    }

    private int TabIndexFromLocalX(float localX)
    {
        var tabLabelSizeQueue = new Queue<float>(DockedTabs.Select(x => x.Label.RectTransform.rect.width));
        var index = 0;
        while (localX > 0 && tabLabelSizeQueue.Count > 0)
        {
            var width = tabLabelSizeQueue.Dequeue();
            if (localX < width)
                break;
            localX -= width;
            index++;
        }
        return index;
    }

    private PanelRegion GetPanelRegion(Vector2 pointerPosition)
    {
        var worldSpaceRect = new Rect(RectTransform.rect.position + (Vector2)RectTransform.position, RectTransform.rect.size);
        var normalizedPosition = (pointerPosition - worldSpaceRect.position) / worldSpaceRect.size;
        if (normalizedPosition.y > 1f - PANEL_REGION_MARGIN)
            return PanelRegion.Top;
        if (normalizedPosition.y < PANEL_REGION_MARGIN)
            return PanelRegion.Bottom;
        if (normalizedPosition.x < PANEL_REGION_MARGIN)
            return PanelRegion.Left;
        if (normalizedPosition.x > 1f - PANEL_REGION_MARGIN)
            return PanelRegion.Right;
        return PanelRegion.Center;
    }

    public EditorTab GetEditorTab(string typeName) => editorTabs.Where(x => x.GetType().Name == typeName).SingleOrDefault();
    public EditorTab GetEditorTabInChildren(string typeName)
    {
        if (IsLeaf) return GetEditorTab(typeName);
        var childATab = ChildA.GetEditorTabInChildren(typeName);
        return childATab ? childATab : ChildB.GetEditorTabInChildren(typeName);
    }
    #endregion

    #region LayoutTree Operations
    public void SetChildren(DynamicPanel A, DynamicPanel B, SplitOrientation splitOrientation = SplitOrientation.Horizontal, float splitPercent = .5f)
    {
        if (editorTabs.Count > 0 || !A || !B)
            return;
        children = (A, B);

        A.parent = this;
        A.name = this.name + "A";
        A.transform.SetParent(childContainer.transform);
        A.transform.localScale = Vector3.one;

        B.name = this.name + "B";
        B.parent = this;
        B.transform.SetParent(childContainer.transform);
        B.transform.localScale = Vector3.one;

        SplitOrientation = splitOrientation;
        SplitPercent = splitPercent;
        childContainer.SetActive(true);
        viewportContainer.SetActive(false);
    }

    public void ClearChildren()
    {
        Destroy(ChildA.gameObject);
        Destroy(ChildB.gameObject);
        children = (null, null);
        childContainer.SetActive(false);
        viewportContainer.SetActive(true);
    }

    private void Split(EditorTab splitTab, PanelRegion splitRegion)
    {
        if (!IsLeaf)
            return;
        var oldPanel = splitTab.CurrentPanel;
        if (oldPanel && oldPanel.DockedTabs.Where(x => x != splitTab).Count() == 0)
        {
            if (oldPanel.Sibling == this)
            {
                MergeWithSibling();
                Parent.Split(splitTab, splitRegion);
                return;
            }
            oldPanel.MergeWithSibling();
        }

        var splitOrientation = (splitRegion == PanelRegion.Bottom || splitRegion == PanelRegion.Top) ? SplitOrientation.Horizontal : SplitOrientation.Vertical;
        var oldContentRemainsInTabA = splitRegion == PanelRegion.Right || splitRegion == PanelRegion.Bottom;
        var otherTabs = DockedTabs.Where(x => x != splitTab).ToArray();
        var splitPercent = oldContentRemainsInTabA ? 1 - PANEL_REGION_MARGIN : PANEL_REGION_MARGIN;
        var tabsA = oldContentRemainsInTabA ? otherTabs : new EditorTab[] { splitTab };
        var tabsB = oldContentRemainsInTabA ? new EditorTab[] { splitTab } : otherTabs;

        var selectedTab = SelectedTab;
        var newPanelA = RuntimeEditorWindowManager.Singleton.InstantiateDynamicPanel(childContainer.transform);
        var newPanelB = RuntimeEditorWindowManager.Singleton.InstantiateDynamicPanel(childContainer.transform);
        foreach (var tab in tabsA) newPanelA.MoveEditorTabToThis(tab);
        foreach (var tab in tabsB) newPanelB.MoveEditorTabToThis(tab);
        newPanelA.SelectedTab = selectedTab;
        newPanelB.SelectedTab = selectedTab;
        if (RuntimeEditorWindowManager.Singleton.SelectedLeafPanel == this)
            RuntimeEditorWindowManager.Singleton.SelectedLeafPanel = newPanelA.DockedTabs.Contains(selectedTab) ? newPanelA : newPanelB;
        SetChildren(newPanelA, newPanelB, splitOrientation, splitPercent);
    }
    public void MergeWithSibling() => parent?.MergeChildren();
    public void MergeChildren() //needs to merge empty panel with non leaf panel as well
    {
        if (IsLeaf) return;
        var bothChildrenAreLeafs = ChildA.IsLeaf && ChildB.IsLeaf;
        if (bothChildrenAreLeafs)
            MergeTwoLeafChildren();
        else if (ChildA.IsLeaf || ChildB.IsLeaf)
            MergeLeafAndNonLeafChild();
    }

    private void MergeTwoLeafChildren()
    {
        var childA = ChildA;
        var childB = ChildB;
        children = (null, null);
        foreach (var tab in childA.DockedTabs.Concat(childB.DockedTabs))
            MoveEditorTabToThis(tab);

        var selectedPanel = RuntimeEditorWindowManager.Singleton.SelectedLeafPanel; //cache selected panel
        RuntimeEditorWindowManager.Singleton.SelectedLeafPanel = selectedPanel == childA || selectedPanel == childB ? this : selectedPanel; //set selected panel to this panel if a child was selected previously

        Destroy(childA.gameObject);
        Destroy(childB.gameObject);
        childContainer.SetActive(false);
        viewportContainer.SetActive(true);
    }

    private void MergeLeafAndNonLeafChild()
    {
        var leafChild = ChildA.IsLeaf ? ChildA : ChildB;
        var nonLeafChild = leafChild.Sibling;
        SetChildren(nonLeafChild.ChildA, nonLeafChild.ChildB, nonLeafChild.SplitOrientation, nonLeafChild.SplitPercent);
        var fallbackLeafPanel = LeafPanels[0];
        var selectedPanel = RuntimeEditorWindowManager.Singleton.SelectedLeafPanel; //cache selected panel
        RuntimeEditorWindowManager.Singleton.SelectedLeafPanel = selectedPanel == leafChild ? fallbackLeafPanel : selectedPanel; //set selected panel to fallback leaf panel if a child was selected previously

        foreach (var tab in leafChild.DockedTabs)
            fallbackLeafPanel.MoveEditorTabToThis(tab);
        Destroy(leafChild.gameObject);
        Destroy(nonLeafChild.gameObject);
        childContainer.SetActive(true);
        viewportContainer.SetActive(false);
    }
    #endregion

    #region Editor Tab Management
    ///<summary>Add new EditorTab to this panel</summary>
    public void MoveEditorTabToThis(EditorTab tab)
    {
        if (!tab || HasChildren)
            return;
        tab.CurrentPanel?.DetachTab(tab);
        editorTabs.Add(tab);
        tab.Label.transform.SetParent(null);
        tab.Label.transform.SetParent(editorTabLabelContainer.transform);
        tab.transform.SetParent(editorTabContentContainer.transform);
        tab.RegisterNewParentPanel(this);
    }

    private void DetachTab(EditorTab tab)
    {
        if (!tab || !editorTabs.Contains(tab))
            return;
        editorTabs.Remove(tab);
    }

    public void SelectTab(EditorTab tab)
    {
        if (!editorTabs.Contains(tab))
            return;
        int index = editorTabs.IndexOf(tab);
        selectedTab = index;
        for (int i = 0; i < editorTabs.Count; i++)
            editorTabs[i].gameObject.SetActive(i == index);
    }
    #endregion
}