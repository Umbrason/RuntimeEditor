using System.Collections;
using System.Collections.Generic;
using System;
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

    #region Template references
    public GameObject TabLabelTemplate;
    #endregion

    #region component References
    public LayoutElement layoutElement;
    public DropTarget tabDropTarget;
    public DropTarget viewportDropTarget;
    public GameObject tabBar;
    public GameObject viewport;
    public GameObject childContainer;
    public FlexibleGridLayoutGroup childLayout;

    private List<EditorTab> tabs = new List<EditorTab>();
    private Dictionary<EditorTab, EventTrigger> tabButtons = new Dictionary<EditorTab, EventTrigger>();

    private RectTransform _rectTransform;
    public RectTransform RectTransform { get { return _rectTransform ??= GetComponent<RectTransform>(); } }

    private DynamicPanel parent;
    public DynamicPanel Parent { get { return parent; } }
    private (DynamicPanel, DynamicPanel) children;
    public bool HasChildren { get { return children.Item1 != null && children.Item2 != null; } }

    #endregion

    #region runtime-values
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
    private Vector2 dragPointerOffset;
    #endregion

    public enum PanelRegion
    {
        Center, Left, Top, Right, Bottom
    }

    private void OnEnable()
    {
        tabDropTarget.DropCallback.AddListener(OnDropTab);
        viewportDropTarget.DropCallback.AddListener(OnDropViewport);
    }

    private void OnDisable()
    {
        tabDropTarget.DropCallback.RemoveListener(OnDropTab);
        viewportDropTarget.DropCallback.RemoveListener(OnDropViewport);
    }

    private void InstantiateTabLabel(EditorTab tab)
    {
        var tabLabelInstance = Instantiate(TabLabelTemplate);
        var tabLabelEventTrigger = tabLabelInstance.GetComponent<EventTrigger>();
        var beginDrag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.BeginDrag
        };
        beginDrag.callback.AddListener((x) => BeginTabDrag(x as PointerEventData, tab));

        var drag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.Drag
        };
        drag.callback.AddListener((x) => DragTab(x as PointerEventData, tab));

        var endDrag = new EventTrigger.Entry()
        {
            eventID = EventTriggerType.EndDrag
        };
        endDrag.callback.AddListener((x) => EndTabDrag(x as PointerEventData, tab));

        tabLabelEventTrigger.triggers.Add(beginDrag);
        tabLabelEventTrigger.triggers.Add(drag);
        tabLabelEventTrigger.triggers.Add(endDrag);
    }

    private void BeginTabDrag(PointerEventData eventData, EditorTab tab)
    {
        dragPointerOffset = eventData.position - (Vector2)RectTransform.position;
    }

    private void DragTab(PointerEventData eventData, EditorTab tab)
    {
        RectTransform.position = eventData.position + dragPointerOffset;
    }

    private void EndTabDrag(PointerEventData eventData, EditorTab tab)
    {
        Debug.Log($"ended dragging this ({gameObject.GetInstanceID()}) object");
    }

    private void OnDropTab(PointerEventData eventData)
    {
        var editorTab = eventData.pointerDrag.GetComponentInChildren<EditorTab>();
        DockOtherTab(editorTab);
    }

    private void OnDropViewport(PointerEventData eventData)
    {
        var panelRegion = GetPanelRegion(RectTransform.worldToLocalMatrix * eventData.position);
        Debug.Log($"dropped {eventData.pointerDrag} on {panelRegion} region");
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
        B.transform.SetParent(childContainer.transform);
        childContainer.SetActive(true);
        viewport.SetActive(false);
    }

    public void RemoveChild(DynamicPanel child)
    {
        if (children.Item1 != child && children.Item2 != child)
            return;
        DynamicPanel otherChild = children.Item1 != child ? children.Item1 : children.Item2;
        foreach (var tab in otherChild.tabs)
            DockOtherTab(tab);
        childContainer.SetActive(false);
        viewport.SetActive(true);
    }

    ///<summary>Add new EditorTab to this panel</summary>
    public void DockOtherTab(EditorTab other)
    {
        if (tabs.Contains(other))
            return;
        //copy GO from other to this
    }

    public void Split(EditorTab other, PanelRegion splitDirection)
    {

    }

    ///<summary></summary>
    public void SeparateEditorTab(EditorTab tab)
    {
        if (!tabs.Contains(tab))
            return;
        throw new NotImplementedException();
    }

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
