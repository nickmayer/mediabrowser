using System;
using System.Collections.Generic;
using System.Text;
using HttpServer.Headers;

namespace HttpServer.Authentication
{
    /// <summary>
    /// Implements basic authentication scheme.
    /// </summary>
    public class BasicAuthentication : IAuthenticator
    {
        private readonly AuthenticateHandler _handler;

        public BasicAuthentication(AuthenticateHandler handler)
        {
            _handler = handler;
        }

        /// <summary>
        /// Create a response that can be sent in the WWW-Authenticate header.
        /// </summary>
        /// <param name="realm">Realm that the user should authenticate in</param>
        /// <param name="options">Not used by basic authentication</param>
        /// <returns>A WWW-Authenticate header.</returns>
        /// <exception cref="ArgumentNullException">Argument is <c>null</c>.</exception>
        public IHeader CreateChallenge(string realm, object[] options)
        {
            if (string.IsNullOrEmpty(realm))
                throw new ArgumentNullException("realm");

            return new StringHeader("WWW-Authenticate", "Basic realm=\"" + realm + "\"");
        }

        /// <summary>
        /// An authentication response have been received from the web browser.
        /// Check if it's correct
        /// </summary>
        /// <param name="header">Authorization header</param>
        /// <param name="realm">Realm that should be authenticated</param>
        /// <param name="httpVerb">GET/POST/PUT/DELETE etc.</param>
        /// <param name="options">Not used in basic auth</param>
        /// <returns>Authentication object that is stored for the request. A user class or something like that.</returns>
        /// <exception cref="ArgumentException">if authenticationHeader is invalid</exception>
        /// <exception cref="ArgumentNullException">If any of the paramters is empty or null.</exception>
        public bool Authenticate(AuthorizationHeader header, string realm, string httpVerb, object[] options)
        {
            if (header == null)
                throw new ArgumentNullException("realm");
            if (string.IsNullOrEmpty(realm))
                throw new ArgumentNullException("realm");
            if (string.IsNullOrEmpty(httpVerb))
                throw new ArgumentNullException("httpVerb");

            /*
             * To receive authorization, the client sends the userid and password,
                separated by a single colon (":") character, within a base64 [7]
                encoded string in the credentials.*/
            string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Data));
            int pos = decoded.IndexOf(':');
            if (pos == -1)
                return false;

            string password = decoded.Substring(pos + 1, decoded.Length - pos - 1);
            string userName = decoded.Substring(0, pos);
            var context = new BasicAuthenticationContext(realm, userName, password);
            return _handler(context);
        }

        /// <summary>
        /// Gets authenticator scheme
        /// </summary>
        /// <value></value>
        /// <example>
        /// digest
        /// </example>
        public string Scheme
        {
            get { return "basic"; }
        }

    }
}
