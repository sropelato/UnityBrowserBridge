var unityInstance = null

$(window).on('load', function () {
	// disable ajax caching
	$.ajaxSetup({ cache: false })

	// resize canvas
	resizeCanvas()
	$(window).on('resize', resizeCanvas)

	// show loading bar
	$('#loading_bar').css('display', 'block')

	// load game
	createUnityInstance($('#canvas').get(0), buildConfig, (progress) => {
		$('#loading_bar_full').css('width', 100 * progress + '%')
	}).then((unityInstance) => {
		this.unityInstance = unityInstance
		// hide loading bar
		$('#loading_bar').css('display', 'none')
	}).catch((message) => {
		showBanner(message, 'error')
	})
})

showBanner = function (msg, type) {
	let msgElement = $('<div class="banner_content">' + msg + '</div>')
	msgElement.appendTo('#banner')
	if (type == 'error')
		msgElement.addClass('error')
	else {
		if (type == 'warning')
			msgElement.addClass('warning')
		setTimeout(function () {
			msgElement.remove()
			updateBannerVisibility()
		}, 5000)
	}
	updateBannerVisibility()
}

resizeCanvas = function () {
	for (let i = 0; i < 2; i++) {
		$('#canvas').prop('width', $('#canvas_container').width())
		$('#canvas').prop('height', $('#canvas_container').height())
	}

}

updateBannerVisibility = function () {
	if ($('#banner').children().length > 0)
		$('#banner').css('display', 'block')
	else
		$('#banner').css('display', 'none')
}

