using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using HttpServer.Headers;
using HttpServer.Logging;
using HttpServer.Messages;

namespace HttpServer
{
	/// <summary>
	/// Http listener.
	/// </summary>
	public class HttpListener
	{
		private readonly HttpFactory _factory;
		private readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
		private TcpListener _listener;
		private ILogger _logger = LogFactory.CreateLogger(typeof (HttpListener));
		private int _pendingAccepts;
		private bool _shuttingDown;

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpListener"/> class.
		/// </summary>
		/// <param name="address">The address.</param>
		/// <param name="port">The port.</param>
		protected HttpListener(IPAddress address, int port)
		{
			Address = address;
			Port = port;
			_factory = new HttpFactory();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HttpListener"/> class.
		/// </summary>
		/// <param name="address">The address.</param>
		/// <param name="port">The port.</param>
		/// <param name="httpFactory">The HTTP factory.</param>
		protected HttpListener(IPAddress address, int port, HttpFactory httpFactory)
		{
			Address = address;
			Port = port;
			_factory = httpFactory;
		}


		/// <summary>
		/// Gets listener address.
		/// </summary>
		public IPAddress Address { get; private set; }

		/// <summary>
		/// Gets HTTP factory used to create types used by this HTTP library.
		/// </summary>
		protected IHttpFactory Factory
		{
			get { return _factory; }
		}

		/// <summary>
		/// Gets if listener is secure.
		/// </summary>
		public virtual bool IsSecure
		{
			get { return true; }
		}

		/// <summary>
		/// Gets if listener have been started.
		/// </summary>
		public bool IsStarted { get; private set; }

		/// <summary>
		/// Gets or sets logger.
		/// </summary>
		public ILogger Logger
		{
			get { return _logger; }
			set
			{
				_logger = value ?? NullLogWriter.Instance;
				_logger.Debug("Logger attached to " + (IsSecure ? "secure" : string.Empty) + " listener [" + Address +
				              ":" + Port +
				              "].");
			}
		}

		/// <summary>
		/// Gets listening port.
		/// </summary>
		public int Port { get; private set; }

		private void BeginAccept()
		{
			if (_shuttingDown)
				return;


			Interlocked.Increment(ref _pendingAccepts);
			try
			{
				_listener.BeginAcceptSocket(OnSocketAccepted, null);
			}
			catch (Exception err)
			{
#if DEBUG
				if (ExceptionThrown.GetInvocationList().Length == 1)
					throw;
#endif
				ExceptionThrown(this, new ExceptionEventArgs(err));
				_logger.Error("Unhandled exception in BeginAccept.\r\n" + err);
			}
		}

		private bool CanAcceptSocket(Socket socket)
		{
			try
			{
				var args = new SocketFilterEventArgs(socket);
				SocketAccepted(this, args);
				return args.IsSocketOk;
			}
			catch (Exception err)
			{
				_logger.Error("SocketAccepted trigger exception: " + err);
				return true;
			}
		}

		/// <summary>
		/// Creates a new <see cref="HttpListener"/> instance with default factories.
		/// </summary>
		/// <param name="address">Address that the listener should accept connections on.</param>
		/// <param name="port">Port that listener should accept connections on.</param>
		/// <returns>Created HTTP listener.</returns>
		public static HttpListener Create(IPAddress address, int port)
		{
			return new HttpListener(address, port);
		}

		/// <summary>
		/// Creates a new <see cref="HttpListener"/> instance with default factories.
		/// </summary>
		/// <param name="address">Address that the listener should accept connections on.</param>
		/// <param name="port">Port that listener should accept connections on.</param>
		/// <param name="factory">Factory used to create different types in the framework.</param>
		/// <returns>Created HTTP listener.</returns>
		public static HttpListener Create(IPAddress address, int port, HttpFactory factory)
		{
			return new HttpListener(address, port);
		}

		/// <summary>
		/// Creates a new <see cref="HttpListener"/> instance with default factories.
		/// </summary>
		/// <param name="address">Address that the listener should accept connections on.</param>
		/// <param name="port">Port that listener should accept connections on.</param>
		/// <param name="certificate">Certificate to use</param>
		/// <returns>Created HTTP listener.</returns>
		public static HttpListener Create(IPAddress address, int port, X509Certificate certificate)
		{
			//RequestParserFactory requestFactory = new RequestParserFactory();
			//HttpContextFactory factory = new HttpContextFactory(NullLogWriter.Instance, 16384, requestFactory);
			return new SecureHttpListener(address, port, certificate);
		}

		/// <summary>
		/// Create a new context 
		/// </summary>
		/// <param name="socket">Accepted socket</param>
		/// <returns>A new context.</returns>
		protected virtual HttpContext CreateContext(Socket socket)
		{
			return Factory.Get<HttpContext>(socket);
		}

		private void ErrorPage(IRequest request, IResponse response, HttpException exception)
		{
			response.Status = exception.Code;
			response.Reason = exception.Message;
			response.Body.SetLength(0);

			var args = new ErrorPageEventArgs(HttpContext.Current, request, response);
			args.Exception = exception;
			ErrorPageRequested(this, args);

			// Add default error body.
			if (!args.IsHandled)
			{
				string htmlTemplate = "<html><head><title>{1}</title></head><body>Error {0}: {1}</body></html>";
				byte[] body = Encoding.UTF8.GetBytes(string.Format(htmlTemplate, (int) response.Status, response.Reason));
				response.Body.Write(body, 0, body.Length);
			}

			try
			{
				var generator = new ResponseWriter();
				generator.Send(HttpContext.Current, response);
			}
			catch (Exception err)
			{
#if DEBUG
				if (ExceptionThrown.GetInvocationList().Length == 1)
					throw;
#endif
				ExceptionThrown(this, new ExceptionEventArgs(err));
			}
		}

		private void OnDisconnect(object sender, EventArgs e)
		{
			HttpFactory.Current = Factory;
			var context = (HttpContext) sender;
			context.Disconnected -= OnDisconnect;
			context.RequestReceived -= OnRequest;
			context.ContinueResponseRequested -= On100Continue;
		}

		/// <exception cref="Exception">Throwing exception if in debug mode and not exception handler have been specified.</exception>
		private void OnRequest(object sender, RequestEventArgs e)
		{
			var context = (HttpContext) sender;
			HttpFactory.Current = Factory;
			HttpContext.Current = context;

			try
			{
				var args = new RequestEventArgs(context, e.Request, e.Response);
				RequestReceived(this, args);
				if (!args.IsHandled)
				{
					// need to respond to the context.
					var generator = new ResponseWriter();
					generator.Send(context, args.Response);
				}

				// Disconnect when done.
				if (e.Response.HttpVersion == "HTTP/1.0" || e.Response.Connection.Type == ConnectionType.Close)
					context.Disconnect();
			}
			catch (Exception err)
			{
				if (err is HttpException)
				{
					var exception = (HttpException) err;
					ErrorPage(e.Request, e.Response, exception);
				}
				else
				{
					_logger.Debug("Request failed.", err);
#if DEBUG
					if (ExceptionThrown.GetInvocationList().Length == 1)
						throw;
#endif
					ExceptionThrown(this, new ExceptionEventArgs(err));
					SendInternalServerError(context, err);
				}
			}
		}

		private void OnSocketAccepted(IAsyncResult ar)
		{
			HttpFactory.Current = Factory;
			Socket socket = null;
			try
			{
				socket = _listener.EndAcceptSocket(ar);
				Interlocked.Decrement(ref _pendingAccepts);
				if (_shuttingDown && _pendingAccepts == 0)
					_shutdownEvent.Set();

				if (!CanAcceptSocket(socket))
				{
					_logger.Debug("Socket was rejected: " + socket.RemoteEndPoint);
					socket.Disconnect(true);
					BeginAccept();
					return;
				}
			}
			catch (Exception err)
			{
				_logger.Warning("Failed to end accept: " + err.Message);
				BeginAccept();
				if (socket != null)
					socket.Disconnect(true);
				return;
			}

			if (!_shuttingDown)
				BeginAccept();

			_logger.Trace("Accepted connection from: " + socket.RemoteEndPoint);

			// Got a new context.
			try
			{
				HttpContext context = CreateContext(socket);
				HttpContext.Current = context;
				context.HttpFactory = _factory;
				context.RequestReceived += OnRequest;
				context.Disconnected += OnDisconnect;
				context.ContinueResponseRequested += On100Continue;
				context.Start();
			}
			catch (Exception err)
			{
				_logger.Error("ContextReceived raised an exception: " + err.Message);
#if DEBUG
				if (ExceptionThrown.GetInvocationList().Length == 1)
					throw;
#endif
				ExceptionThrown(this, new ExceptionEventArgs(err));
				socket.Disconnect(true);
			}
		}

		private void On100Continue(object sender, ContinueEventArgs e)
		{
			var response = new Response(e.Request.HttpVersion, HttpStatusCode.Continue, "Please continue mate.");
			var args = new RequestEventArgs((IHttpContext) sender, e.Request, response);
			ContinueResponseRequested(sender, args);

			string responseString = string.Format("{0} {1} {2}\r\n\r\n", "HTTP/1.0", (int) response.Status,
			                                      response.Reason);
			byte[] buffer = Encoding.ASCII.GetBytes(responseString);
			HttpContext.Current.Stream.Write(buffer, 0, buffer.Length);
			HttpContext.Current.Stream.Flush();
			_logger.Info(responseString);
		}

		private void SendInternalServerError(HttpContext context, Exception exception)
		{
			var generator = new ResponseWriter();
			var response = HttpFactory.Current.Get<IResponse>();
			byte[] body = Encoding.ASCII.GetBytes("Internal server error " + exception);
			response.Body.Write(body, 0, body.Length);
			response.Status = HttpStatusCode.InternalServerError;
			response.Reason = exception.Message;
			generator.Send(context, response);
		}

		/// <summary>
		/// Start listener.
		/// </summary>
		/// <param name="backLog">Number of pending accepts.</param>
		/// <remarks>
		/// Make sure that you are subscribing on <see cref="RequestReceived"/> first.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Listener have already been started.</exception>
		/// <exception cref="SocketException">Failed to start socket.</exception>
		/// <exception cref="ArgumentOutOfRangeException">Invalid port number.</exception>
		public void Start(int backLog)
		{
			if (_listener != null)
				throw new InvalidOperationException("Listener have already been started.");

			IsStarted = true;
			_listener = new TcpListener(Address, Port);
			_listener.Start(backLog);

			if (Port == 0 && _listener.LocalEndpoint is IPEndPoint)
				Port = ((IPEndPoint) _listener.LocalEndpoint).Port;

			// do not use beginaccept. Let exceptions be thrown.
			Interlocked.Increment(ref _pendingAccepts);
			_listener.BeginAcceptSocket(OnSocketAccepted, null);
		}

		/// <summary>
		/// Stop listener.
		/// </summary>
		public void Stop()
		{
			_shuttingDown = true;
			_listener.Stop();
		}

		/// <summary>
		/// A unhandled exception have been caught.
		/// </summary>
		public event EventHandler<ExceptionEventArgs> ExceptionThrown = delegate { };


		/// <summary>
		/// A new request have been received.
		/// </summary>
		public event EventHandler<RequestEventArgs> RequestReceived = delegate { };

		/// <summary>
		/// Can be used to reject certain clients.
		/// </summary>
		public event EventHandler<SocketFilterEventArgs> SocketAccepted = delegate { };


		/// <summary>
		/// A HTTP exception have been thrown.
		/// </summary>
		/// <remarks>
		/// Fill the body with a user friendly error page, or redirect to somewhere else.
		/// </remarks>
		public event EventHandler<ErrorPageEventArgs> ErrorPageRequested = delegate { };


		/// <summary>
		/// Client asks if he may continue.
		/// </summary>
		/// <remarks>
		/// If the body is too large or anything like that you should respond <see cref="HttpStatusCode.ExpectationFailed"/>.
		/// </remarks>
		public event EventHandler<RequestEventArgs> ContinueResponseRequested = delegate { };
	}
}