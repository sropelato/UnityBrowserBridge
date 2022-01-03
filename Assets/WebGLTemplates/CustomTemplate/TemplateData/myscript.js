getDateAndTime = function (prefix) {
    return prefix + new Date().toLocaleString()
}

getDateAndTimeAsync = function (prefix) {
    setTimeout(function () {
        unityInstance.SendMessage('Main', 'SetDateAndTime', prefix + new Date().toLocaleString())
    }, 1000)
}
