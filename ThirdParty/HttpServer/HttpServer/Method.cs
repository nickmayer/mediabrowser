namespace HttpServer
{
    /// <summary>
    /// HTTP methods.
    /// </summary>
    public enum Method
    {
        /// <summary>
        /// Unknown method
        /// </summary>
        Unknown,

        /// <summary>
        /// Posting data
        /// </summary>
        Post,

        /// <summary>
        /// Get data
        /// </summary>
        Get,

        /// <summary>
        /// Update data
        /// </summary>
        Put,

        /// <summary>
        /// Remove data
        /// </summary>
        Delete,

        /// <summary>
        /// Get only HTTP headers.
        /// </summary>
        Header,

        /// <summary>
        /// Options HTTP 1.1 header.
        /// </summary>
        Options
    }
}