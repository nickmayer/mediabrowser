using System;

namespace HttpServer.Mvc
{
    /// <summary>
    /// To use another name than the class name
    /// </summary>
    public class ControllerNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerNameAttribute"/> class.
        /// </summary>
        /// <param name="name">Controller name.</param>
        public ControllerNameAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControllerNameAttribute"/> class.
        /// </summary>
        /// <param name="name">Controller name in router.</param>
        /// <param name="rootName">Root name in router.</param>
        /// <remarks>
        /// Root name can be used to provide second level controllers.
        /// Setting name to "booking" and rootName to "hotel" would produce the
        /// following action uri: "/hotel/booking/[actionName]". Just make
        /// sure that "booking" is not a method that is being used in the
        /// "hotel" controller.
        /// </remarks>
        public ControllerNameAttribute(string name, string rootName)
        {
            Name = name;
            RootName = rootName;
        }

        /// <summary>
        /// Gets controller name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the name of the root.
        /// </summary>
        /// <value>Root controller name.</value>
        /// <remarks>
        /// Root name can be used to provide second level controllers.
        /// Setting name to "booking" and rootName to "hotel" would produce the
        /// following action uri: "/hotel/booking/[actionName]". Just make
        /// sure that "booking" is not a method that is being used in the
        /// "hotel" controller.
        /// </remarks>
        public string RootName { get; private set; }
    }
}