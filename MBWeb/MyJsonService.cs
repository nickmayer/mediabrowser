using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MediaBrowser.Web.Framework;
using MediaBrowser.Library.Persistance;
using System.Collections;

namespace MediaBrowser
{

    public class MyJsonService: MediaBrowser.Web.Framework.JsonService
    {
        static Dictionary<Guid, BaseItem> ItemMap = new Dictionary<Guid, BaseItem>();

        [Route("/item")]
        public object Item(Guid? id)
        {
            Folder folder = null;
            BaseItem item = null;

            if (id == null)
            {
                folder = Kernel.Instance.RootFolder;
            }

            if (folder == null)
            {
                lock (ItemMap)
                {
                    item = ItemMap[id.Value];
                }
                folder = item as Folder;
            }
  

            if (folder != null)
            {
                List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
                foreach (var child in folder.Children)
                {
                    var row = ToJsonSerializable(child)  as Dictionary<string, object>;
                    row["Name"] = child.Name;
                    row["Id"] = child.Id;
                    result.Add(row);
                }

                lock (ItemMap)
                {
                    foreach (var baseItem in folder.Children)
                    {
                        ItemMap[baseItem.Id] = baseItem;
                    }
                }


                return result;
            }
           

            return new object[] { item };
        }


        public object ToJsonSerializable(object input)
        {
            Type type = input != null ? input.GetType() : (Type)null;
            if (input == null || type.IsValueType || type.IsEnum || type == typeof(string))
            {
                return input;
            }

            if (typeof(IList).IsAssignableFrom(type))
            {
                var list = new List<object>();
                foreach (var item in (IList)input)
                {
                    list.Add(item);
                }
                return list;
            }

            var rval = new Dictionary<string, object>();
            foreach (var persistable in Serializer.GetPersistables(input))
            {
                rval[persistable.MemberInfo.Name] = ToJsonSerializable(persistable.GetValue(input));
            }
            return rval;
        }
    }
}
