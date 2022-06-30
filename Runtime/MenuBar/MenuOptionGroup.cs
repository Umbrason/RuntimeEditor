using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class MenuOptionGroup : Selectable, IPointerEnterHandler, IPointerExitHandler, IMoveHandler, ISubmitHandler, ICancelHandler
{
    public Transform content;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        ShowOptions();
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        CloseOptions();
    }

    public override void OnSelect(BaseEventData eventData)
    {
        ShowOptions();
        base.OnSelect(eventData);
    }

    private void ShowOptions()
    {
        content.gameObject.SetActive(true);
    }

    private void CloseOptions()
    {
        content.gameObject.SetActive(false);
    }

    public override Selectable FindSelectableOnUp()
    {
        for (int i = 0; i < transform.GetSiblingIndex(); i++)
            if (transform.parent.GetChild(i).GetComponentsInChildren<Selectable>() != null)
                return base.FindSelectableOnUp();
        return transform.parent?.GetComponentInParent<Selectable>();
    }
    public override Selectable FindSelectableOnDown() => FindSiblingOption(+1);
    public override Selectable FindSelectableOnLeft() => FindNeighbourCategory(-1);
    public override Selectable FindSelectableOnRight() => content?.GetComponentInChildren<Selectable>();

    private Selectable FindSiblingOption(int indexOffset)
    {
        return transform.parent?
        .GetChild(
            (
                transform.GetSiblingIndex()
                + indexOffset
                + transform.parent.childCount
            )
            % transform.parent.childCount
        )
        .GetComponentInChildren<Selectable>();
    }

    private MenuCategory FindNeighbourCategory(int indexOffset)
    {
        var category = GetComponentInParent<MenuCategory>();
        return category?
            .transform.parent?
            .GetChild(
                (
                    category.transform.GetSiblingIndex()
                    + indexOffset
                    + category.transform.parent.childCount
                )
                % category.transform.parent.childCount
            )
            .GetComponentInChildren<MenuCategory>();
    }

    public override void OnMove(AxisEventData eventData)
    {
        switch (eventData.moveDir)
        {
            case MoveDirection.Right:
                ShowOptions();
                break;
            case MoveDirection.Up:
            case MoveDirection.Down:
                CloseOptions();
                break;
        }
        base.OnMove(eventData);
    }

    public void OnCancel(BaseEventData eventData)
    {
        SendMessageUpwards("CloseOptions");
    }

    public void OnSubmit(BaseEventData eventData)
    {
        SendMessageUpwards("CloseOptions");
    }
}
