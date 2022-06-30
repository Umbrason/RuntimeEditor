using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Editor
{
    public class MenuOption : Selectable, ISubmitHandler, ICancelHandler
    {
        public UnityEvent onClick = new UnityEvent();

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            onClick.Invoke();
            SendMessageUpwards("CloseOptions");
        }

        public void OnSubmit(BaseEventData eventData)
        {
            onClick.Invoke();
            SendMessageUpwards("CloseOptions");
        }

        public void OnCancel(BaseEventData eventData)
        {
            SendMessageUpwards("CloseOptions");
        }

        public override Selectable FindSelectableOnUp()
        {
            for (int i = 0; i < transform.GetSiblingIndex(); i++)
                if (transform.parent.GetChild(i).GetComponentsInChildren<Selectable>() != null)
                    return base.FindSelectableOnUp();
            return transform.parent?.GetComponentInParent<Selectable>();
        }

        public override Selectable FindSelectableOnDown() => FindSiblingOption(+1);
        public override Selectable FindSelectableOnLeft() => FindNeighbourCategory(-1).FindSelectableOnDown();
        public override Selectable FindSelectableOnRight() => FindNeighbourCategory(+1).FindSelectableOnDown();

        public override void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Right || eventData.moveDir == MoveDirection.Left)
                FindNeighbourCategory(eventData.moveDir == MoveDirection.Right ? +1 : -1)?.ShowOptions();

            base.OnMove(eventData);
        }

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


    }
}