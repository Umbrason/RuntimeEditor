using UnityEngine;

public abstract class EditorTab : MonoBehaviour
{
    private EditorTabLabel m_label;
    public EditorTabLabel Label { get { return m_label; } }
    private DynamicPanel m_currentPanel;
    public DynamicPanel CurrentPanel { get { return m_currentPanel; } }

    public void RegisterNewParentPanel(DynamicPanel panel) => m_currentPanel = panel;    
    public void RegisterTabLabel(EditorTabLabel tabLabel)
    {
        if (m_label) { Debug.LogError("Already has a label assigned"); return; }
        m_label = tabLabel;
        m_label.Tab = this;
    }
}
