using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace MTCG_Karner.Server
{
    /// <summary>
    /// This class provides HTTP server event arguments.
    /// </summary>
    public class HttpSvrEventArgs : EventArgs
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Protected Members                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// TCP client.
        /// </summary>
        protected TcpClient _Client;

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Constructors                                                                                                     //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="client">TCP client object.</param>
        /// <param name="plainMessage">HTTP plain message.</param>
        public HttpSvrEventArgs(TcpClient client, string plainMessage)
        {
            _Client = client;
            PlainMessage = plainMessage;
            Payload = string.Empty;

            string[] lines = plainMessage.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            bool inheaders = true;
            List<HttpHeader> headers = new();

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    string[] inc = lines[0].Split(' ');
                    Method = inc[0];
                    Path = inc[1];
                }
                else if (inheaders)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        inheaders = false;
                    }
                    else
                    {
                        headers.Add(new HttpHeader(lines[i]));
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(Payload))
                    {
                        Payload += "\r\n";
                    }

                    Payload += lines[i];
                }
            }

            Headers = headers.ToArray();
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Public Properties                                                                                                //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the plain HTTP message.
        /// </summary>
        public string PlainMessage { get; protected set; }

        /// <summary>
        /// Gets the HTTP method.
        /// </summary>
        public string Method { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets the request path.
        /// </summary>
        public string Path { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets the HTTP headers.
        /// </summary>
        public HttpHeader[] Headers { get; protected set; }

        /// <summary>
        /// Gets the HTTP payload.
        /// </summary>
        public string Payload { get; protected set; }
        
        public string ResponseData { get; private set; }
        public int ResponseCode { get; set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Public Methods                                                                                                   //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a reply to the HTTP request.
        /// </summary>
        /// <param name="status">Status code.</param>
        /// <param name="payload">Payload.</param>
        public virtual void Reply(int status, string? payload = null)
        {
            string data;

            switch (status)
            {
                case 200:
                    data = "HTTP/1.1 200 OK\n";
                    break;
                case 201:
                    data = "HTTP/1.1 201 Created\n";
                    break;
                case 400:
                    data = "HTTP/1.1 400 Bad Request\n";
                    break;
                case 401:
                    data = "HTTP/1.1 401 Unauthorized\n";
                    break;
                case 403:
                    data = "HTTP/1.1 403 Forbidden\n";
                    break;
                case 404:
                    data = "HTTP/1.1 404 Not Found\n";
                    break;
                case 409:
                    data = "HTTP/1.1 409 Conflict\n";
                    break;
                case 500:
                    data = "HTTP/1.1 500 Internal Server Error\n";
                    break;
                case 503:
                    data = "HTTP/1.1 503 Service Unavailable\n";
                    break;
                case 204:
                    data = "HTTP/1.1 204 No Content\n";
                    break;
                case 301:
                    data = "HTTP/1.1 301 Moved Permanently\n";
                    break;
                case 302:
                    data = "HTTP/1.1 302 Found\n";
                    break;
                case 307:
                    data = "HTTP/1.1 307 Temporary Redirect\n";
                    break;
                case 308:
                    data = "HTTP/1.1 308 Permanent Redirect\n";
                    break;
                case 405:
                    data = "HTTP/1.1 405 Method Not Allowed\n";
                    break;
                case 406:
                    data = "HTTP/1.1 406 Not Acceptable\n";
                    break;
                case 412:
                    data = "HTTP/1.1 412 Precondition Failed\n";
                    break;
                case 415:
                    data = "HTTP/1.1 415 Unsupported Media Type\n";
                    break;
                case 429:
                    data = "HTTP/1.1 429 Too Many Requests\n";
                    break;
                case 451:
                    data = "HTTP/1.1 451 Unavailable For Legal Reasons\n";
                    break;
                case 418:
                    data = "HTTP/1.1 418 I'm a Teapot\n";
                    break;
                default:
                    data = "HTTP/1.1 418 I'm a Teapot\n";
                    break;
            }

            if (string.IsNullOrEmpty(payload))
            {
                data += "Content-Length: 0\n";
            }
            else
            {
                data += $"Content-Length: {Encoding.ASCII.GetByteCount(payload)}\n";
                data += "Content-Type: text/plain\n";
                data += "\n" + payload;
            }
            
            //capture response data for testing
            if (!string.IsNullOrEmpty(payload))
            {
                ResponseData = payload;
            }
            ResponseCode = status;

            // Skip network operations if _Client is null (in tests)
            if (_Client != null)
            {
                byte[] buf = Encoding.ASCII.GetBytes(data);
                _Client.GetStream().Write(buf, 0, buf.Length);
                _Client.Close();
                _Client.Dispose();
            }
        }

        //Test constructor
        public HttpSvrEventArgs(TcpClient client, string plainMessage, IEnumerable<HttpHeader> headers) : this(client,
            plainMessage)
        {
            Headers = headers.ToArray();
        }

        public HttpSvrEventArgs(TcpClient client, IEnumerable<HttpHeader> headers)
        {
            _Client = client;
            Headers = headers.ToArray();
        }

        //for testing
        public static HttpSvrEventArgs CreateForTest(string method, string path, string payload, Dictionary<string, string> headers)
        {
            var httpSvrEventArgs = new HttpSvrEventArgs(null, $"{method} {path} HTTP/1.1\n\n{payload}");
            httpSvrEventArgs.Headers = headers.Select(kvp => new HttpHeader($"{kvp.Key}: {kvp.Value}")).ToArray();
            return httpSvrEventArgs;
        }
    }
}