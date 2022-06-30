using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;


public class MenuCategory : Selectable, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IMoveHandler, ICancelHandler, ISubmitHandler
{
    private static MenuCategory s_expandedCategory;
    private bool IsContentSelfActive { get { return content.gameObject.activeSelf; } }
    private bool pointerOver;
    public Transform content;


    public override void OnPointerDown(PointerEventData eventData)
    {
        if (IsContentSelfActive)
            CloseOptions();
        else
            ShowOptions();
        base.OnPointerDown(eventData);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            if (IsContentSelfActive && !pointerOver)
                CloseOptions();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        pointerOver = true;
        if (s_expandedCategory)
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
            ShowOptions();
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
        pointerOver = false;
    }

    public void ShowOptions()
    {
        if (s_expandedCategory && s_expandedCategory != this)
            s_expandedCategory.CloseOptions();
        s_expandedCategory = this;
        content.gameObject.SetActive(true);
    }

    public void CloseOptions()
    {
        if (EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
        s_expandedCategory = this == s_expandedCategory ? null : s_expandedCategory;
        content.gameObject.SetActive(false);
    }

    public override void OnSelect(BaseEventData eventData) { base.OnSelect(eventData); ShowOptions(); }
    public override Selectable FindSelectableOnDown() => content.GetComponentInChildren<Selectable>(true);

    public void OnCancel(BaseEventData eventData)
    {
        CloseOptions();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        CloseOptions();
    }
}

