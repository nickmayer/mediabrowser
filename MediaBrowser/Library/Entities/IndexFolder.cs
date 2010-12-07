using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Entities
{
    class IndexFolder : Folder
    {
        public override void ValidateChildren()
        {
            return; //never validate as they don't actually exist in the file system in this way
        }

        public void AddChild(BaseItem child)
        {
            this.ActualChildren.Add(child);
        }
    }
}
