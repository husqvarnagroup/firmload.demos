import time
timeout = 0

def CallbackDialogs():

	global timeout
	timeout = 5

	# Shows a dialog which is automatically dismissed once the callback function returns true
	tifDialogs.ShowConditionalInstructionWithCallback("Wait for call back dialog", "This dialog waits for a python-function to return true",ConditionalFunction, "./face.png")


def InstructionDialogs():
	# Full dialog

	tifDialogs.ShowInstruction("Manual input", "Please perform an important action then press Confirm.", "./instruction.png")

	# Invalid image url example
	tifDialogs.ShowInstruction("Icon test", "Bad image url", "./bad-url.png")

	# No image specified
	tifDialogs.ShowInstruction("Icon test", "No image url")


	#public dynamic ShowInputDialog(string title, string description, string mediaResource)
def InputDialogs():
	# Shows a dialog where the user may enter a value manually
	result = tifDialogs.ShowInputDialog("Input dialog", "Please enter a value in the textbox below.", "./face.png")

	tifConsole.AddText("Return value '" + str(result) + "'", ElementType.Info)

	# Shows a dialog where the user may pass or fail a test
	tifTestRunner.ShowManualVerification("Pass or fail dialog", "Please press 'Pass' or 'Fail' using the buttons below.", "Test", True)


def ConditionalFunction():
	global timeout

	time.sleep(1)

	timeout = timeout - 1
	return timeout < 1