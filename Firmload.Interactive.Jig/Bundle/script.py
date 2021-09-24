import time

def FirstTest():
	# Add a test case which asserts that 1 == 1
	tifTestRunner.AreEqual(1,1, "Test description", "test name")

	# Sleep for prettier output
	time.sleep(1)

def WaitForJig():
	attempts = 10

	# Let the jig know that we are ready to receive events
	tifJig.Send("Bundle.Ready()", 0)

	while attempts > 0:
		# Wait for the next event to be received over the socket (tcp) connection
		event = tifJig.AwaitEvent("*", 1000)

		# Check if any response was received within 1000 ms
		if event.ResponseResult == ResponseResult.Ok:
			break
		else:
			tifConsole.AddText("Waiting for event", ElementType.Trace)

		# Its better to have multiple attempts with AwaitEvent (smaller) then one big
		attempts -= 1

	# Make sure we got a Ok result
	if event.ResponseResult != ResponseResult.Ok:
		tifTestRunner.Fail("Did not receive any jig event.")

	tifConsole.Log("Got event " + event.FamilyAndCommand)

	if event.FamilyAndCommand == "Request.ReadRpm":
		tifJig.Send("Response.ReadRpm(value:1337)", 0)
	else:
		tifTestRunner.Fail("Didnt know how to handle " + event.FamilyAndCommand)

	# Sleep for prettier output
	time.sleep(2)


def LastTest():
	sleepDuration = 0.25
	# Informative messages only displayed during the test
	tifConsole.AddText("This is info message", ElementType.Info)
	time.sleep(sleepDuration)

	tifConsole.AddText("This is warning message", ElementType.Warn)
	time.sleep(sleepDuration)

	tifConsole.AddText("This is error message", ElementType.Error)
	time.sleep(sleepDuration)

	tifConsole.AddText("This is debug message", ElementType.Debug)
	time.sleep(sleepDuration)

	tifConsole.AddText("This is trace message", ElementType.Trace)
	time.sleep(sleepDuration)


	# Data validation (unit-test:ish) which are stored as a result and
	# uploaded to backend to enable further analysis

	tifTestRunner.AreInRange(2, 1,3, "Are in range", "are-in-range")
	time.sleep(sleepDuration)

	tifTestRunner.AreNotInRange(4,1,3, "Are not in range", "are-not-in-range")
	time.sleep(sleepDuration)

	tifTestRunner.AreGreaterThan(3,1, "Are greater than", "are-greater-than")
	time.sleep(sleepDuration)

	tifTestRunner.AreLessThan(1,3, "Are less than", "are-less-than")
	time.sleep(sleepDuration)

	tifTestRunner.AreEqual(1,1, "Are equal", "are-equal")
	time.sleep(sleepDuration)

	tifTestRunner.AreNotEqual(1,2, "Are not equal", "are-not-equal")
	time.sleep(sleepDuration)


	tifTestRunner.Pass("Passed test", "pass")
	time.sleep(sleepDuration)