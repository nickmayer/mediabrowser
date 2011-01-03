namespace HttpServer.Authentication
{
    /// <summary>
    /// Context used when digest authentication is invoked.
    /// </summary>
    /// <remarks>
    /// Since the authentication information is encrypted, your are either expected
    /// to return the password in the <see cref="Password"/> property, or a HA1 hash
    /// in the <see cref="HA1"/> property.
    /// </remarks>
    public class DigestContext : IAuthenticationContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigestContext"/> class.
        /// </summary>
        /// <param name="realm">The realm.</param>
        /// <param name="userName">Name of the user.</param>
        public DigestContext(string realm, string userName)
        {
            Realm = realm;
            UserName = userName;
        }

        /// <summary>
        /// Gets realm that the user is getting authenticated in
        /// </summary>
        public string Realm { get; private set; }

        /// <summary>
        /// Gets or sets entered user name.
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// Gets or sets password to authenticate.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets first hash.
        /// </summary>
        /// <remarks>
        /// See the Digest authentication article on wikipedia for more information.
        /// </remarks>
        public string HA1 { get; set; }
    }
}