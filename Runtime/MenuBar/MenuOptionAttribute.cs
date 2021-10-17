using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MenuOption : Attribute
    {
        public string key;        
        public Category category;
        public enum Category
        {
            Default, Project, Layout, Assets

        }
        public MenuOption(Category category, string key)
        {
            this.key = key;
            this.category = category;
        }
    }
