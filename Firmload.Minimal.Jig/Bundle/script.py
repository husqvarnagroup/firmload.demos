import time

def FirstTest():
	# Add a test case which asserts that 1 == 1
	tifTestRunner.AreEqual(1,1, "Test description", "test name")

	# Sleep for prettier output
	time.sleep(1)


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