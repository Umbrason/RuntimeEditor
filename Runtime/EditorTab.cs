using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EditorTab : MonoBehaviour
{
    public abstract void OnShow();
    public abstract void OnHide();
    public abstract void OnCreate();
    
}
