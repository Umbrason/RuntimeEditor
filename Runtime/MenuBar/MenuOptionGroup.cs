using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;


    public class MenuOptionGroup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Transform content;

        private void OnEnable()
        {
            //content = transform.SearchFirstByName("Content");
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowOptions();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideOptions();
        }

        private void ShowOptions()
        {
            content.gameObject.SetActive(true);
        }

        private void HideOptions()
        {
            content.gameObject.SetActive(false);
        }
    }
