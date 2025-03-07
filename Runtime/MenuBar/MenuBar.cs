using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;

namespace Game.Editor
{
    public class MenuBar : MonoBehaviour
    {
        private static readonly string[] IGNORED_ASSEMBLY_PREFIXES = {
        "UnityEditor",
        "UnityEngine",
        "Unity",
        "System",
        "mscorlib"
        };

        private static MenuBar m_Singleton;
        public static MenuBar Singleton { get { return m_Singleton; } }

        public Transform content;
        private Queue<GameObject> categoryInstances = new Queue<GameObject>();
        public GameObject optionTemplate, categoryTemplate, groupTemplate;
        private Dictionary<(MenuBarOption.Category, string), Node> dynamicNodes = new Dictionary<(MenuBarOption.Category, string), Node>();
        private Node root;

        private void OnEnable()
        {
            m_Singleton ??= this;
            enabled = this == m_Singleton;
            if (!enabled)
                return;
            FetchOptionsFromAssembly();
            RefreshOptionsUI();
        }

        private void OnDisable() => m_Singleton = this == m_Singleton ? null : m_Singleton;

        private void FetchOptionsFromAssembly()
        {
            root = new Node("Root", 0);
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !IGNORED_ASSEMBLY_PREFIXES.Any(prefix => assembly.FullName.StartsWith(prefix)));
            var methods = assemblies.SelectMany((assembly) => assembly.GetTypes())
                                    .SelectMany(type => type.GetMethods())
                                    .Where(method => method.IsStatic && method.GetCustomAttributes(typeof(MenuBarOption), false).Any());
            foreach (var method in methods)
            {
                if (!(method.IsStatic))
                    continue;

                var menuUptionAttributes = method.GetCustomAttributes(typeof(MenuBarOption), false).Select((x) => (MenuBarOption)x).ToArray();
                if (!(menuUptionAttributes.Length > 0))
                    continue;
                var runtimeMenuOptionAttribute = menuUptionAttributes.SingleOrDefault();
                var key = runtimeMenuOptionAttribute.key;
                var category = runtimeMenuOptionAttribute.category;
                UnityAction callback = () => method.Invoke(null, null);
                AddOption(key, category, callback);
            }
        }

        public void AddDynamicNode(string path, MenuBarOption.Category category, UnityAction callback)
        {
            if (dynamicNodes.ContainsKey((category, path)))
                return;
            dynamicNodes.Add((category, path), AddOption(path, category, callback));
            RefreshOptionsUI();
        }

        public void RemoveDynamicNode(string path, MenuBarOption.Category category)
        {
            if (!dynamicNodes.ContainsKey((category, path)))
                return;
            RemoveOption(path, category);
            dynamicNodes.Remove((category, path));
            RefreshOptionsUI();
        }

        private Node AddOption(string path, MenuBarOption.Category category, UnityAction callback)
        {
            var keys = path.Split('/');
            var pathQueue = new Queue<string>(keys);
            var parentNode = root;
            //Traverse or Append
            while (pathQueue.Count > 1)
            {
                var key = pathQueue.Dequeue();
                if (parentNode.children.Any((x) => x.key == key))
                    parentNode = parentNode.children.Single((x) => x.key == key);
                else
                {
                    var child = new Node(key, category);
                    parentNode.children.Add(child);
                    parentNode = child;
                }
            }
            var node = new Node(pathQueue.Dequeue(), category, callback);
            parentNode.children.Add(node);
            return node;
        }

        private void RemoveOption(string path, MenuBarOption.Category category)
        {
            var keys = path.Split('/');
            var pathQueue = new Queue<string>(keys);
            var node = root;
            var parentStack = new Stack<Node>();
            //Traverse or Append
            while (pathQueue.Count > 1)
            {
                var key = pathQueue.Dequeue();
                if (node.children.Any((x) => x.key == key))
                {
                    node = node.children.Single((x) => x.key == key);
                    parentStack.Push(node);
                }
                else break;
            }
            //Clear node and potential empty parents
            while (parentStack.Count > 0)
            {
                var child = node;
                if (node.children.Count > 0 || node.HasOptionCallback)
                    break;
                node = parentStack.Pop();
                node.children.Remove(node);
            }
        }

        private void RefreshOptionsUI()
        {
            while (categoryInstances.Count > 0)
                Destroy(categoryInstances.Dequeue());
            var categories = System.Enum.GetValues(typeof(MenuBarOption.Category)).Cast<MenuBarOption.Category>();
            foreach (var category in categories)
            {
                var categoryMembers = root.children.Where((x) => x.category == category);
                if (categoryMembers.Count() == 0)
                    continue;
                var categoryInstance = InstantiateCategory(category.ToString());
                var categoryComponent = categoryInstance.GetComponent<MenuCategory>();
                categoryInstances.Enqueue(categoryInstance);
                foreach (var node in categoryMembers)
                    InstantiateNodeRecursively(node, categoryComponent.content);
            }
        }

        private void InstantiateNodeRecursively(Node node, Transform parent)
        {
            if (node.IsGroup)
            {
                var groupInstance = InstantiateGroup(node.key, parent).transform;
                var groupComponent = groupInstance.GetComponent<MenuOptionGroup>();
                foreach (var child in node.children)
                    InstantiateNodeRecursively(child, groupComponent.content);
            }
            if (!node.HasOptionCallback)
                return;
            InstantiateOption(node.key, node.callback, parent);
        }
        private GameObject InstantiateCategory(string key)
        {
            var categoryInstance = Instantiate(categoryTemplate, content);
            var Text = categoryInstance.GetComponentInChildren<Text>();
            Text.text = key;
            return categoryInstance;
        }

        private GameObject InstantiateGroup(string key, Transform parent)
        {
            var groupInstance = Instantiate(groupTemplate, parent);
            var Text = groupInstance.GetComponentInChildren<Text>();
            Text.text = key;
            return groupInstance;
        }

        private void InstantiateOption(string key, UnityAction callback, Transform parent = null)
        {
            var optionInstance = Instantiate(optionTemplate, parent);
            var text = optionInstance.GetComponentInChildren<Text>();
            text.text = key;
            var menuOption = optionInstance.GetComponent<MenuOption>();
            menuOption.onClick.AddListener(callback);
        }

        private struct Node
        {
            public string key;
            public MenuBarOption.Category category;
            public List<Node> children;
            public UnityAction callback;
            public bool HasOptionCallback { get { return callback != null; } }
            public bool IsGroup { get { return children.Count > 0; } }
            public Node(string key, MenuBarOption.Category category, UnityAction callback = null)
            {
                this.key = key;
                this.category = category;
                children = new List<Node>();
                this.callback = callback;
            }
        }
    }
}