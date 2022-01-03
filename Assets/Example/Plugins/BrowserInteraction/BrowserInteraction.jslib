mergeInto(LibraryManager.library, {
	GetDateAndTime: function (prefix) {
	    var returnStr = getDateAndTime(Pointer_stringify(prefix))
        var bufferSize = lengthBytesUTF8(returnStr) + 1
        var buffer = _malloc(bufferSize)
        stringToUTF8(returnStr, buffer, bufferSize)
        return buffer
	},
    GetDateAndTimeAsync: function (prefix) {
        getDateAndTimeAsync(Pointer_stringify(prefix))
    }
})
