using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


[RequireComponent(typeof(RectTransform))]
public class DynamicPanel : MonoBehaviour, IDragHandler
{
    //can resize -> two modes: window, docked. when docked just let parent handle relative size. 
    //parent has following layout: child A, child B.
    //
    //can keep minimum size
    //
    //Contains (Multiple) Editor(s)
    //
    //can merge with other panel
    //
    //can split off editor tab into new panel

    //when editor tab is dropped onto tab bar -> add to tabs
    //when editor tab is dropped onto viewport -> 1. instantiate new panel ("parent"). 2. make "parent"'s parent the same as this panels 3. parent this to "parent" 4.create new panel with only dropped editor tab and parent it to "parent". Set parent split-orientation and child-order according to drop position 


    #region component References
    public LayoutElement layoutElement;
    public DropTarget tabDropTarget;
    public DropTarget viewportDropTarget;
    public GameObject editorTabLabelContainer;
    public GameObject editorTabContentContainer;
    public GameObject viewportContainer;
    public GameObject childContainer;
    public FlexibleGridLayoutGroup childLayout;

    private List<EditorTab> tabs = new List<EditorTab>();
    public string[] DockedTabNames { get { return tabs.Select((x) => x.GetType().Name).ToArray(); } }
    private Dictionary<EditorTab, EventTrigger> tabButtons = new Dictionary<EditorTab, EventTrigger>();

    private RectTransform _rectTransform;
    public RectTransform RectTransform { get { return _rectTransform ??= GetComponent<RectTransform>(); } }

    private DynamicPanel parent;
    public DynamicPanel Parent { get { return parent; } }
    private (DynamicPanel, DynamicPanel) children;
    public DynamicPanel ChildA { get { return children.Item1; } }
    public DynamicPanel ChildB { get { return children.Item2; } }
    public bool HasChildren { get { return children.Item1 != null && children.Item2 != null; } }
    public bool IsLeaf { get { return !HasChildren; } }

    #endregion

    #region runtime_values
    private float _splitPercent = .5f;
    public float SplitPercent
    {
        get { return _splitPercent; }
        set
        {
            _splitPercent = value;
            if (!HasChildren)
                return;
            switch (_splitOrientation)
            {
                case SplitOrientation.Horizontal:
                    children.Item1.layoutElement.flexibleHeight = _splitPercent * 100;
                    children.Item2.layoutElement.flexibleHeight = (1 - _splitPercent) * 100;
                    break;
                case SplitOrientation.Vertical:
                    children.Item1.layoutElement.flexibleWidth = _splitPercent * 100;
                    children.Item2.layoutElement.flexibleWidth = (1 - _splitPercent) * 100;
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
            if (!childLayout)
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
    #endregion

    public enum PanelRegion
    {
        Center, Left, Top, Right, Bottom
    }

    private void OnEnable()
    {
        tabDropTarget.DropCallback.AddListener(OnDropTabListArea);
        viewportDropTarget.DropCallback.AddListener(OnDropViewportArea);
    }

    private void OnDisable()
    {
        tabDropTarget.DropCallback.RemoveListener(OnDropTabListArea);
        viewportDropTarget.DropCallback.RemoveListener(OnDropViewportArea);
    }

    private void OnDropViewportArea(PointerEventData eventData)
    {
        var panelRegion = GetPanelRegion(RectTransform.worldToLocalMatrix * eventData.position);
        Debug.Log($"dropped {eventData.pointerDrag} on {panelRegion} region");
    }

    private void OnDropTabListArea(PointerEventData eventData)
    {
        var editorTab = eventData.pointerDrag.GetComponentInChildren<EditorTab>();
        AppendTab(editorTab);
    }

    private PanelRegion GetPanelRegion(Vector2 localPosition)
    {
        Vector2 normalizedPosition = localPosition / tabDropTarget.WorldSpaceRect.size;
        var margin = 0.25f;
        if (normalizedPosition.x < margin)
            return PanelRegion.Left;
        if (normalizedPosition.x > 1f - margin)
            return PanelRegion.Right;
        if (normalizedPosition.x > 1f - margin)
            return PanelRegion.Top;
        if (normalizedPosition.y < margin)
            return PanelRegion.Bottom;
        return PanelRegion.Center;
    }


    //needs to be on the top level to allow for rendering the label on top (could have a new label dedicated to being dragged like the context menu -> similar to VS code tab drag) 
    #region Label Drag Mechanic    
    private Vector2 dragPointerOffset;

    public void BeginTabLabelDrag(PointerEventData eventData, RectTransform labelTransform)
        => dragPointerOffset = eventData.position - (Vector2)labelTransform.position;

    public void DragTabLabel(PointerEventData eventData, RectTransform labelTransform)
        => labelTransform.position = eventData.position + dragPointerOffset;

    public void EndTabLabelDrag(PointerEventData eventData, RectTransform labelTransform)
        => Debug.Log($"ended dragging this ({gameObject.GetInstanceID()}) object");
    #endregion

    public void SetChildren(DynamicPanel A, DynamicPanel B, SplitOrientation splitOrientation = SplitOrientation.Horizontal, float splitPercent = .5f)
    {
        if (tabs.Count > 0)
            return;
        children = (A, B);

        SplitOrientation = splitOrientation;
        SplitPercent = splitPercent;
        A.parent = this;
        A.name = this.name + "A";

        B.parent = this;
        B.name = this.name + "B";
        A.transform.SetParent(childContainer.transform);
        A.transform.localScale = Vector3.one;
        B.transform.SetParent(childContainer.transform);
        B.transform.localScale = Vector3.one;
        childContainer.SetActive(true);
        viewportContainer.SetActive(false);
    }

    public void RemoveChild(DynamicPanel child)
    {
        if (children.Item1 != child && children.Item2 != child)
            return;
        DynamicPanel otherChild = children.Item1 != child ? children.Item1 : children.Item2;
        foreach (var tab in otherChild.tabs)
            AppendTab(tab);
        childContainer.SetActive(false);
        viewportContainer.SetActive(true);
    }

    ///<summary>Add new EditorTab to this panel</summary>
    public void AppendTab(EditorTab tab)
    {
        if (HasChildren || tabs.Contains(tab))
            return;
        tabs.Add(tab);
        tab.MoveToPanel(this);
    }

    public void Split(EditorTab other, PanelRegion splitDirection)
    {

    }

    public void SelectTab(EditorTab tab)
    {
        if (!tabs.Contains(tab))
            return;
        int index = tabs.IndexOf(tab);
        selectedTab = index;
        for (int i = 0; i < tabs.Count; i++)
            tabs[i].gameObject.SetActive(i == index);
    }

    ///<summary>allows editor tab to separate from this panel</summary>
    public void SeparateEditorTab(EditorTab tab)
    {
        if (!tabs.Contains(tab))
            return;
        throw new NotImplementedException();
    }

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
}
