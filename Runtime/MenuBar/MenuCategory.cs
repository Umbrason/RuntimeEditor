using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;


public class MenuCategory : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    private static MenuCategory s_expandedCategory;
    private bool IsContentSelfActive { get { return content.gameObject.activeSelf; } }
    private bool pointerOver;
    public Transform content;


    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsContentSelfActive)
            CloseOptions();
        else
            ShowOptions();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            if (IsContentSelfActive && !pointerOver)
                CloseOptions();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        if (s_expandedCategory)
            ShowOptions();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
    }

    private void ShowOptions()
    {
        if (s_expandedCategory && s_expandedCategory != this)
            s_expandedCategory.CloseOptions();
        s_expandedCategory = this;
        content.gameObject.SetActive(true);
    }

    private void CloseOptions()
    {
        s_expandedCategory = this == s_expandedCategory ? null : s_expandedCategory;
        content.gameObject.SetActive(false);
    }
}

