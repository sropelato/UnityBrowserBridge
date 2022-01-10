// return string with formatted date and time
getDateAndTime = function (locale) {
	return new Date().toLocaleString(locale)
}

// get formatted date and time and invoke the 'SetDateAndTime' method on the 'Main' game object (after a delay of 1000ms)
getDateAndTimeAsync = function (locale) {
	setTimeout(function () {
		unityInstance.SendMessage('Main', 'SetDateAndTime', new Date().toLocaleString(locale))
	}, 1000)
}
