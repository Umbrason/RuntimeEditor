using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EditorTabLabel : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    private EditorTab m_tab;
    public EditorTab Tab { get { return m_tab; } set { m_tab ??= value; } }
    private RectTransform m_rectTransform;
    public RectTransform RectTransform { get { return m_rectTransform ??= GetComponent<RectTransform>(); } }
    private CanvasGroup m_CanvasGroup;
    public CanvasGroup CanvasGroup { get { return m_CanvasGroup ??= GetComponent<CanvasGroup>(); } }

    private Vector2 dragPointerOffset;
    private Transform preDragParentTransform;

    public void OnPointerClick(PointerEventData eventData) => Tab.CurrentPanel.SelectTab(Tab);

    public void OnBeginDrag(PointerEventData eventData)
    {
        CanvasGroup.blocksRaycasts = false;
        dragPointerOffset = (Vector2)RectTransform.position - eventData.position;
        preDragParentTransform = transform.parent;
    }
    public void OnDrag(PointerEventData eventData) => RectTransform.position = eventData.position /*+dragPointerOffset*/;
    public void OnEndDrag(PointerEventData eventData)
    {
        dragPointerOffset = Vector2.zero;
        CanvasGroup.blocksRaycasts = true;
        if (transform.parent != preDragParentTransform)
            return;
        transform.SetParent(null);
        transform.SetParent(preDragParentTransform);
    }

}
