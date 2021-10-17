using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class EditorLayoutTree
{
    [System.NonSerialized, System.Xml.Serialization.XmlIgnore]
    public EditorLayoutTree parent;
    public EditorLayoutTree childA, childB;

    public bool IsLeaf { get { return (childA == null && childB == null); } }

    public float splitPosition; //position of split between child A and child B. e.g. .5f, .3f, .7f
    public SplitOrientation splitOrientation;

    public List<string> dockedTabs = new List<string>();


}
