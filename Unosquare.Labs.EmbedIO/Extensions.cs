﻿namespace Unosquare.Labs.EmbedIO
{
    using Newtonsoft.Json;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Net.WebSockets;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Extension methods to help your coding!
    /// </summary>
    public static class Extensions
    {
        public const string HeaderAcceptEncoding = "Accept-Encoding";
        public const string HeaderContentEncoding = "Content-Encoding";
        public const string HeaderIfModifiedSince = "If-Modified-Since";
        public const string HeaderCacheControl = "Cache-Control";
        public const string HeaderPragma = "Pragma";
        public const string HeaderExpires = "Expires";
        public const string HeaderLastModified = "Last-Modified";
        public const string HeaderIfNotMatch = "If-None-Match";
        public const string HeaderETag = "ETag";
        public const string HeaderAcceptRanges = "Accept-Ranges";
        public const string HeaderRange = "Range";
        public const string HeaderContentRanges = "Content-Range";
        public const string BrowserTimeFormat = "ddd, dd MMM yyyy HH:mm:ss 'GMT'";
        
        /// <summary>
        /// Default Http Status 404 response output
        /// </summary>
        public const string Response404 = "<html><head></head><body><h1>404 - Not Found</h1></body></html>";

        /// <summary>
        /// Default Http Status 500 response output
        /// </summary>
        public const string Response500 =
            "<html><head></head><body><h1>500 - Internal Server Error</h1><h2>Message</h2><pre>{0}</pre><h2>Stack Trace</h2><pre>\r\n{1}</pre></body></html>";

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this HttpListenerContext context, WebServer server)
        {
            return server.SessionModule == null ? null : server.SessionModule.GetSession(context);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="server">The server.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebSocketContext context, WebServer server)
        {
            return server.SessionModule == null ? null : server.SessionModule.GetSession(context);
        }

        /// <summary>
        /// Gets the session object associated to the current context.
        /// Returns null if the LocalSessionWebModule has not been loaded.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebServer server, HttpListenerContext context)
        {
            return server.SessionModule == null ? null : server.SessionModule.GetSession(context);
        }

        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static SessionInfo GetSession(this WebServer server, WebSocketContext context)
        {
            return server.SessionModule == null ? null : server.SessionModule.GetSession(context);
        }

        /// <summary>
        /// Gets the request path for the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string RequestPath(this HttpListenerContext context)
        {
            return context.Request.Url.LocalPath.ToLowerInvariant();
        }

        /// <summary>
        /// Retrieves the exception message, plus all the inner exception messages separated by new lines
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <returns></returns>
        public static string ExceptionMessage(this Exception ex)
        {
            return ex.ExceptionMessage(string.Empty);
        }

        /// <summary>
        /// Retrieves the exception message, plus all the inner exception messages separated by new lines
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="priorMessage">The prior message.</param>
        /// <returns></returns>
        public static string ExceptionMessage(this Exception ex, string priorMessage)
        {
            var fullMessage = string.IsNullOrWhiteSpace(priorMessage) ? ex.Message : priorMessage + "\r\n" + ex.Message;
            if (ex.InnerException != null && string.IsNullOrWhiteSpace(ex.InnerException.Message) == false)
                return ExceptionMessage(ex.InnerException, fullMessage);

            return fullMessage;
        }

        /// <summary>
        /// Sends headers to disable caching on the client side.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void NoCache(this HttpListenerContext context)
        {
            context.Response.AddHeader(HeaderExpires, "Mon, 26 Jul 1997 05:00:00 GMT");
            context.Response.AddHeader(HeaderLastModified, DateTime.UtcNow.ToString(BrowserTimeFormat));
            context.Response.AddHeader(HeaderCacheControl, "no-store, no-cache, must-revalidate");
            context.Response.AddHeader(HeaderPragma, "no-cache");
        }

        /// <summary>
        /// Gets the value for the specified query string key.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string QueryString(this HttpListenerContext context, string key)
        {
            return context.InQueryString(key) ? context.Request.QueryString[key] : null;
        }

        /// <summary>
        /// Determines if a key exists within the Request's query string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static bool InQueryString(this HttpListenerContext context, string key)
        {
            return context.Request.QueryString.AllKeys.Contains(key);
        }

        /// <summary>
        /// Retrieves the Request Verb of this contetext.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static HttpVerbs RequestVerb(this HttpListenerContext context)
        {
            var verb = HttpVerbs.Get;
            Enum.TryParse<HttpVerbs>(context.Request.HttpMethod.ToLowerInvariant().Trim(), true, out verb);
            return verb;
        }

        /// <summary>
        /// Redirects the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="location">The location.</param>
        /// <param name="useAbsoluteUrl">if set to <c>true</c> [use absolute URL].</param>
        public static void Redirect(this HttpListenerContext context, string location, bool useAbsoluteUrl)
        {
            if (useAbsoluteUrl)
            {
                var hostPath = context.Request.Url.GetLeftPart(UriPartial.Authority);
                location = hostPath + location;
            }

            context.Response.StatusCode = 302;
            context.Response.AddHeader("Location", location);
        }

        /// <summary>
        /// Prettifies the json.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static string PrettifyJson(this string json)
        {
            dynamic parsedJson = JsonConvert.DeserializeObject(json);
            return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
        }

        /// <summary>
        /// Outputs a Json Response given a data object
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public static bool JsonResponse(this HttpListenerContext context, object data)
        {
            var jsonFormatting = Formatting.None;
#if DEBUG
            jsonFormatting = Formatting.Indented;
#endif
            var json = JsonConvert.SerializeObject(data, jsonFormatting);
            return context.JsonResponse(json);
        }

        /// <summary>
        /// Outputs a Json Response given a Json string
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static bool JsonResponse(this HttpListenerContext context, string json)
        {
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            context.Response.ContentType = "application/json";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);

            return true;
        }

        /// <summary>
        /// Parses the json as a given type from the request body.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static T ParseJson<T>(this HttpListenerContext context)
            where T : class
        {
            var body = context.RequestBody();
            return body == null ? null : JsonConvert.DeserializeObject<T>(body);
        }

        /// <summary>
        /// Retrieves the request body
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string RequestBody(this HttpListenerContext context)
        {
            if (context.Request.HasEntityBody == false)
                return null;

            using (var body = context.Request.InputStream) // here we have data
            {
                using (var reader = new StreamReader(body, context.Request.ContentEncoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Retrieves the spcified request the header.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        public static string RequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.HasRequestHeader(headerName) == false ? string.Empty : context.Request.Headers[headerName];
        }

        /// <summary>
        /// Determines whether [has request header] [the specified context].
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="headerName">Name of the header.</param>
        /// <returns></returns>
        public static bool HasRequestHeader(this HttpListenerContext context, string headerName)
        {
            return context.Request.Headers[headerName] != null;
        }

        /// <summary>
        /// Compresses the specified buffer using the G-Zip compression algorithm.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        public static byte[] Compress(this byte[] buffer)
        {
            byte[] outputBuffer = null;

            using (var targetStream = new MemoryStream())
            {
                using (var compressor = new GZipStream(targetStream, CompressionMode.Compress, true))
                {
                    compressor.Write(buffer, 0, buffer.Length);
                }
                outputBuffer = targetStream.ToArray();
            }

            return outputBuffer;
        }

        /// <summary>
        /// Hash with MD5
        /// </summary>
        /// <param name="inputBytes"></param>
        /// <returns></returns>
        public static string HashMd5(byte[] inputBytes)
        {
            var hash = MD5.Create().ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Hash with MD5
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string HashMd5(string input)
        {
            return HashMd5(Encoding.ASCII.GetBytes(input));
        }
    }
}