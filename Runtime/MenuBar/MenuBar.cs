using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class MenuBar : MonoBehaviour
{
    private bool expanded;
    public Transform content;
    private Queue<GameObject> categoryInstances = new Queue<GameObject>();
    public GameObject optionTemplate, categoryTemplate, groupTemplate;

    private Node root;

    private void OnEnable()
    {
        //content = transform.SearchFirstByName("content");
        FetchOptionsFromAssembly();
        RefreshOptionsUI();
    }

    private void FetchOptionsFromAssembly()
    {
        root = new Node("Root", 0);
        var types = GetType().Assembly.GetTypes();
        foreach (var type in types)
        {
            foreach (var method in type.GetMethods())
            {
                if (!(method.IsStatic))
                    continue;

                var menuUptionAttributes = method.GetCustomAttributes(typeof(MenuOption), false).Select((x) => (MenuOption)x).ToArray();
                if (!(menuUptionAttributes.Length > 0))
                    continue;
                var runtimeMenuOptionAttribute = menuUptionAttributes.SingleOrDefault();
                var key = runtimeMenuOptionAttribute.key;
                var category = runtimeMenuOptionAttribute.category;

                UnityAction callback = () => method.Invoke(null, null);
                AddOption(key, category, callback);
            }
        }
    }

    private void AddOption(string path, MenuOption.Category category, UnityAction callback)
    {
        var keys = path.Split('/');
        var pathQueue = new Queue<string>(keys);
        var node = root;
        //Traverse or Append
        while (pathQueue.Count > 1)
        {
            var key = pathQueue.Dequeue();
            if (node.children.Any((x) => x.key == key))
                node = node.children.Single((x) => x.key == key);
            else
            {
                var child = new Node(key, category);
                node.children.Add(child);
                node = child;
            }
        }
        node.children.Add(new Node(pathQueue.Dequeue(), category, callback));
    }

    private void RefreshOptionsUI()
    {
        while (categoryInstances.Count > 0)
            Destroy(categoryInstances.Dequeue());
        var categories = System.Enum.GetValues(typeof(MenuOption.Category)).Cast<MenuOption.Category>();
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
        var button = optionInstance.GetComponent<Button>();
        button.onClick.AddListener(callback);
        button.onClick.AddListener(() => button.SendMessageUpwards("CloseOptions"));
    }

    private struct Node
    {
        public string key;
        public MenuOption.Category category;
        public List<Node> children;
        public UnityAction callback;
        public bool HasOptionCallback { get { return callback != null; } }
        public bool IsGroup { get { return children.Count > 0; } }
        public Node(string key, MenuOption.Category category, UnityAction callback = null)
        {
            this.key = key;
            this.category = category;
            children = new List<Node>();
            this.callback = callback;
        }
    }
}
