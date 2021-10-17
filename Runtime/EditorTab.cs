using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class EditorTab : MonoBehaviour
{
    public RectTransform labelTransform;
    public RectTransform contentTransform;
    public DynamicPanel containerPanel;

    #region Label Drag Mechanic
    private Vector2 dragPointerOffset;
    public void BeginTabLabelDrag(PointerEventData eventData)
        => dragPointerOffset = eventData.position - (Vector2)labelTransform.position;

    public void DragTabLabel(PointerEventData eventData)
        => labelTransform.position = eventData.position + dragPointerOffset;

    public void EndTabLabelDrag(PointerEventData eventData)
        => Debug.Log($"ended dragging this ({gameObject.GetInstanceID()}) object");
    #endregion
    public abstract void OnShow();
    public abstract void OnHide();
    public abstract void OnCreate();
}
