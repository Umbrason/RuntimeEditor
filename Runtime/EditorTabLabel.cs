using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EditorTabLabel : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
{
    private EditorTab m_tab;
    public EditorTab Tab { get { return m_tab; } set { m_tab ??= value; } }
    private RectTransform cached_rectTransform;
    public RectTransform RectTransform { get { return cached_rectTransform ??= GetComponent<RectTransform>(); } }

    private LayoutElement cached_LayoutElement;
    public LayoutElement LayoutElement { get { return cached_LayoutElement ??= GetComponent<LayoutElement>(); } }

    private CanvasGroup cached_CanvasGroup;
    public CanvasGroup CanvasGroup { get { return cached_CanvasGroup ??= GetComponent<CanvasGroup>(); } }

    private Canvas cached_Canvas;
    public Canvas Canvas { get { return cached_Canvas ??= GetComponent<Canvas>(); } }
    
    public void OnPointerClick(PointerEventData eventData) => Tab.CurrentPanel.SelectTab(Tab);

    public void OnBeginDrag(PointerEventData eventData)
    {
        CanvasGroup.blocksRaycasts = false;
        Canvas.sortingOrder = 32767;        
        LayoutElement.ignoreLayout = true;
    }
    public void OnDrag(PointerEventData eventData) => RectTransform.position = eventData.position;
    public void OnEndDrag(PointerEventData eventData)
    {
        CanvasGroup.blocksRaycasts = true;
        Canvas.sortingOrder = 1;
        LayoutElement.ignoreLayout = false;
        gameObject.SetActive(false);
        gameObject.SetActive(true);
    }

}
