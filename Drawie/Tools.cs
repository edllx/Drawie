using System.Text;

namespace Drawie;

public interface ICommand
{
    void Execute();
    Task ExecuteAsync();
}

public class Command(Func<Task> action) : ICommand
{
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    public void Execute()
    {
        action.Invoke();
    }

    public async Task ExecuteAsync()
    {
        await action.Invoke();
    }
}

internal static class IdGenerator
{
    private static string _alphaNum = "abcdefghijklmnopkrstuvwxyz0123456789";

    public static string GenerateRandomString(int len, string prefix = "", int? seed = null)
    {
        StringBuilder builder = new();
        Random r = new Random();

        if (seed is not null && seed.Value >= 0 && seed.Value <= int.MaxValue)
        {
            r = new(seed.Value);
        }
        else
        {
            r = Random.Shared;
        }

        int i = 0;
        for (; i < prefix.Length && i < len; i++)
        {
            builder.Append(prefix[i]);
        }

        for (; i < len; i++)
        {
            var pick = r.Next(_alphaNum.Length);
            builder.Append(_alphaNum[pick]);
        }

        return builder.ToString();
    }
}

/// <summary>
/// Add delay to consecutive command Request discarding commands that are coming to fast
/// </summary>
/// <param name="refresh"> delay between each command in (ms)</param>
/// <remarks>The last command will always get executed</remarks>
public class RequestManager(int delay)
{
    private readonly Stack<Command> _commands = [];
    private readonly SemaphoreSlim _commandSem = new(1, 1);
    private readonly SemaphoreSlim _execCommandSem = new(1, 1);
    private readonly Lock _lock = new();

    private CancellationTokenSource _cts = new();

    public async Task Execute(Command command)
    {
        Cancel();
        try
        {
            await _commandSem.WaitAsync();
            if (_commands.Count > 0)
            {
                _commands.Pop();
            }

            _commands.Push(command);
            _ = QueueCommand(command);
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e);
#endif
        }
        finally
        {
            _commandSem.Release();
        }
    }

    public async Task Save()
    {
        Cancel();
        try
        {
            await _commandSem.WaitAsync();
            if (_commands.Count > 0)
            {
                Command c = _commands.Pop();
                c.Execute();
            }
        }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e);
#endif
        }
        finally
        {
            _commandSem.Release();
        }
    }

    private async Task QueueCommand(Command command)
    {
        try
        {
            // should wait a maximum of refresh ms
            // can throw here

            await _execCommandSem.WaitAsync(_cts.Token);
            // should not throw anymore

            // always release after refresh ms
            // regardless of the time taken for the command to execute
            _ = Task.Run(async () =>
            {
                await Task.Delay(delay);
                _execCommandSem.Release();
            });

            // synchronously execute the command
            await command.ExecuteAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
#if DEBUG
            Console.WriteLine(e);
#endif
        }
    }

    private void Cancel()
    {
        lock (_lock)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }
}
