using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

public class SimpleHTTPServer
{
    const string default404Page = @"
<head>
<style>*{
    transition: all 0.6s;
}

html {
    height: 100%;
}

body{
    font-family: 'Lato', sans-serif;
    color: #888;
    margin: 0;
}

#main{
    display: table;
    width: 100%;
    height: 100vh;
    text-align: center;
}

.fof{
	  display: table-cell;
	  vertical-align: middle;
}

.fof h1{
	  font-size: 50px;
	  display: inline-block;
	  padding-right: 12px;
	  animation: type .5s alternate infinite;
}

@keyframes type{
	  from{box-shadow: inset -3px 0px 0px #888;}
	  to{box-shadow: inset -3px 0px 0px transparent;}
}</style>
</head>
<body>
    <div id='main'>
    <div class='fof'>
        <h1>Error 404</h1>
    </div>
    </div>
</body>
";

    public Func<object, string> OnJsonSerialized;
    static int bufferSize = 16;
    public System.Object _methodController;
    private readonly string[] _indexFiles =
    {
                    "index.html",
                    "index.htm",
                    "default.html",
                    "default.htm"
            };

    private static IDictionary<string, string> _mimeTypeMappings = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
			#region extension to MIME type list
					{ ".asf", "video/x-ms-asf" },
                    { ".asx", "video/x-ms-asf" },
                    { ".avi", "video/x-msvideo" },
                    { ".bin", "application/octet-stream" },
                    { ".cco", "application/x-cocoa" },
                    { ".crt", "application/x-x509-ca-cert" },
                    { ".css", "text/css" },
                    { ".deb", "application/octet-stream" },
                    { ".der", "application/x-x509-ca-cert" },
                    { ".dll", "application/octet-stream" },
                    { ".dmg", "application/octet-stream" },
                    { ".ear", "application/java-archive" },
                    { ".eot", "application/octet-stream" },
                    { ".exe", "application/octet-stream" },
                    { ".flv", "video/x-flv" },
                    { ".gif", "image/gif" },
                    { ".hqx", "application/mac-binhex40" },
                    { ".htc", "text/x-component" },
                    { ".htm", "text/html" },
                    { ".html", "text/html" },
                    { ".ico", "image/x-icon" },
                    { ".img", "application/octet-stream" },
                    { ".svg", "image/svg+xml" },
                    { ".iso", "application/octet-stream" },
                    { ".jar", "application/java-archive" },
                    { ".jardiff", "application/x-java-archive-diff" },
                    { ".jng", "image/x-jng" },
                    { ".jnlp", "application/x-java-jnlp-file" },
                    { ".jpeg", "image/jpeg" },
                    { ".jpg", "image/jpeg" },
                    { ".js", "application/x-javascript" },
                    { ".mml", "text/mathml" },
                    { ".mng", "video/x-mng" },
                    { ".mov", "video/quicktime" },
                    { ".mp3", "audio/mpeg" },
                    { ".mpeg", "video/mpeg" },
                    { ".mp4", "video/mp4" },
                    { ".mpg", "video/mpeg" },
                    { ".msi", "application/octet-stream" },
                    { ".msm", "application/octet-stream" },
                    { ".msp", "application/octet-stream" },
                    { ".pdb", "application/x-pilot" },
                    { ".pdf", "application/pdf" },
                    { ".pem", "application/x-x509-ca-cert" },
                    { ".pl", "application/x-perl" },
                    { ".pm", "application/x-perl" },
                    { ".png", "image/png" },
                    { ".prc", "application/x-pilot" },
                    { ".ra", "audio/x-realaudio" },
                    { ".rar", "application/x-rar-compressed" },
                    { ".rpm", "application/x-redhat-package-manager" },
                    { ".rss", "text/xml" },
                    { ".run", "application/x-makeself" },
                    { ".sea", "application/x-sea" },
                    { ".shtml", "text/html" },
                    { ".sit", "application/x-stuffit" },
                    { ".swf", "application/x-shockwave-flash" },
                    { ".tcl", "application/x-tcl" },
                    { ".tk", "application/x-tcl" },
                    { ".txt", "text/plain" },
                    { ".war", "application/java-archive" },
                    { ".wbmp", "image/vnd.wap.wbmp" },
                    { ".wmv", "video/x-ms-wmv" },
                    { ".xml", "text/xml" },
                    { ".xpi", "application/x-xpinstall" },
                    { ".zip", "application/zip" },
			#endregion
			};
    private Thread _serverThread;
    private string _rootDirectory;
    private HttpListener _listener;
    private int _port;

    public int Port
    {
        get { return _port; }
        private set { }
    }

    /// <summary>
    /// Construct server with given port, path ,controller and buffer.
    /// </summary>
    /// <param name="path">The root folder path in your computer (Absolute path)</param>
    /// <param name="port">The port for your http server</param>
    /// <param name="controller">The controller instance for the WebAPI</param>
    /// <param name="buffer">The buffer size for the http response</param>
    public SimpleHTTPServer(string path, int port, System.Object controller, int buffer)
    {
        this._methodController = controller;
        this.Initialize(path, port);
    }

    /// <summary>
    /// Construct server with given port, path and buffer.
    /// </summary>
    /// <param name="path">The root folder path in your computer (Absolute path)</param>
    /// <param name="port">The port for your http server</param>
    /// <param name="buffer">The buffer size for the http response</param>
    public SimpleHTTPServer(string path, int port, int buffer)
    {
        bufferSize = buffer;
        this.Initialize(path, port);
    }

    /// <summary>
    /// Stop Server
    /// </summary>
    public void Stop()
    {
        _serverThread.Abort();
        _listener.Stop();
    }

    private void Listen()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://*:" + _port.ToString() + "/");
        _listener.Start();
        while (true)
        {
            try
            {
                HttpListenerContext context = _listener.GetContext();
                Process(context);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log(ex);
            }
        }
    }

    private void Process(HttpListenerContext context)
    {
        string filename = context.Request.Url.AbsolutePath;
        filename = filename.Substring(1);

        if (string.IsNullOrEmpty(filename))
        {
            foreach (string indexFile in _indexFiles)
            {
                if (File.Exists(Path.Combine(_rootDirectory, indexFile)))
                {
                    filename = indexFile;
                    break;
                }
            }
        }

        filename = Path.Combine(_rootDirectory, filename);

        Dictionary<string, object> namedParameters = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(context.Request.Url.Query))
        {
            UnityEngine.Debug.Log(context.Request.Url.Query);
            var query = context.Request.Url.Query.Replace("?", "").Split('&');
            foreach (var item in query)
            {
                var t = item.Split('=');


                namedParameters.Add(t[0], t[1]);
            }
        }

        var method = TryParseToController(context.Request.Url);

        if (File.Exists(filename))
        {
            TryServeFile();
        }
        //A ASP.Net MVC like controller route
        else if (method != null)
        {
            context.Response.ContentType = "application/json";

            object result = null;
            try
            {
                result = method.InvokeWithNamedParameters(_methodController, namedParameters);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                UnityEngine.Debug.LogError(ex);
                context.Response.StatusDescription = ex.Message;
                goto WebResponse;
            }
            if (result == null)
            {
                result = new VoidResult { msg = "Success" };
            }
            string jsonString = "";
            if (OnJsonSerialized == null)
            {
                UnityEngine.Debug.LogError("There is no JsonSerialize delegate regist on SimpleHTTPServer.OnJsonSerialized");
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.StatusDescription = "There is no JsonSerialize delegate regist on SimpleHTTPServer.OnJsonSerialized";
                goto WebResponse;
            }
            else
            {
                jsonString = OnJsonSerialized.Invoke(result);
            }

            byte[] jsonByte = Encoding.UTF8.GetBytes(jsonString);
            context.Response.ContentLength64 = jsonByte.Length;
            Stream jsonStream = new MemoryStream(jsonByte);
            byte[] buffer = new byte[1024 * bufferSize];
            int nbytes;
            while ((nbytes = jsonStream.Read(buffer, 0, buffer.Length)) > 0)
                context.Response.OutputStream.Write(buffer, 0, nbytes);
            jsonStream.Close();
        }
        else
        {
            byte[] resultByte = Encoding.UTF8.GetBytes(default404Page);
            Stream resultStream = new MemoryStream(resultByte);
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = resultByte.Length;
            context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
            context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

            byte[] buffer = new byte[1024 * bufferSize];
            int nbytes;
            while ((nbytes = resultStream.Read(buffer, 0, buffer.Length)) > 0)
                context.Response.OutputStream.Write(buffer, 0, nbytes);
            resultStream.Close();

        }
    WebResponse:
        context.Response.OutputStream.Flush();
        context.Response.OutputStream.Close();

        void TryServeFile()
        {
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                Stream input = new FileStream(filename, FileMode.Open, FileAccess.Read);

                //Adding permanent http response headers
                string mime;
                context.Response.ContentType = _mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime) ? mime : "application/octet-stream";
                context.Response.ContentLength64 = input.Length;
                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", System.IO.File.GetLastWriteTime(filename).ToString("r"));

                byte[] buffer = new byte[1024 * bufferSize];
                int nbytes;
                while ((nbytes = input.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, nbytes);
                input.Close();

            }
            catch (Exception ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                UnityEngine.Debug.LogError(ex);
                context.Response.StatusDescription = ex.Message;
            }
        }
    }

    private void Initialize(string path, int port)
    {
        this._rootDirectory = path;
        this._port = port;
        _serverThread = new Thread(this.Listen);
        _serverThread.Start();
    }

    System.Reflection.MethodInfo TryParseToController(Uri uri)
    {
        if (uri.Segments.Length <= 1)
        {
            return null;
        }
        string methodName = uri.Segments[1].Replace("/", "");
        System.Reflection.MethodInfo method = null;
        try
        {
            method = _methodController.GetType().GetMethod(methodName);
        }
        catch
        {
            method = null;
        }

        return method;
    }

    //Mark as Serializable to make Unity's JsonUtility works.
    [System.Serializable]
    class VoidResult
    {
        public string msg;
    }
}

