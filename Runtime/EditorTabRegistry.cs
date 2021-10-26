using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class EditorTabRegistry
{
    private static Dictionary<string, (GameObject, Sprite)> EditorInfoRegistry = new Dictionary<string, (GameObject, Sprite)>();

    public static void Register(string identifier, GameObject prefab, Sprite icon) => EditorInfoRegistry[identifier.ToLower()] = (prefab, icon);

    public static Sprite GetIcon(string identifier)
    {
        if (!EditorInfoRegistry.ContainsKey(identifier.ToLower()))
            return null;
        return EditorInfoRegistry[identifier.ToLower()].Item2;
    }

    public static GameObject GetPrefab(string identifier)
    {
        if (!EditorInfoRegistry.ContainsKey(identifier.ToLower()))
            return null;
        return EditorInfoRegistry[identifier.ToLower()].Item1;
    }

    public static (string, Sprite) GetDescriptor(string identifier)
    {
        if (!EditorInfoRegistry.ContainsKey(identifier.ToLower()))
            return ("null", Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f));
        var val = EditorInfoRegistry[identifier.ToLower()];
        return (identifier, val.Item2);
    }
}
