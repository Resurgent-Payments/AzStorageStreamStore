namespace LvStreamStore;
using System;
using System.Threading.Tasks;

internal static class TaskExtensions {
    public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan? timeout) {
        timeout ??= TimeSpan.FromSeconds(1);
        return await Task.WhenAny(task, Task.Delay(timeout.Value)) == task
            ? task.Result
            : throw new TimeoutException("Timed out waiting for task to complete.");
    }

    public static async Task WithTimeout(this Task task, TimeSpan? timeout) {
        timeout ??= TimeSpan.FromSeconds(1);
        if (await Task.WhenAny(task, Task.Delay(timeout.Value)) != task) {
            throw new TimeoutException("Timed out waiting for task to complete.");
        }
    }
}