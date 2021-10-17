using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EditorTabRegistry
{
    private static Dictionary<string, GameObject> EditorPrefabs;

    public static void Register(string identifier, GameObject prefab) => EditorPrefabs[identifier] = prefab;
    public static EditorTab InstantiateTabContent(string identifier, Transform parent = null)
    {
        if (!EditorPrefabs.ContainsKey(identifier))
            return null;
        var prefab = EditorPrefabs[identifier];
        var gameObject = GameObject.Instantiate(prefab, parent);
        return gameObject.GetComponent<EditorTab>();
    }

}
