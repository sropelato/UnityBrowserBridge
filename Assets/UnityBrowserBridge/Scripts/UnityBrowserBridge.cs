using System;
using System.Collections.Generic;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using UnityEditor;
using UnityEngine;

namespace UBB
{
	/// <summary>
	/// The UnityBrowserBridge class serves as the gateway between the Unity editor and the web browser.
	/// </summary>
	public class UnityBrowserBridge : MonoBehaviour
	{
		#if UNITY_EDITOR

		/// <summary>
		/// Static singleton property to access UnityBrowserBridge instance. 
		/// </summary>
		public static UnityBrowserBridge Instance { get; private set; }

		/// <summary>
		/// Port used for the web server.
		/// </summary>
		public int httpServerPort = 63388;

		/// <summary>
		/// index.html file served by the web server.
		/// Other resources such as style sheets or images will be accessed relative to the path of this file.
		/// </summary>
		public TextAsset indexFile;

		/// <summary>
		/// JavaScript files included in the WebGL project (located in the Assets folder).
		/// </summary>
		public List<DefaultAsset> includeProjectFiles = new List<DefaultAsset>();

		/// <summary>
		/// Path to JavaScript files included in the WebGL project (located outside of the Assets folder).
		/// </summary>
		public List<string> includeExternalFiles = new List<string>();

		// HTTP server
		private HttpServer httpServer = null;

		// web driver to control the browser
		private IWebDriver webDriver = null;

		// messages from the browser to be forwarded to game objects
		private readonly Queue<KeyValuePair<string, string>> messagesWithoutValue = new Queue<KeyValuePair<string, string>>();
		private readonly Queue<KeyValuePair<string, KeyValuePair<string, object>>> messagesWithValue = new Queue<KeyValuePair<string, KeyValuePair<string, object>>>();

		/// <summary>
		/// Indicate whether the browser is ready to execute JavaScript commands.
		/// </summary>
		/// <value><code>True</code> after the browser is ready and all resources have been loaded.</value>
		public bool Ready { get; set; } = false;

		// sets instance property on awake
		private void Awake()
		{
			Instance = this;
		}

		// invoked after application has been terminated
		private void OnApplicationQuit()
		{
			// close web driver
			if (webDriver != null)
			{
				try
				{
					webDriver.Quit();
					Debug.Log("Unity Browser Bridge - Web driver has been closed.");
				}
				catch (Exception e)
				{
					Debug.LogWarning("Unity Browser Bridge - Could not close web driver: " + e.Message);
				}
			}

			// stop web server
			if (httpServer != null)
			{
				try
				{
					httpServer.Stop();
					Debug.Log("Unity Browser Bridge - HTTP server has been stopped.");
				}
				catch (Exception e)
				{
					Debug.LogWarning("Unity Browser Bridge - Could not stop http server: " + e.Message);
				}
			}
		}

		// initializes UnityBrowserBridge on application start
		private void Start()
		{
			// create web server
			httpServer = new HttpServer(httpServerPort, Path.GetDirectoryName(AssetDatabase.GetAssetPath(indexFile)));
			httpServer.Start();

			// add internal files
			foreach (DefaultAsset asset in includeProjectFiles)
				httpServer.AddJSFile(AssetDatabase.GetAssetPath(asset));

			// add external files
			foreach (string path in includeExternalFiles)
				httpServer.AddJSFile(path);

			// open browser
			webDriver = new ChromeDriver();
			webDriver.Url = "http://localhost:" + httpServerPort + "/";
		}

