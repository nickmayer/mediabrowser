namespace HttpServer.Authentication
{
    /// <summary>
    /// Context used when doing basic authentication.
    /// </summary>
    public class BasicAuthenticationContext : IAuthenticationContext
    {
        /// <summary>
        /// Gets realm
        /// </summary>
        public string Realm { get; private set; }

        /// <summary>
        /// Gets user name
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Gets password.
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicAuthenticationContext"/> class.
        /// </summary>
        /// <param name="realm">The realm.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public BasicAuthenticationContext(string realm, string userName, string password)
        {
            Realm = realm;
            UserName = userName;
            Password = password;
        }
    }
}