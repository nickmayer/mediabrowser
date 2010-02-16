using System;
using System.Text;

namespace MediaBrowser.Library.Plugins.Attributes {

    [global::System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class TestedVersionAttribute : Attribute {

        public TestedVersionAttribute(System.Version version) {
            this.Version = version;
        }

        public TestedVersionAttribute(string version) {
            this.Version = new System.Version(version);
        }


        // This is a named argument
        public System.Version Version { get; private set; }
    }

}
