using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class EditorTabRegistry
{
    private static Dictionary<Type, (GameObject, Sprite)> m_editorInfoRegistry;
    private static Dictionary<Type, (GameObject, Sprite)> EditorInfoRegistry { get { return m_editorInfoRegistry ??= BuildInfoRegistryFromAssembly(); } }

    private static Dictionary<Type, (GameObject, Sprite)> BuildInfoRegistryFromAssembly()
    {
        var dict = new Dictionary<Type, (GameObject, Sprite)>();
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
            dict[type] = (prefab, sprite);
        }
        return dict;
    }

    public static Sprite GetIcon(Type editorTabTypeName)
    {
        if (!EditorInfoRegistry.ContainsKey(editorTabTypeName))
            return null;
        return EditorInfoRegistry[editorTabTypeName].Item2;
    }

    public static GameObject GetPrefab(Type editorTabTypeName)
    {
        if (!EditorInfoRegistry.ContainsKey(editorTabTypeName))
            return null;
        return EditorInfoRegistry[editorTabTypeName].Item1;
    }

    public static (string, Sprite) GetDescriptor(Type editorTabTypeName)
    {
        if (!EditorInfoRegistry.ContainsKey(editorTabTypeName))
            return ("null", Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f));
        var val = EditorInfoRegistry[editorTabTypeName];
        return (editorTabTypeName.Name, val.Item2);
    }
}

