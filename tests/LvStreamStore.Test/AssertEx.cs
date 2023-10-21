namespace LvStreamStore;

public static class AssertEx {
    public static void IsOrBecomesTrue(Func<bool> test, TimeSpan timeoutAfter) {
        CancellationTokenSource cts = new();
        cts.CancelAfter(timeoutAfter);
        Task.Run(async () => {
            while (!cts.IsCancellationRequested) {
                if (test()) {
                    return;
                }
                await Task.Delay(20);
            }
            throw new TimeoutException();
        }, cts.Token);
    }
}