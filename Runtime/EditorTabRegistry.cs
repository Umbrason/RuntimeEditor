using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EditorTabRegistry
{
    private static Dictionary<string, (GameObject, Sprite)> EditorInfoRegistry = new Dictionary<string, (GameObject, Sprite)>();

    public static void Register(string identifier, GameObject prefab, Sprite icon) => EditorInfoRegistry[identifier] = (prefab, icon);

    public static Sprite GetIcon(string identifier)
    {
        if (!EditorInfoRegistry.ContainsKey(identifier))
            return null;
        return EditorInfoRegistry[identifier].Item2;
    }

    public static GameObject GetPrefab(string identifier)
    {
        if (!EditorInfoRegistry.ContainsKey(identifier))
            return null;
        return EditorInfoRegistry[identifier].Item1;
    }

    public static (string, Sprite) GetDescriptor(string identifier)
    {
        if (!EditorInfoRegistry.ContainsKey(identifier))
            return ("null", Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f));
        var val = EditorInfoRegistry[identifier];
        return (identifier, val.Item2);
    }
}
