using System;
using HttpServer.Headers;

namespace HttpServer.Authentication
{
    /// <summary>
    /// Authenticates requests
    /// </summary>
    internal interface IAuthenticator
    {
        /// <summary>
        /// Authenticate request
        /// </summary>
        /// <param name="header">Authorization header send by web client</param>
        /// <param name="realm">Realm to authenticate in, typically a domain name.</param>
        /// <param name="httpVerb">HTTP Verb used in the request.</param>
        /// <param name="options">Scheme specific options.</param>
        /// <returns><c>true</c> if authentication was successful; otherwise <c>false</c>.</returns>
        bool Authenticate(AuthorizationHeader header, string realm, string httpVerb, object[] options);

        /// <summary>
        /// Gets authenticator scheme
        /// </summary>
        /// <example>
        /// digest
        /// </example>
        string Scheme { get; }

        /// <summary>
        /// Create a authentication challenge.
        /// </summary>
        /// <param name="realm">Realm that the user should authenticate in</param>
        /// <param name="options">First options specifies if true if username/password is correct but not cnonce.</param>
        /// <returns>A WWW-Authenticate header.</returns>
        /// <exception cref="ArgumentNullException">If realm is empty or <c>null</c>.</exception>
        IHeader CreateChallenge(string realm, object[] options);
    }

    /// <summary>
    /// Delegate used by authenticators to be able to request user information needed to authenticate.
    /// </summary>
    /// <param name="context">Scheme specific context</param>
    /// <returns><c>true</c> if information was found.</returns>
    public delegate bool AuthenticateHandler(IAuthenticationContext context);

    /// <summary>
    /// Interface implemented by each <see cref="IAuthenticator"/>. 
    /// </summary>
    /// <remarks>
    /// Used by <see cref="IAuthenticator"/> to get authentication scheme specific information.
    /// </remarks>
    public interface IAuthenticationContext
    {
        
    }
    
}