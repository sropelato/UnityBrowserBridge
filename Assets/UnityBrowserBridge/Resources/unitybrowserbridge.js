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

_ubb_logBrowserCall = function (call) {
    if (!liveBrowserCalls)
        return
    let callDiv = document.createElement('div')
    callDiv.innerHTML = call
    callDiv.className = 'calls_list_entry'
    document.getElementById('browser_calls_list').appendChild(callDiv)
    document.getElementById('browser_calls_list').scrollTop = document.getElementById('browser_calls_list').scrollHeight
}

_ubb_logUnityCall = function (call) {
    if (!liveUnityCalls)
        return
    let callDiv = document.createElement('div')
    callDiv.innerHTML = call
    callDiv.className = 'calls_list_entry'
    document.getElementById('unity_calls_list').appendChild(callDiv)
    document.getElementById('unity_calls_list').scrollTop = document.getElementById('unity_calls_list').scrollHeight
}

_ubb_changeLiveBrowserCalls = function () {
    if (document.getElementById('live_browser_calls_checkbox').checked) {
        // clear browser calls list
        //document.getElementById('browser_calls_list').innerHTML = ''
        liveBrowserCalls = true
    } else
        liveBrowserCalls = false
}

_ubb_changeLiveUnityCalls = function () {
    if (document.getElementById('live_unity_calls_checkbox').checked) {
        // clear unity calls list
        //document.getElementById('unity_calls_list').innerHTML = ''
        liveUnityCalls = true
    } else
        liveUnityCalls = false
}

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
