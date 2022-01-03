using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

namespace UnityBrowserBridge
{
    public class MainController : MonoBehaviour
    {
        public Text dateAndTimeText;
        public Button getDateAndTimeButton;
        public Button getDateAndTimeAsyncButton;
        public string prefix = "Timestamp's: ";

        void Start()
        {
            getDateAndTimeButton.onClick.AddListener(() => { dateAndTimeText.text = GetDateAndTime(prefix); });
            getDateAndTimeAsyncButton.onClick.AddListener(() =>
            {
                GetDateAndTimeAsync(prefix);
                getDateAndTimeAsyncButton.interactable = false;
            });
        }

        // in webgl, these functions are executed in the browser
        // in the editor, these functions are executed via UnityBrowserBridge
        #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern string GetDateAndTime(string prefix);
    [DllImport("__Internal")]
    public static extern void GetDateAndTimeAsync(string prefix);
        #else

        public static string GetDateAndTime(string prefix)
        {
            return UnityBrowserBridge.Instance.ExecuteJS<string>("getDateAndTime('" + prefix + "')");
        }

        public static void GetDateAndTimeAsync(string prefix)
        {
            UnityBrowserBridge.Instance.ExecuteJS("getDateAndTimeAsync('" + prefix + "')");
        }

        #endif

        public void SetDateAndTime(string dateAndTimeString)
        {
            dateAndTimeText.text = dateAndTimeString;
            getDateAndTimeAsyncButton.interactable = true;
        }
    }
}