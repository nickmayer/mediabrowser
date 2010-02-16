using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaBrowser.Library.Plugins.Attributes {
    [global::System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class RequiredVersionAttribute : Attribute {

        public RequiredVersionAttribute(System.Version version) {
            this.Version = version;
        }

        public RequiredVersionAttribute(string version) {
            this.Version = new System.Version(version);
        }


        // This is a named argument
        public System.Version Version { get; private set; }
    }
}
