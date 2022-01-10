mergeInto(LibraryManager.library, {
	GetDateAndTime: function (locale) {
	    var returnStr = getDateAndTime(Pointer_stringify(locale))
        var bufferSize = lengthBytesUTF8(returnStr) + 1
        var buffer = _malloc(bufferSize)
        stringToUTF8(returnStr, buffer, bufferSize)
        return buffer
	},
    GetDateAndTimeAsync: function (locale) {
        getDateAndTimeAsync(Pointer_stringify(locale))
    }
})
