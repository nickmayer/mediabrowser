using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Attributes
{
    public class GroupAttribute : Attribute
    {
        private string _group;

        public string Group { get { return _group; } }

        public GroupAttribute(string Group)
        {
            this._group = Group;
        }
    }
    
    public class ControlAttribute : Attribute
    {
        private Type _control;

        public Type Control { get { return _control; } }

        public ControlAttribute(Type control)
        {
            this._control = control;
        }
    }

    public class HiddenAttribute : Attribute
    {
        private bool _hidden;

        public bool Hidden { get { return _hidden; } }

        public HiddenAttribute(bool hidden)
        {
            this._hidden = hidden;
        }
    }

    public class ItemsAttribute : Attribute
    {
        private string _items;

        public string Items { get { return _items; } }

        public ItemsAttribute(string Items)
        {
            this._items = Items;
        }
    }

    public class ExtAttribute : Attribute
    {
        private string _ext;

        public string Ext { get { return _ext; } }

        public ExtAttribute(string ext)
        {
            this._ext = ext;
        }
    }

    public class LabelAttribute : Attribute
    {
        private string _label;

        public string Label { get { return _label; } }

        public LabelAttribute(string label)
        {
            this._label = label;
        }
    }

    public class DefaultAttribute : Attribute
    {
        private object _default;

        public object Default { get { return _default; } }

        public DefaultAttribute(object _default)
        {
            this._default = _default;
        }
    }
}
