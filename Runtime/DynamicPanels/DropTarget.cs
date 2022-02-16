using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class DropTarget : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    public readonly UnityEvent<PointerEventData> DropCallback = new UnityEvent<PointerEventData>();
    public readonly UnityEvent<PointerEventData> PointerEnterCallback = new UnityEvent<PointerEventData>();
    public readonly UnityEvent<PointerEventData> PointerMoveCallback = new UnityEvent<PointerEventData>();
    public readonly UnityEvent<PointerEventData> PointerExitCallback = new UnityEvent<PointerEventData>();
    public void OnDrop(PointerEventData eventData) => DropCallback?.Invoke(eventData);
    public void OnPointerEnter(PointerEventData eventData) => PointerEnterCallback?.Invoke(eventData);
    public void OnPointerMove(PointerEventData eventData) => PointerMoveCallback?.Invoke(eventData);
    public void OnPointerExit(PointerEventData eventData) => PointerExitCallback?.Invoke(eventData);

    private RectTransform rectTransform;
    private RectTransform RectTransform { get { return rectTransform ??= GetComponent<RectTransform>(); } }

    private Rect? worldSpaceRect;
    public Rect WorldSpaceRect { get { return worldSpaceRect ??= GetRect(); } }

    private Rect GetRect()
    {
        if (!RectTransform.hasChanged && worldSpaceRect.HasValue)
            return worldSpaceRect.Value;
        var corners = new Vector3[4];
        RectTransform.GetWorldCorners(corners);
        var min = corners[0];
        var max = corners[0];
        foreach (var vec in corners)
        {
            min = Vector3.Min(min, vec);
            max = Vector3.Max(max, vec);
        }
        var position = min;
        var size = max - min;
        return new Rect(position, size);
    }

    
}
