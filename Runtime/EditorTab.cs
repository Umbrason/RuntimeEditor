using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class EditorTab : MonoBehaviour
{
    public GameObject labelGO;
    private DynamicPanel m_currentPanel;
    public DynamicPanel CurrentPanel { get { return m_currentPanel; } }

    public void MoveToPanel(DynamicPanel panel)
    {
        m_currentPanel = panel;
        labelGO.transform.SetParent(panel.editorTabLabelContainer.transform);
        transform.SetParent(panel.editorTabContentContainer.transform);
    }

    public abstract void OnShow();
    public abstract void OnHide();
    public abstract void OnCreate();
}
