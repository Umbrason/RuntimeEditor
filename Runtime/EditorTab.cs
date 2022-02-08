using UnityEngine;

public abstract class EditorTab : MonoBehaviour
{
    private GameObject labelGO;
    public GameObject LabelGO { get { return labelGO; } }
    private DynamicPanel m_currentPanel;
    public DynamicPanel CurrentPanel { get { return m_currentPanel; } }

    public void MoveToPanel(DynamicPanel panel)
    {
        m_currentPanel = panel;
        labelGO.transform.SetParent(panel.editorTabLabelContainer.transform);
        transform.SetParent(panel.editorTabContentContainer.transform);
    }

    public void RegisterTabLabel(GameObject GO)
    {
        if (labelGO) Debug.LogError("Already has a label assigned");
        labelGO ??= GO;
    }
}