//MethodInfo 可使用具名變數的擴充方法
public static class ReflectionExtensions
{

    public static object InvokeWithNamedParameters(this MethodBase self, object obj, IDictionary<string, object> namedParameters)
    {
        return self.Invoke(obj, MapParameters(self, namedParameters));
    }

    public static object[] MapParameters(MethodBase method, IDictionary<string, object> namedParameters)
    {
        ParameterInfo[] paramInfos = method.GetParameters().ToArray();
        object[] parameters = new object[paramInfos.Length];
        int index = 0;
        foreach (var item in paramInfos)
        {
            object parameterName;
            if (!namedParameters.TryGetValue(item.Name, out parameterName))
            {
                parameters[index] = Type.Missing;
                index++;
                continue;
            }
            parameters[index] = ObjectCastTypeByParameterInfo(item, parameterName);
            index++;
        }
        return parameters;
    }
    static object ObjectCastTypeByParameterInfo(ParameterInfo parameterInfo, object value)
    {
        if (parameterInfo.ParameterType == typeof(int) ||
            parameterInfo.ParameterType == typeof(System.Int32) ||
            parameterInfo.ParameterType == typeof(System.Int16) ||
            parameterInfo.ParameterType == typeof(System.Int64))
        {
            return (int)Convert.ChangeType(value, typeof(int));
        }
        else
        {
            return value;
        }

    }
}
