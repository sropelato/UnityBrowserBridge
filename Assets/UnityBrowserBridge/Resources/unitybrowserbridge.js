// ready when everything has been loaded
window.addEventListener('load', function () {
    // set status to 'ready'
    document.getElementById('status_light').className = 'green'
    document.getElementById('status_text').innerText = 'ready'

    // send ready to server
    const xhttp = new XMLHttpRequest();
    xhttp.open("GET", "ubbReady", true);
    xhttp.send();
})

var liveBrowserCalls = false
var liveUnityCalls = false

// displays the call in the browser call list
_ubb_logBrowserCall = function (call) {
    if (!liveBrowserCalls)
        return
    let callDiv = document.createElement('div')
    callDiv.innerHTML = call
    callDiv.className = 'calls_list_entry'
    document.getElementById('browser_calls_list').appendChild(callDiv)
    document.getElementById('browser_calls_list').scrollTop = document.getElementById('browser_calls_list').scrollHeight
}

// displays the call in the Unity call list
_ubb_logUnityCall = function (call) {
    if (!liveUnityCalls)
        return
    let callDiv = document.createElement('div')
    callDiv.innerHTML = call
    callDiv.className = 'calls_list_entry'
    document.getElementById('unity_calls_list').appendChild(callDiv)
    document.getElementById('unity_calls_list').scrollTop = document.getElementById('unity_calls_list').scrollHeight
}

// toggle display of live browser calls
_ubb_changeLiveBrowserCalls = function () {
    liveBrowserCalls = document.getElementById('live_browser_calls_checkbox').checked
}

// toggle display of live Unity calls
_ubb_changeLiveUnityCalls = function () {
    liveUnityCalls = document.getElementById('live_unity_calls_checkbox').checked
}

// clear browser calls list
_ubb_clearBrowserCalls = function () {
    document.getElementById('browser_calls_list').innerHTML = ''
}

// clear unity calls list
_ubb_clearUnityCalls = function () {
    document.getElementById('unity_calls_list').innerHTML = ''
}

// emulates unityInstance object of Unity's WebGL build with a 'SendMessage' function to invoke methods in Unity game objects
unityInstance = {
    SendMessage: function (gameObject, methodName, value) {
        // send request to server
        const xhttp = new XMLHttpRequest();
        if (typeof (value) == 'undefined') {
            xhttp.open("GET", "ubbSendMessage?gameObject=" + encodeURI(gameObject) + "&methodName=" + encodeURI(methodName), true);
            _ubb_logUnityCall(gameObject + '&nbsp;&rarr;&nbsp;' + methodName + '()')
        } else if (typeof (value) == 'number') {
            xhttp.open("GET", "ubbSendMessage?gameObject=" + encodeURI(gameObject) + "&methodName=" + encodeURI(methodName) + "&valueNum=" + encodeURI(value), true);
            _ubb_logUnityCall(gameObject + '&nbsp;&rarr;&nbsp;' + methodName + '(' + value + ')')
        } else {
            xhttp.open("GET", "ubbSendMessage?gameObject=" + encodeURI(gameObject) + "&methodName=" + encodeURI(methodName) + "&valueStr=" + encodeURI(value), true);
            _ubb_logUnityCall(gameObject + '&nbsp;&rarr;&nbsp;' + methodName + '("' + String(value).replace(/'/g, "\\'") + '")')
        }
        xhttp.send();
    }
}
