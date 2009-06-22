using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaBrowser.Library.Persistance;
using MediaBrowser.Library.Entities.Attributes;

namespace MediaBrowser.Library.Entities {
    public class Person : BaseItem {
        public Person() {
        }

        [Persist]
        [NotSourcedFromProvider]
        string name;

        public override string Name {
            get {
                return name;
            }
            set {
                name = value;
            }
        }

        public Person(Guid id, string name) {
            this.name = name;
            this.Id = id;
        }
    }
}
