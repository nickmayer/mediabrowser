using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using HttpServer;

namespace MediaBrowser.Web.Framework
{
    public class ActionRunner : IServableContent
    {
        public Type Type { get; set; }
        public MethodInfo Action { get; set; }
        public string[] Parameters { get; set; }

        public string Invoke(JsonService service, IRequest request)
        {
            List<object> parameters = new List<object>();
            foreach (var parameter in Action.GetParameters())
            {
                string val = request.Parameters[parameter.Name];
                if (parameter.ParameterType == typeof(string))
                {
                    parameters.Add(val);
                }
                else if (parameter.ParameterType == typeof(Nullable<Guid>))
                {
                    Guid? guid = null;
                    try { guid = new Guid(val); }
                    catch { }
                    parameters.Add(guid);
                }
            }

            var obj = Action.Invoke(service, parameters.ToArray());
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }
    }
}
