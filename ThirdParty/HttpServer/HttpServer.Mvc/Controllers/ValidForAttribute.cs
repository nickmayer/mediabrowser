using System;

namespace HttpServer.Mvc.Controllers
{
    /// <summary>
    /// Action is valid for specific HTTP methods.
    /// </summary>
    internal class ValidForAttribute : Attribute
    {
        public ValidForAttribute(params Method[] methods)
        {
            Methods = methods;
        }

        /// <summary>
        /// Methods that this action is valid for.
        /// </summary>
        public Method[] Methods { get; private set; }
    }
}