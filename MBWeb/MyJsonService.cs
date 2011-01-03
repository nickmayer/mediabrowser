using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library;
using MediaBrowser.Library.Entities;
using MediaBrowser.Web.Framework;
using MediaBrowser.Library.Persistance;

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
                    Dictionary<string, object> row = new Dictionary<string, object>();
                    row["Name"] = child.Name;
                    row["Id"] = child.Id;
                    foreach (var persistable in Serializer.GetPersistables(child))
                    {
                        row[persistable.MemberInfo.Name] = persistable.GetValue(child);
                    }
                   
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

    }
}
