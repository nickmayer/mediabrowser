using System;
using System.Collections.Generic;
using System.IO;
using HttpServer.Logging;
using HttpServer.Modules;
using HttpServer.Mvc.ActionResults;
using HttpServer.Mvc.Controllers;
using HttpServer.Mvc.Views;
using HttpServer.Resources;

namespace HttpServer.Mvc
{
    /// <summary>
    /// MVC web server.
    /// </summary>
    public class MvcServer : Server, IModule
    {
        [ThreadStatic] private static MvcServer _current;
        private readonly ActionResultProvider _actionProvider = new ActionResultProvider();
        private readonly BuiltinActions _actions = new BuiltinActions();
        private readonly List<ControllerDirector> _directors = new List<ControllerDirector>();
        private readonly ILogger _logger = LogFactory.CreateLogger(typeof (MvcServer));
        private readonly ViewEngineCollection _viewEngines;
        private readonly ResourceProvider _viewProvider = new ResourceProvider();

        /// <summary>
        /// Initializes a new instance of the <see cref="MvcServer"/> class.
        /// </summary>
        public MvcServer()
        {
            _current = this;
            Add(this);
            _viewEngines = new ViewEngineCollection();

            // register built in actions
            _actions.Register(_actionProvider);
        }

        /// <summary>
        /// Gets all action handlers.
        /// </summary>
        public ActionResultProvider ActionProvider
        {
            get { return _actionProvider; }
        }

        /// <summary>
        /// Gets current web server
        /// </summary>
        public static MvcServer CurrentMvc
        {
            get { return _current; }
        }

        /// <summary>
        /// Gets view engine collection.
        /// </summary>
        public ViewEngineCollection ViewEngines
        {
            get { return _viewEngines; }
        }

        /// <summary>
        /// Gets view provider.
        /// </summary>
        public ResourceProvider ViewProvider
        {
            get { return _viewProvider; }
        }

        /// <summary>
        /// Add a controller.
        /// </summary>
        /// <param name="controller"></param>
        public void Add(Controller controller)
        {
            var director = new ControllerDirector(controller);
            director.InvokingAction += OnInvokingAction;
            _directors.Add(director);
        }

        private void OnInvokingAction(object sender, ControllerEventArgs e)
        {
            InvokingAction(this, e);
        }

        private void RenderView(ControllerContext controllerContext, IViewData viewData)
        {
            //ViewProvider.Get(controllerContext.ViewPath + ".*");


            // do not dispose writer, since it will close the stream.
            TextWriter bodyWriter = new StreamWriter(controllerContext.RequestContext.Response.Body);


            _viewEngines.Render(bodyWriter, controllerContext, viewData);
            bodyWriter.Flush();
        }

        #region IModule Members

        /// <summary>
        /// Process a request.
        /// </summary>
        /// <param name="context">Request information</param>
        /// <returns>What to do next.</returns>
        public ProcessingResult Process(RequestContext context)
        {
            _current = this;
            var controllerContext = new ControllerContext(context);

            foreach (ControllerDirector director in _directors)
            {
                if (!director.CanProcess(controllerContext))
                    continue;

                _logger.Debug("Controller '" + director.Name + "' is handling the request.");
                Controller controller = null;
                try
                {
                    object result = director.Process(controllerContext, out controller);
                    if (result == null)
                        continue;

                    var viewData = result as IViewData;
                    if (viewData != null)
                    {
                        _logger.Trace("Rendering action " + controllerContext.ActionName);
                        RenderView(controllerContext, viewData);
                    }
                    else
                    {
                        var action = result as IActionResult;
                        if (action != null)
                        {
                            ProcessingResult processingResult = _actionProvider.Invoke(context, action);
                            if (processingResult == ProcessingResult.Abort)
                                return processingResult;
                        }
                    }

                    return ProcessingResult.SendResponse;
                }
                finally
                {
                    // We want to keep the controller during the whole
                    // rendering process. To be able to access the controller from
                    // the view.
                    if (controller != null)
                        director.Enqueue(controller);
                }
            }

            return ProcessingResult.Continue;
        }

        #endregion

        /// <summary>
        /// Raised before a controller action is invoked.
        /// </summary>
        /// <remarks>Use it to invoke any controller initializations you might need to do.</remarks>
        public event EventHandler<ControllerEventArgs> InvokingAction = delegate { };
    }
}