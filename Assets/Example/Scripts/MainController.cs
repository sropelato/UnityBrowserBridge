using UnityEngine;
using UnityEngine.UI;
using UBB;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class MainController : MonoBehaviour
{
	public Text dateAndTimeText;
	public Button getDateAndTimeButton;
	public Button getDateAndTimeAsyncButton;
	public string locale = "en-GB";

	private void Start()
	{
		getDateAndTimeButton.onClick.AddListener(() => { dateAndTimeText.text = GetDateAndTime(locale); });
		getDateAndTimeAsyncButton.onClick.AddListener(() =>
		{
			GetDateAndTimeAsync(locale);
			getDateAndTimeAsyncButton.interactable = false;
		});
	}

	// in webgl, these functions are executed directly in the browser
	// in the editor, they are executed via UnityBrowserBridge
	#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal")]
		public static extern string GetDateAndTime(string locale);
		[DllImport("__Internal")]
		public static extern void GetDateAndTimeAsync(string locale);
	#else

	/// <summary>
	/// Get current date and time formatted according to given locale.
	/// </summary>
	/// <param name="locale">Name of locale to format date (e.g. "en-GB").</param>
	/// <returns>Formatted date and time.</returns>
	public static string GetDateAndTime(string locale)
	{
		return UnityBrowserBridge.Instance.ExecuteJS<string>("getDateAndTime('" + locale + "')");
	}

	/// <summary>
	/// Asynchronously gets the current date and time formatted according to given locale.
	/// This will trigger the browser to invoke the <code>SetDateAndTime</code> method with the formatted date and time.
	/// </summary>
	/// <param name="locale">Name of locale to format date (e.g. "en-GB").</param>
	public static void GetDateAndTimeAsync(string locale)
	{
		UnityBrowserBridge.Instance.ExecuteJS("getDateAndTimeAsync('" + locale + "')");
	}

	#endif

	/// <summary>
	/// Displays the formatted date and time.
	/// </summary>
	/// <param name="dateAndTimeString">Formatted date and time.</param>
	public void SetDateAndTime(string dateAndTimeString)
	{
		dateAndTimeText.text = dateAndTimeString;
		getDateAndTimeAsyncButton.interactable = true;
	}
}