using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EditorTabRegistry
{
    private static Dictionary<string, (GameObject, Sprite)> m_editorInfoRegistry;
    private static Dictionary<string, (GameObject, Sprite)> EditorInfoRegistry { get { return m_editorInfoRegistry ??= BuildInfoRegistryFromAssembly(); } }
    
    private static Dictionary<string, (GameObject, Sprite)> BuildInfoRegistryFromAssembly()
    {
        var dict = new Dictionary<string, (GameObject, Sprite)>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var types = assemblies.SelectMany(x => x.GetTypes());
        var editorTabTypes = types.Where(x => !x.IsAbstract && x.IsSubclassOf(typeof(EditorTab)));
        foreach (var type in editorTabTypes)
        {
            var prefab = Resources.Load<GameObject>($"EditorTabs/{type.Name}/{type.Name}_Prefab");
            var sprite = Resources.Load<Sprite>($"EditorTabs/{type.Name}/{type.Name}_Icon");
            if (prefab == null || sprite == null)
            {
                Debug.LogWarning($"missing assets for editor tab {type.Name}: {(prefab == null ? $"{type.Name}_Prefab " : "")}{(sprite == null ? $"{type.Name}_Icon" : "")} \n add the missing assets to Resources/EditorTabs/{type.Name}");
                continue;
            }
            dict[type.Name] = (prefab, sprite);
        }
        return dict;
    }

    public static Sprite GetIcon(string editorTabName)
    {
        if (!EditorInfoRegistry.ContainsKey(editorTabName))
            return null;
        return EditorInfoRegistry[editorTabName].Item2;
    }

    public static GameObject GetPrefab(string editorTabName)
    {
        if (!EditorInfoRegistry.ContainsKey(editorTabName))
            return null;
        return EditorInfoRegistry[editorTabName].Item1;
    }

    public static (string, Sprite) GetDescriptor(string editorTabName)
    {
        if (!EditorInfoRegistry.ContainsKey(editorTabName))
            return ("null", Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f));
        var val = EditorInfoRegistry[editorTabName];
        return (editorTabName, val.Item2);
    }
}

