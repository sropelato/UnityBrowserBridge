using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UBB
{
	/// <summary>
	/// The HttpServer class implements a simple web browser to offer files to the browser.
	/// </summary>
	public class HttpServer
	{
		#if UNITY_EDITOR

		// port on which the server listens for connections
		private readonly int port;

		// root path where resources are located
		private readonly string rootPath;

		// JavaScript files included in the project
		private readonly Dictionary<string, string> jsFiles = new Dictionary<string, string>();

		// http listener waiting for connections
		private HttpListener httpListener = null;

		// server thread handling requests in the background
		private Thread serverThread = null;

		// web server has been stopped
		private bool stopped = false;

		/// <summary>
		/// Creates a new HTTP server with a given port and root path.
		/// </summary>
		/// <param name="port">Port on which the server listens for connections.</param>
		/// <param name="rootPath">Root path where resources are located.</param>
		public HttpServer(int port, string rootPath)
		{
			this.port = port;
			this.rootPath = rootPath;
		}

		/// <summary>
		/// Starts the web server.
		/// </summary>
		public void Start()
		{
			httpListener = new HttpListener();
			httpListener.Prefixes.Add("http://*:" + port + "/");
			httpListener.Start();
			serverThread = new Thread(Listen);
			serverThread.Start();
		}

		/// <summary>
		/// Stops the web server.
		/// </summary>
		public void Stop()
		{
			stopped = true;
			serverThread.Abort();
		}

		/// <summary>
		/// Adds a JavaScript file that's included in the web project.
		/// </summary>
		/// <param name="path">Path of the JavaScript file.</param>
		public void AddJSFile(string path)
		{
			string filename = Path.GetFileName(path);
			if (jsFiles.ContainsKey(filename))
			{
				Debug.LogError("Unity Browser Bridge - Cannot add two files with the same name (" + filename + ")");
				return;
			}

			jsFiles.Add(filename, path);
		}

		// listen for connections (serverThread)
		private void Listen()
		{
			while (true)
			{
				try
				{
					HttpListenerContext context = httpListener.GetContext();
					ProcessRequest(context);
				}
				catch (Exception e)
				{
					if (!stopped)
						Debug.LogError("Unity Browser Bridge - Error listening for connections: " + e.Message);
				}
			}
		}

		// process web request
		private void ProcessRequest(HttpListenerContext context)
		{
			string requestPath = context.Request.Url.AbsolutePath;
			if (requestPath.Equals("/"))
			{
				// load index.html file
				string indexHtml = File.ReadAllText(Path.Combine(rootPath, "index.html"));

				// replace script placeholders
				string headScripts = "";
				string scriptList = "";
				foreach (string jsFile in jsFiles.Keys)
				{
					string scriptUrl = "/scripts/" + jsFile;
					headScripts += (headScripts.Length > 0 ? "\n" : "") + "<script src=\"" + scriptUrl + "\"></script>";
					scriptList += (scriptList.Length > 0 ? "\n" : "") + "<a href=\"" + scriptUrl + "\" target=\"_blank\" class=\"script_list_entry\">" + jsFile + "</a>";
				}

				if (scriptList.Length == 0)
				{
					scriptList = "<div class=\"script_list_empty\">empty</div>";
				}

				indexHtml = indexHtml.Replace("<!-- HEAD_SCRIPTS -->", headScripts);
				indexHtml = indexHtml.Replace("<!-- SCRIPT_LIST -->", scriptList);

				// send response
				context.Response.StatusCode = (int)HttpStatusCode.OK;
				context.Response.ContentType = "text/html";
				byte[] responseBytes = Encoding.UTF8.GetBytes(indexHtml);
				context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
			}
			else if (requestPath.Equals("/ubbReady") || requestPath.Equals("/ubbReady/"))
			{
				// browser is ready
				UnityBrowserBridge.Instance.Ready = true;
				Debug.Log("Unity Browser Bridge - Browser is ready");

				// send response
				context.Response.StatusCode = (int)HttpStatusCode.OK;
				context.Response.ContentType = "text/plain";
				byte[] responseBytes = Encoding.UTF8.GetBytes("OK");
				context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
			}
			else if (requestPath.Equals("/ubbSendMessage") || requestPath.Equals("/ubbSendMessage/"))
			{
				// send message to a Unity game object
				string gameObjectName = context.Request.QueryString["gameObject"];
				string methodName = context.Request.QueryString["methodName"];
				if (gameObjectName == null)
				{
					Debug.LogError("Unity Browser Bridge - gameObject cannot be null.");

					// send response
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					context.Response.ContentType = "text/plain";
					byte[] responseBytes = Encoding.UTF8.GetBytes("gameObject must be set.");
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
				else if (methodName == null)
				{
					Debug.LogError("Unity Browser Bridge - methodName cannot be null.");

					// send response
					context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
					context.Response.ContentType = "text/plain";
					byte[] responseBytes = Encoding.UTF8.GetBytes("methodName must be set.");
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
				else if (context.Request.QueryString["valueNum"] != null)
				{
					// send value as number (double)
					double num = double.Parse(context.Request.QueryString["valueNum"]);
					UnityBrowserBridge.Instance.SendMessageToGameObject(gameObjectName, methodName, num);

					// send response
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					context.Response.ContentType = "text/plain";
					byte[] responseBytes = Encoding.UTF8.GetBytes("OK");
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
				else if (context.Request.QueryString["valueStr"] != null)
				{
					// send value as string
					string str = context.Request.QueryString["valueStr"];
					UnityBrowserBridge.Instance.SendMessageToGameObject(gameObjectName, methodName, str);

					// send response
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					context.Response.ContentType = "text/plain";
					byte[] responseBytes = Encoding.UTF8.GetBytes("OK");
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
				else
				{
					// send without value
					UnityBrowserBridge.Instance.SendMessageToGameObject(gameObjectName, methodName);

					// send response
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					context.Response.ContentType = "text/plain";
					byte[] responseBytes = Encoding.UTF8.GetBytes("OK");
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
			}
			else if (requestPath.StartsWith("/scripts/"))
			{
				// request js file
				string filename = requestPath.Substring(9);
				if (jsFiles.ContainsKey(filename) && File.Exists(jsFiles[filename]))
				{
					context.Response.StatusCode = (int)HttpStatusCode.OK;
					context.Response.ContentType = "application/javascript";
					byte[] responseBytes = File.ReadAllBytes(jsFiles[filename]);
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
				else
				{
					Debug.LogWarning("Unity Browser Bridge - File not found: " + requestPath);
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					context.Response.ContentType = "text/plain";
					byte[] responseBytes = Encoding.UTF8.GetBytes("Not found");
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
			}
			else
			{
				// request resource

				// remove leading '/'
				while (requestPath.StartsWith("/"))
					requestPath = requestPath.Substring(1);
				string filename = Path.Combine(rootPath, requestPath);
				if (File.Exists(filename))
				{
					// status code 200
					context.Response.StatusCode = (int)HttpStatusCode.OK;

					// set mime type
					switch (Path.GetExtension(filename).ToLower())
					{
						case ".html":
							context.Response.ContentType = "text/html";
							break;
						case ".css":
							context.Response.ContentType = "text/css";
							break;
						case ".js":
							context.Response.ContentType = "application/javascript";
							break;
						case ".png":
							context.Response.ContentType = "image/png";
							break;
						case ".jpg":
						case ".jpeg":
							context.Response.ContentType = "image/jpeg";
							break;
						case ".ico":
							context.Response.ContentType = "image/x-icon";
							break;
						default:
							context.Response.ContentType = "application/octet-stream";
							break;
					}

					byte[] responseBytes = File.ReadAllBytes(filename);
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
				else
				{
					Debug.LogWarning("Unity Browser Bridge - File not found: " + requestPath);
					context.Response.StatusCode = (int)HttpStatusCode.NotFound;
					context.Response.ContentType = "text/plain";
					byte[] responseBytes = Encoding.UTF8.GetBytes("Not found");
					context.Response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
				}
			}

			// flush and close output stream
			context.Response.OutputStream.Flush();
			context.Response.OutputStream.Close();
		}

		#endif
	}
}