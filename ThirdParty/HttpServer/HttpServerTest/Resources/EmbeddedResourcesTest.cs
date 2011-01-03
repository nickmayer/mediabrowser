using HttpServer.Resources;
using Xunit;

namespace HttpServerTest.Resources
{
    public class EmbeddedResourcesTest
    {
        private EmbeddedResources _resources;

        public EmbeddedResourcesTest()
        {
            _resources = new EmbeddedResources();
        }

        [Fact]
        public void Add()
        {
            _resources.Add("/users/", GetType().Assembly, GetType().Namespace + ".Views",
                           GetType().Namespace + ".Views.MyFile.xml.spark");
        }

        [Fact]
        private void AutoFind()
        {
            _resources = new EmbeddedResources("/", GetType().Assembly, GetType().Namespace + ".Views");
        }
    }
}