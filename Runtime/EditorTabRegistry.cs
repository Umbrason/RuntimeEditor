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
            var prefab = Resources.Load<GameObject>($"EditorTabs/{type.Name}/Prefab");
            var sprite = Resources.Load<Sprite>($"EditorTabs/{type.Name}/Icon");
            if (prefab == null || sprite == null)
            {
                Debug.LogWarning($"missing assets for editor tab {type.Name}: {(prefab == null ? "prefab " : "")}{(sprite == null ? "sprite" : "")} \n add the missing assets to Resources/EditorTabs/{type.Name}");
                continue;
            }
            dict[type] = (prefab, sprite);
        }
        return dict;
    }

    public static Sprite GetIcon(Type editorType)
    {
        if (!EditorInfoRegistry.ContainsKey(editorType))
            return null;
        return EditorInfoRegistry[editorType].Item2;
    }

    public static GameObject GetPrefab(Type editorType)
    {
        if (!EditorInfoRegistry.ContainsKey(editorType))
            return null;
        return EditorInfoRegistry[editorType].Item1;
    }

    public static (string, Sprite) GetDescriptor(Type editorType)
    {
        if (!EditorInfoRegistry.ContainsKey(editorType))
            return ("null", Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f));
        var val = EditorInfoRegistry[editorType];
        return (editorType.Name, val.Item2);
    }
}

