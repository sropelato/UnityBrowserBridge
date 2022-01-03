using System;
using System.Collections.Generic;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using UnityEditor;
using UnityEngine;

namespace UnityBrowserBridge
{
    public class UnityBrowserBridge : MonoBehaviour
    {
        #if UNITY_EDITOR

        public static UnityBrowserBridge Instance { get; private set; }

        public int httpServerPort = 63388;
        public TextAsset indexFile;
        public List<DefaultAsset> includeProjectFiles = new List<DefaultAsset>();
        public List<string> includeExternalFiles = new List<string>();

        private HttpServer httpServer = null;
        private IWebDriver webDriver = null;

        private readonly Queue<KeyValuePair<string, string>> messagesWithoutValue = new Queue<KeyValuePair<string, string>>();
        private readonly Queue<KeyValuePair<string, KeyValuePair<string, object>>> messagesWithValue = new Queue<KeyValuePair<string, KeyValuePair<string, object>>>();

        public bool Ready { get; set; } = false;

        private void Awake()
        {
            Instance = this;
        }

        private void OnApplicationQuit()
        {
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

        private void Start()
        {
            // create http server
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

        private void Update()
        {
            // send messages to game objects
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

        // no return value
        public void ExecuteJS(string jsCommand)
        {
            if (!Ready)
            {
                Debug.LogError("Unity Browser Bridge - Browser is not ready");
                return;
            }

            try
            {
                ((IJavaScriptExecutor)webDriver).ExecuteScript("_ubb_logBrowserCall('" + jsCommand.Replace("'", "&apos;") + "')");
                ((IJavaScriptExecutor)webDriver).ExecuteScript(jsCommand);
            }
            catch (Exception e)
            {
                Debug.LogError("Unity Browser Bridge - Could not execute command " + jsCommand + ": " + e.Message);
            }
        }

        // expect return value of type T
        public T ExecuteJS<T>(string jsCommand)
        {
            if (!Ready)
            {
                Debug.LogError("Unity Browser Bridge - Browser is not ready");
                return default;
            }

            // execute command
            object result;
            try
            {
                ((IJavaScriptExecutor)webDriver).ExecuteScript("_ubb_logBrowserCall('" + jsCommand.Replace("'", "&apos;") + "')");
                result = ((IJavaScriptExecutor)webDriver).ExecuteScript("return " + jsCommand);
            }
            catch (Exception e)
            {
                Debug.LogError("Unity Browser Bridge - Could not execute command " + jsCommand + ": " + e.Message);
                return default;
            }

            // convert type
            try
            {
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception)
            {
                Debug.LogError("Unity Browser Bridge - Could not convert return value from " + jsCommand + " to type " + typeof(T));
                return default;
            }
        }

        public void SendMessageToGameObject(string gameObjectName, string methodName)
        {
            lock (messagesWithoutValue)
                messagesWithoutValue.Enqueue(new KeyValuePair<string, string>(gameObjectName, methodName));
        }

        public void SendMessageToGameObject(string gameObjectName, string methodName, object value)
        {
            lock (messagesWithValue)
                messagesWithValue.Enqueue(new KeyValuePair<string, KeyValuePair<string, object>>(gameObjectName, new KeyValuePair<string, object>(methodName, value)));
        }

        # endif
    }
}