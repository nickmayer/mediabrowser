using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml;
using MediaBrowser.Code.ShadowTypes;
using System.Diagnostics;
using System.Collections;
using MediaBrowser.Util;

namespace MediaBrowser.Library.Persistance {


    [global::System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class SkipFieldAttribute : Attribute {

        // This is a positional argument
        public SkipFieldAttribute() {
        }
    }

    [global::System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class CommentAttribute : Attribute {

        string comment;

        // This is a positional argument
        public CommentAttribute(string comment) {
            this.comment = comment;
        }

        public string Comment {
            get { return comment; }
        }

    }

    public class XmlSettings<T> where T : class, new() {

        #region Serializers

        abstract class AbstractSerializer {
            public abstract object Read(XmlNode node, Type type);
            public abstract void Write(XmlNode node, object o);
            public abstract bool SupportsType(Type type);
        }


        class GenericObjectSerializer : AbstractSerializer {

            List<AbstractSerializer> serializers;


            public override object Read(XmlNode node, Type type) {
                object rval = type.GetConstructor(Type.EmptyTypes).Invoke(null);
                foreach (var member in SettingMembers(type)) {

                    var serializer = FindSerializer(member.Type);
                    XmlNode inner = node.SelectSingleNode(member.Name);

                    if (inner != null) {
                        member.Write(rval, serializer.Read(inner, member.Type));
                    }
                }
                return rval;
            }

            public override void Write(XmlNode node, object item) {
                if (item != null) {
                    foreach (var member in SettingMembers(item.GetType())) {

                        var serializer = FindSerializer(member.Type);
                        XmlNode inner = node.SelectSingleNode(member.Name);

                        if (inner == null) {
                            inner = node.OwnerDocument.CreateNode(XmlNodeType.Element, member.Name, null);
                            node.AppendChild(inner);
                        }
                        serializer.Write(inner, member.Read(item));
                    }
                }
            }

            public override bool SupportsType(Type type) {
                return type.IsClass;
            }
        }

        class EnumSerializer : AbstractSerializer {
            public override bool SupportsType(Type type) {
                return type.IsEnum;
            }

            public override object Read(XmlNode node, Type type) {
                return Enum.Parse(type, node.InnerText);
            }

            public override void Write(XmlNode node, object o) {
                node.InnerText = Enum.GetName(o.GetType(), o);
            }
        }

        class ListSerializer : AbstractSerializer {

            string GetChildName(XmlNode node) {
                string childName = node.Name;
                if (childName.EndsWith("s")) {
                    childName = childName.Substring(0, childName.Length - 1);
                }
                return childName;
            }

            public override object Read(XmlNode node, Type type) {
                IList list = (IList)type.GetConstructor(Type.EmptyTypes).Invoke(null);
                var childName = GetChildName(node);

                Type listType = type.GetGenericArguments()[0];
                var serializer = FindSerializer(listType);

                foreach (XmlNode child in node.SelectNodes(childName)) {
                    list.Add(serializer.Read(child, listType));
                }
                return list;
            }

            public override void Write(XmlNode node, object o) {
                IList list = (IList)o;

                node.InnerXml = "";

                if (list != null) {

                    Type listType = list.GetType().GetGenericArguments()[0];
                    var serializer = FindSerializer(listType);
                    var childName = GetChildName(node);

                    foreach (var item in list) {
                        var inner = node.OwnerDocument.CreateNode(XmlNodeType.Element, childName, null);
                        node.AppendChild(inner);
                        serializer.Write(inner, item);
                    }
                }
            }

            public override bool SupportsType(Type type) {
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
            }
        }


        class StringSerializer : AbstractSerializer {
            public override object Read(XmlNode node, Type type) {
                return node.InnerText;
            }

            public override void Write(XmlNode node, object item) {
                node.InnerText = (string)item;
            }

            public override bool SupportsType(Type type) {
                return type == typeof(String);
            }
        }

        class BoolSerializer : AbstractSerializer {
            public override object Read(XmlNode node, Type type) {
                return Boolean.Parse(node.InnerText);
            }

            public override void Write(XmlNode node, object item) {
                node.InnerText = ((bool)item).ToString();
            }

            public override bool SupportsType(Type type) {
                return type == typeof(bool);
            }
        }

        class Int32Serializer : AbstractSerializer {
            public override object Read(XmlNode node, Type type) {
                return Int32.Parse(node.InnerText);
            }

            public override void Write(XmlNode node, object item) {
                node.InnerText = ((int)item).ToString();
            }

            public override bool SupportsType(Type type) {
                return type == typeof(int);
            }
        }

        class DateTimeSerializer : AbstractSerializer {

            public override object Read(XmlNode node, Type type) {
                return DateTime.Parse(node.InnerText);
            }

            public override void Write(XmlNode node, object item) {
                node.InnerText = ((DateTime)item).ToString();
            }

            public override bool SupportsType(Type type) {
                return type == typeof(DateTime);
            }
        }

        class SingleSerializer : AbstractSerializer {

            public override object Read(XmlNode node, Type type) {
                return Single.Parse(node.InnerText);
            }

            public override void Write(XmlNode node, object item) {
                node.InnerText = ((Single)item).ToString();
            }

            public override bool SupportsType(Type type) {
                return type == typeof(Single);
            }
        }


        class DoubleSerializer : AbstractSerializer {

            public override object Read(XmlNode node, Type type) {
                return Double.Parse(node.InnerText);
            }

            public override void Write(XmlNode node, object item) {
                node.InnerText = ((Double)item).ToString();
            }

            public override bool SupportsType(Type type) {
                return type == typeof(Double);
            }
        }


        #endregion

        Dictionary<string, object> defaults = new Dictionary<string, object>();
        T boundObject;
        string filename;

        private void InitDefaults() {
            foreach (var member in SettingMembers(typeof(T))) {
                defaults[member.Name] = member.Read(boundObject);
            }
        }

        public static XmlSettings<T> Bind(T obj, string filename) {
            return new XmlSettings<T>(obj, filename);
        }

        static List<AbstractSerializer> serializers;

        static XmlSettings() {
            serializers = new List<AbstractSerializer>() { 
                new StringSerializer(),
                new BoolSerializer(), 
                new SingleSerializer(),
                new DoubleSerializer(),
                new Int32Serializer(),
                new DateTimeSerializer(),
                new EnumSerializer(),
                new ListSerializer(),
                new GenericObjectSerializer()
            };
        }

        private XmlSettings(T boundObject, string filename) {
            this.boundObject = boundObject;
            this.filename = filename;
            InitDefaults();
            Read();
        }

        static List<AbstractMember> SettingMembers(Type type) {

            // todo: cache this, not really important 
            List<AbstractMember> members = new List<AbstractMember>();
            foreach (MemberInfo mi in type.GetMembers(
                   BindingFlags.Public | BindingFlags.Instance)) {
                if (IsSetting(mi)) {
                    PropertyInfo pi = mi as PropertyInfo;
                    FieldInfo fi = mi as FieldInfo;

                    if (pi != null) {
                        if (pi.GetGetMethod() != null && pi.GetSetMethod() != null) {
                            members.Add(new PropertyMember(pi));
                        }
                    } else {
                        members.Add(new FieldMember(fi));
                    }
                }
            }
            return members;
        }

        static AbstractSerializer FindSerializer(Type type) {
            foreach (var serializer in serializers) {
                if (serializer.SupportsType(type)) {
                    return serializer;
                }
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Read current config from file
        /// </summary>
        void Read() {
            bool stuff_changed = false;

            XmlDocument dom = new XmlDocument();
            XmlNode settingsNode = null;

            try {
              
                dom.Load(filename);
                settingsNode = GetSettingsNode(dom);
                
                if (settingsNode == null) {
                    throw new Exception("Corrupt file can not recover");
                }
            } catch (Exception) {
                // corrupt or missing config so create
                File.WriteAllText(filename, "<Settings></Settings>");
                dom.Load(filename);
                settingsNode = GetSettingsNode(dom);
            }


            Profiler.TimeAction("Load fields", () =>
            {
                foreach (AbstractMember member in SettingMembers(typeof(T))) {


                    var serializer = FindSerializer(member.Type);

                    XmlNode node = settingsNode.SelectSingleNode(member.Name);

                    if (node == null) {
                        node = dom.CreateNode(XmlNodeType.Element, member.Name, null);
                        settingsNode.AppendChild(node);
                        serializer.Write(node, defaults[member.Name]);
                        stuff_changed = true;
                    }

                    try {
                        var data = serializer.Read(node, member.Type);
                        member.Write(boundObject, data);
                    } catch (Exception e) {
                        Trace.WriteLine(e.ToString());
                        serializer.Write(node, defaults[member.Name]);
                        stuff_changed = true;
                    }
                }


            });

            if (stuff_changed) {
                Write();
            }


        }


        private static XmlNode GetSettingsNode(XmlDocument dom) {
            return dom.SelectSingleNode("/Settings");
        }



        /// <summary>
        /// Write current config to file
        /// </summary>
        public void Write() {

            XmlDocument dom = new XmlDocument();
            dom.Load(filename);
            var settingsNode = GetSettingsNode(dom);

            foreach (var member in SettingMembers(typeof(T))) {

                var serializer = FindSerializer(member.Type);

                object v = member.Read(boundObject);
                if (v == null) {
                    v = defaults[member.Name];
                }

                XmlNode node = settingsNode.SelectSingleNode(member.Name);

                if (node == null) {
                    /*
                    var comment = GetComment(member);
                    if (comment != "") {
                        settingsNode.AppendChild(dom.CreateComment(comment));
                    }
                     */
                    node = dom.CreateNode(XmlNodeType.Element, member.Name, null);
                    settingsNode.AppendChild(node);
                }

                serializer.Write(node, v);
            } // for each
            dom.Save(filename);
        }

        private string GetComment(MemberInfo field) {
            string comment = "";
            var attribs = field.GetCustomAttributes(typeof(CommentAttribute), false);
            if (attribs != null && attribs.Length > 0) {
                comment = ((CommentAttribute)attribs[0]).Comment;
            }
            return comment;
        }

        private static bool IsSetting(MemberInfo mi) {

            var attribs = mi.GetCustomAttributes(typeof(SkipFieldAttribute), true);
            bool ignore = attribs != null && attribs.Length > 0;
            return !ignore && (mi.MemberType == MemberTypes.Field || mi.MemberType == MemberTypes.Property);
        }

    }
}

