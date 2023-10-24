namespace LvStreamStore;

public static class AssertEx {
    public static void IsOrBecomesTrue(Func<bool> test, TimeSpan timeoutAfter) {

        var deadline = DateTime.Now + timeoutAfter;
        do {
            if (SpinWait.SpinUntil(test, 50)) {
                return;
            }
            if (DateTime.Now > deadline) {
                throw new TimeoutException();
            }
        } while (true);
    }
}