		// update
		private void Update()
		{
			// send queued messages to game objects
			lock (messagesWithoutValue)
			{
				while (messagesWithoutValue.Count > 0)
				{
					KeyValuePair<string, string> message = messagesWithoutValue.Dequeue();
					string gameObjectName = message.Key;
					string methodName = message.Value;
					GameObject go = GameObject.Find(gameObjectName);
					if (go != null)
						go.SendMessage(methodName);
					else
						Debug.LogError("Unity Browser Bridge - Could not find game object '" + gameObjectName + "'");
				}
			}

			// send queued messages to game objects
			lock (messagesWithValue)
			{
				while (messagesWithValue.Count > 0)
				{
					KeyValuePair<string, KeyValuePair<string, object>> message = messagesWithValue.Dequeue();
					string gameObjectName = message.Key;
					string methodName = message.Value.Key;
					object value = message.Value.Value;
					GameObject go = GameObject.Find(gameObjectName);
					if (go != null)
						go.SendMessage(methodName, value);
					else
						Debug.LogError("Unity Browser Bridge - Could not find game object '" + gameObjectName + "'");
				}
			}
		}

		/// <summary>
		/// Executes JavaScript command without return value.
		/// </summary>
		/// <param name="jsCommand">JavaScript command to be executed.</param>
		public void ExecuteJS(string jsCommand)
		{
			if (!Ready)
			{
				Debug.LogError("Unity Browser Bridge - Browser is not ready");
				return;
			}

			try
			{
				((IJavaScriptExecutor) webDriver).ExecuteScript("_ubb_logBrowserCall('" + jsCommand.Replace("'", "&apos;") + "')");
				((IJavaScriptExecutor) webDriver).ExecuteScript(jsCommand);
			}
			catch (Exception e)
			{
				Debug.LogError("Unity Browser Bridge - Could not execute command " + jsCommand + ": " + e.Message);
			}
		}

		/// <summary>
		/// Executes JavaScript command with a return value of type <code>T</code>.
		/// </summary>
		/// <param name="jsCommand">JavaScript command to be executed.</param>
		/// <typeparam name="T">Type of return value.</typeparam>
		/// <returns>The value returned by the given command or <code>default(T)</code> if the command could not be
		/// executed.</returns>
		public T ExecuteJS<T>(string jsCommand)
		{
			// abort if browser is not ready
			if (!Ready)
			{
				Debug.LogError("Unity Browser Bridge - Browser is not ready");
				return default;
			}

			// execute command
			object result;
			try
			{
				((IJavaScriptExecutor) webDriver).ExecuteScript("_ubb_logBrowserCall('" + jsCommand.Replace("'", "&apos;") + "')");
				result = ((IJavaScriptExecutor) webDriver).ExecuteScript("return " + jsCommand);
			}
			catch (Exception e)
			{
				Debug.LogError("Unity Browser Bridge - Could not execute command " + jsCommand + ": " + e.Message);
				return default;
			}

			// convert type
			try
			{
				return (T) Convert.ChangeType(result, typeof(T));
			}
			catch (Exception)
			{
				Debug.LogError("Unity Browser Bridge - Could not convert return value from " + jsCommand + " to type " + typeof(T));
				return default;
			}
		}

		/// <summary>
		/// Enqueues a message without value to be forwarded to the given game object in the next update.
		/// </summary>
		/// <param name="gameObjectName">Name of the game object.</param>
		/// <param name="methodName">Method name.</param>
		public void SendMessageToGameObject(string gameObjectName, string methodName)
		{
			lock (messagesWithoutValue)
				messagesWithoutValue.Enqueue(new KeyValuePair<string, string>(gameObjectName, methodName));
		}


		/// <summary>
		/// Enqueues a message with a value to be forwarded to the given game object in the next update.
		/// </summary>
		/// <param name="gameObjectName">Name of the game object.</param>
		/// <param name="methodName">Method name.</param>
		/// <param name="value">The value sent with the message.</param>
		public void SendMessageToGameObject(string gameObjectName, string methodName, object value)
		{
			lock (messagesWithValue)
				messagesWithValue.Enqueue(new KeyValuePair<string, KeyValuePair<string, object>>(gameObjectName, new KeyValuePair<string, object>(methodName, value)));
		}

		# endif
	}
}