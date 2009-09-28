using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MusicPlugin.Code.Attributes
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

    public class ExtAttribute : Attribute
    {
        private string _ext;

        public string Ext { get { return _ext; } }

        public ExtAttribute(string ext)
        {
            this._ext = ext;
        }
    }

    public class DescriptionAttribute : Attribute
    {
        private string _description;

        public string Description { get { return _description; } }

        public DescriptionAttribute(string description)
        {
            this._description = description;
        }
    }
}
