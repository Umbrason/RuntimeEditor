using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Game.Editor
{
    public class MenuCategory : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private bool expanded;
        private bool pointerOver;
        public Transform content;


        private void OnEnable()
        {
            //content = transform.SearchFirstByName("Content");
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ShowOptions();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
                if (expanded && !pointerOver)
                    CloseOptions();
            expanded = content.gameObject.activeSelf;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerOver = false;
        }

        private void ShowOptions()
        {
            content.gameObject.SetActive(true);
        }

        private void CloseOptions()
        {
            content.gameObject.SetActive(false);
        }
    }

}