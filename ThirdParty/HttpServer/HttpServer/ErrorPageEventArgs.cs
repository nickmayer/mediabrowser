using System;

namespace HttpServer
{
    /// <summary>
    /// Arguments for <see cref="Server.ErrorPageRequested"/>.
    /// </summary>
    public class ErrorPageEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorPageEventArgs"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        public ErrorPageEventArgs(IHttpContext context, IRequest request, IResponse response)
        {
            Context = context;
            Request = request;
            Response = response;
        }

        internal IHttpContext Context { get; private set; }

        /// <summary>
        /// Gets or sets thrown exception
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// Gets or sets if error page was provided.
        /// </summary>
        public bool IsHandled { get; set; }

        /// <summary>
        /// Gets requested resource.
        /// </summary>
        public IRequest Request { get; private set; }

        /// <summary>
        /// Gets response to send
        /// </summary>
        public IResponse Response { get; private set; }
    }
}