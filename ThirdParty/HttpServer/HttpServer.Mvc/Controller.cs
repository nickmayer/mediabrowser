using System;
using System.Reflection;
using HttpServer.Mvc.ActionResults;
using HttpServer.Mvc.Views;

namespace HttpServer.Mvc.Controllers
{
    /// <summary>
    /// MVC controller.
    /// </summary>
    public abstract class Controller : ICloneable
    {
        private IControllerContext _context;
        private ViewData _viewData;

        /// <summary>
        /// Initializes a new instance of the <see cref="Controller"/> class.
        /// </summary>
        protected Controller()
        {
            _viewData = new ViewData();
        }

        /// <summary>
        /// Gets name of requested action.
        /// </summary>
        public string ActionName { get { return _context.ActionName; } }

        /// <summary>
        /// Gets controller name
        /// </summary>
        public string ControllerName { get; internal set; }

        /// <summary>
        /// Gets or sets layout name.
        /// </summary>
        public string LayoutName { get; set; }

        /// <summary>
        /// Gets or sets document title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets HTTP request
        /// </summary>
        public IRequest Request { get { return _context.RequestContext.Request; } }

        /// <summary>
        /// Gets HTTP response.
        /// </summary>
        public IResponse Response { get { return _context.RequestContext.Response; } }

        /// <summary>
        /// Gets request parameters
        /// </summary>
        /// <remarks>A merged collection of Uri and Form parameters</remarks>
        public IParameterCollection Parameters
        {
            get { return _context.RequestContext.Request.Parameters; }
        }

        /// <summary>
        /// Gets form parameters
        /// </summary>
        /// <remarks>Form parameters</remarks>
        public IParameterCollection Form
        {
            get { return _context.RequestContext.Request.Form; }
        }

        /// <summary>
        /// View data used when rendering a view.
        /// </summary>
        protected IViewData ViewData
        {
            get { return _viewData; }
        }

        /// <summary>
        /// Clear everything from the last invocation.
        /// </summary>
        public virtual void Clear()
        {
            LayoutName = null;
            Title = string.Empty;
            _viewData.Clear();
        }

        /// <summary>
        /// Invoke a method in another controller.
        /// </summary>
        /// <param name="controllerName">Name of controller.</param>
        /// <param name="action">Action to invoke</param>
        /// <param name="arguments">Parameters used by the controller.</param>
        /// <returns></returns>
        public static object Invoke(string controllerName, string action, params object[] arguments)
        {
            return null;
            //return MvcServer.Current.Invoke(controllerName, action, arguments);
        }

        /// <summary>
        /// Redirect to a action or to an Uri.
        /// </summary>
        /// <param name="actionOrUri">Action name</param>
        /// <returns>Result to return from the current action</returns>
        /// <example>
        /// <code>
        /// public IActionResult View()
        /// {
        ///   // do something
        ///   return Redirect("index");
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// Using http:// would redirect to external uris, using no slashes at all will redirect
        /// to an action.
        /// </remarks>
        /// 
        protected IActionResult Redirect(string actionOrUri)
        {
            return actionOrUri.Contains("/") ? new Redirect(actionOrUri) : new Redirect(ControllerName, actionOrUri);
        }

        /// <summary>
        /// Render current action.
        /// </summary>
        /// <returns></returns>
        protected IViewData Render()
        {
            _context.ViewPath = "/" + ControllerName + "/" + ActionName;
            return ViewData;
        }

        /// <summary>
        /// Gets or sets id
        /// </summary>
        public string Id
        {
            get
            {
                if (_context.UriSegments.Length > 2)
                    return _context.UriSegments[2];
                return string.Empty;
            }
            
        }

        /// <summary>
        /// Render a specific view.
        /// </summary>
        /// <param name="viewName">Name of view</param>
        /// <returns><see cref="IViewData"/> that should be returned from action.</returns>
        protected IViewData Render(string viewName)
        {
            if (viewName.StartsWith("/"))
                _context.ViewPath = viewName;
            else
                _context.ViewPath = "/" + ControllerName + "/" + viewName;
            return ViewData;
        }

        /// <summary>
        /// Sets controller context 
        /// </summary>
        /// <remarks>
        /// Context contains information about the current request.
        /// </remarks>
        internal void SetContext(IControllerContext context)
        {
            _context = context;
        }


        /// <summary>
        /// Invoked just before actual action is invoked.
        /// </summary>
        /// <remarks>
        /// Called before the action is invoked.
        /// </remarks>
        /// <returns>
        /// <c>null</c> if action can be invoked; otherwise any action result.
        /// </returns>
        internal IActionResult InvokeBeforeAction(MethodInfo method)
        {
            return BeforeAction(method);
        }

        internal void InvokeAfterAction(IActionResult actionResult)
        {
            AfterAction(actionResult);
        }

        /// <summary>
        /// Invoked just before actual action is invoked.
        /// </summary>
        /// <param name="method">Action method to be invoked.</param>
        /// <remarks>
        /// Called before the action is invoked.
        /// </remarks>
        /// <returns>
        /// <c>null</c> if action can be invoked; otherwise any action result.
        /// </returns>
        protected virtual IActionResult BeforeAction(MethodInfo method)
        {
            return null;
        }

        /// <summary>
        /// Action have been invoked
        /// </summary>
        /// <param name="result">Result returned by action.</param>
        protected virtual void AfterAction(IActionResult result)
        {
            
        }

        /// <summary>
        /// Used to be able to trigger the OnException event from controller director.
        /// </summary>
        /// <param name="err"></param>
        /// <returns></returns>
        internal IActionResult TriggerOnException(Exception err)
        {
            return OnException(err);
        }

        /// <summary>
        /// Invoked when an exception was thrown but not handled.
        /// </summary>
        /// <param name="err">thrown exception</param>
        /// <returns>Action result used to provide feedback about the error to the user. Return <c>null</c> for default exception handling.</returns>
        /// <remarks>Uncaught exceptions can be handled in multiple places, first in the controller, next by the HttpServer and finally by the HttpListener (from which the request origininated).
        /// You can handle exceptions yourself by hooking events in the MvsServer, Server or HttpListener. You can also provide custom
        /// error pages by hooking the ErrorPageRequested event in Server. 
        /// </remarks>
        protected virtual IActionResult OnException(Exception err)
        {
            return null;
        }

        #region ICloneable Members

        /// <summary>
        /// Creates a new controller that is a clone of the current one.
        /// </summary>
        /// <returns>
        /// A new controller.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public abstract object Clone();

        #endregion
    }
}