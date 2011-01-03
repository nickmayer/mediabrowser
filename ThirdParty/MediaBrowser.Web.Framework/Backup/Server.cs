using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using HttpServer.BodyDecoders;
using HttpServer.Headers;
using HttpServer.Logging;
using HttpServer.Messages;
using HttpServer.Modules;
using HttpServer.Routing;

namespace HttpServer
{
	/// <summary>
	/// Http server.
	/// </summary>
	public class Server
	{
		[ThreadStatic] private static Server _server;
		private readonly BodyDecoderCollection _bodyDecoders = new BodyDecoderCollection();
		private readonly List<HttpListener> _listeners = new List<HttpListener>();
		private readonly ILogger _logger = LogFactory.CreateLogger(typeof (Server));
		private readonly List<IModule> _modules = new List<IModule>();
		private readonly List<IRouter> _routers = new List<IRouter>();
		private HttpFactory _factory;
		private bool _isStarted;

		/// <summary>
		/// Initializes a new instance of the <see cref="Server"/> class.
		/// </summary>
		/// <param name="factory">Factory used to create objects used in this library.</param>
		public Server(HttpFactory factory)
		{
			_server = this;
			_factory = factory;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Server"/> class.
		/// </summary>
		public Server()
		{
			_server = this;
			_factory = new HttpFactory();
		}

		/// <summary>
		/// Gets current server.
		/// </summary>
		/// <remarks>
		/// Only valid when a request have been received and is being processed.
		/// </remarks>
		public static Server Current
		{
			get { return _server; }
		}

		/// <summary>
		/// Add a decoder.
		/// </summary>
		/// <param name="decoder">decoder to add</param>
		/// <remarks>
		/// Adding zero decoders will make the server add the 
		/// default ones which is <see cref="MultiPartDecoder"/> and <see cref="UrlDecoder"/>.
		/// </remarks>
		public void Add(IBodyDecoder decoder)
		{
			_bodyDecoders.Add(decoder);
		}


		/// <summary>
		/// Add a new router.
		/// </summary>
		/// <param name="router">Router to add</param>
		/// <exception cref="InvalidOperationException">Server have been started.</exception>
		public void Add(IRouter router)
		{
			if (_isStarted)
				throw new InvalidOperationException("Server have been started.");

			_routers.Add(router);
		}

		/// <summary>
		/// Add a file module
		/// </summary>
		/// <param name="module">Module to add</param>
		/// <exception cref="ArgumentNullException"><c>module</c> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Cannot add modules when server have been started.</exception>
		public void Add(IModule module)
		{
			if (module == null)
				throw new ArgumentNullException("module");
			if (_isStarted)
				throw new InvalidOperationException("Cannot add modules when server have been started.");
			_modules.Add(module);
		}

		/// <summary>
		/// Add a HTTP listener.
		/// </summary>
		/// <param name="listener"></param>
		/// <exception cref="InvalidOperationException">Listener have been started.</exception>
		public void Add(HttpListener listener)
		{
			if (listener.IsStarted)
				throw new InvalidOperationException("Listener have been started.");

			listener.ExceptionThrown += OnExceptionThrown;

			_listeners.Add(listener);
		}

		private void DecodeBody(IRequest request)
		{
			Encoding encoding = null;
			if (request.ContentType != null)
			{
				string encodingStr = request.ContentType.Parameters["Encoding"];
				if (!string.IsNullOrEmpty(encodingStr))
					encoding = Encoding.GetEncoding(encodingStr);
			}

			if (encoding == null)
				encoding = Encoding.UTF8;

			// process body.
			DecodedData data = _bodyDecoders.Decode(request.Body, request.ContentType, encoding);
			if (data == null)
				return;


			var request1 = (Request) request;
			request1.Form = data.Parameters;
			request1.Files = data.Files;
		}

		private void OnErrorPage(object sender, ErrorPageEventArgs e)
		{
			_server = this;
			ErrorPageRequested(this, e);
		}


		private void OnExceptionThrown(object sender, ExceptionEventArgs e)
		{
			TriggerExceptionThrown(e);
		}

		private void OnRequest(object sender, RequestEventArgs e)
		{
			_server = this;

			Exception exception;
			try
			{
				if (HandleRequest(e) != ProcessingResult.Continue)
					return;

				exception = null;
			}
			catch (HttpException err)
			{
				_logger.Error("Got an HTTP exception.", err);
				e.Response.Status = err.Code;
				e.Response.Reason = err.Message;
				exception = err;
			}
			catch (Exception err)
			{
				_logger.Error("Got an exception.", err);
				var args = new ExceptionEventArgs(err);
				TriggerExceptionThrown(args);
				exception = err;
				e.Response.Status = HttpStatusCode.InternalServerError;
				e.Response.Reason = "Failed to process request.";
			}


			if (exception == null)
			{
				e.Response.Status = HttpStatusCode.NotFound;
				e.Response.Reason = "Requested resource is not found. Sorry ;(";
			}
			SendErrorPage(e, exception);
		}

		private void TriggerExceptionThrown(ExceptionEventArgs e)
		{
			ExceptionThrown(this, e);
		}

		private ProcessingResult HandleRequest(RequestEventArgs e)
		{
			var context = new RequestContext
			              	{
			              		HttpContext = e.Context,
			              		Request = e.Request,
			              		Response = e.Response
			              	};

            PrepareRequest(this, e);

			// standard headers.
			e.Response.Add(new DateHeader("Date", DateTime.UtcNow));
			e.Response.Add(new StringHeader("Server", "C# WebServer"));


			if (e.Request.ContentLength.Value > 0)
				DecodeBody(e.Request);

			// Process routers.
			ProcessingResult result = ProcessRouters(context);
			if (ProcessResult(result, e))
				_logger.Debug("Routers processed the request.");

			// process modules.
			result = ProcessModules(context);
			if (ProcessResult(result, e))
				return result;

            RequestReceived(this, e);


			return ProcessingResult.Continue;
		}

		/// <summary>
		/// Go through all modules and check if any of them can handle the current request.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private ProcessingResult ProcessModules(RequestContext context)
		{
			foreach (IModule module in _modules)
			{
				ProcessingResult result = module.Process(context);
				if (result != ProcessingResult.Continue)
				{
					_logger.Debug(module.GetType().Name + ": " + result);
					return result;
				}
			}

			return ProcessingResult.Continue;
		}

		/// <summary>
		/// Process result (check if it should be sent back or not)
		/// </summary>
		/// <param name="result"></param>
		/// <param name="e"></param>
		/// <returns><c>true</c> if request was processed properly.; otherwise <c>false</c>.</returns>
		private bool ProcessResult(ProcessingResult result, RequestEventArgs e)
		{
			if (result == ProcessingResult.Abort)
			{
				e.IsHandled = true;
				return true;
			}

			if (result == ProcessingResult.SendResponse)
			{
				SendResponse(e.Context, e.Request, e.Response);
				e.IsHandled = true;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Client asks if he may continue.
		/// </summary>
		/// <remarks>
		/// If the body is too large or anything like that you should respond <see cref="HttpStatusCode.ExpectationFailed"/>.
		/// </remarks>
		public event EventHandler<RequestEventArgs> ContinueResponseRequested = delegate { };

		/// <summary>
		/// Processes all routers.
		/// </summary>
		/// <param name="context">Request context.</param>
		/// <returns>Processing result.</returns>
		private ProcessingResult ProcessRouters(RequestContext context)
		{
			foreach (IRouter router in _routers)
			{
				if (router.Process(context) == ProcessingResult.SendResponse)
				{
					_logger.Debug(router.GetType().Name + " sends the response.");
					return ProcessingResult.SendResponse;
				}
			}

			return ProcessingResult.Continue;
		}

		private void SendErrorPage(RequestEventArgs e, Exception err)
		{
			var args = new ErrorPageEventArgs(e.Context, e.Request, e.Response) {Exception = err};
			ErrorPageRequested(this, args);
			e.IsHandled = true;

			// use a ugly default error page.
			if (!args.IsHandled)
			{
#if DEBUG
				byte[] body = Encoding.UTF8.GetBytes(err != null ? err.ToString() : e.Response.Reason);
#else
                byte[] body = Encoding.UTF8.GetBytes(e.Response.Reason);
#endif
				e.Response.Body.Write(body, 0, body.Length);
			}

			SendResponse(e.Context, args.Request, args.Response);
		}

		/// <summary>
		/// Send a response.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="response"></param>
		protected void SendResponse(IHttpContext context, IRequest request, IResponse response)
		{
			SendingResponse(this, new RequestEventArgs(context, request, response));

			var generator = HttpFactory.Current.Get<ResponseWriter>();
			generator.Send(context, response);
			if (request.Connection != null && request.Connection.Type == ConnectionType.Close)
			{
				context.Stream.Close();
				_logger.Debug("Closing connection.");
			}
		}

		/// <summary>
		/// Start http server.
		/// </summary>
		/// <param name="backLog">Number of pending connections.</param>
		public void Start(int backLog)
		{
			if (_isStarted)
				return;

			if (_bodyDecoders.Count == 0)
			{
				_bodyDecoders.Add(new MultiPartDecoder());
				_bodyDecoders.Add(new UrlDecoder());
			}

			foreach (HttpListener listener in _listeners)
			{
#if !DEBUG
				listener.ExceptionThrown += OnExceptionThrown;
#endif
				listener.ErrorPageRequested += OnErrorPage;
				listener.RequestReceived += OnRequest;
				listener.ContinueResponseRequested += On100Continue;
				listener.Start(backLog);
			}

			_isStarted = true;
		}

		private void On100Continue(object sender, RequestEventArgs e)
		{
			ContinueResponseRequested(this, e);
		}

		/// <summary>
		/// Invoked just before a response is sent back to the client.
		/// </summary>
		public event EventHandler<RequestEventArgs> SendingResponse = delegate { };

		/// <summary>
		/// Invoked *after* the web server has tried to handled the request.
		/// </summary>
		/// <remarks>
		/// The event can be used to handle the request after all routes and modules
		/// have tried to process the request.
		/// </remarks>
		public event EventHandler<RequestEventArgs> RequestReceived = delegate { };

        /// <summary>
        /// Invoked *before* the web server has tried to handled the request.
        /// </summary>
        /// <remarks>
        /// Event can be used to load a session from a cookie or to force
        /// authentication or anything other you might need t do before a request
        /// is handled.
        /// </remarks>
        public event EventHandler<RequestEventArgs> PrepareRequest = delegate { };


		/// <summary>
		/// An exception that cannot be handled by the library.
		/// </summary>
		public event EventHandler<ExceptionEventArgs> ExceptionThrown = delegate { };

		/// <summary>
		/// An error page have been requested.
		/// </summary>
		public event EventHandler<ErrorPageEventArgs> ErrorPageRequested = delegate { };
	}
}