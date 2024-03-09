namespace OpenF1.Console;

public sealed class CursorUpInputHandler(State state) : IInputHandler
{
    public Screen[]? ApplicableScreens => state.CursorOffset == 0 ? [] : Enum.GetValues<Screen>();

    public ConsoleKey ConsoleKey => ConsoleKey.UpArrow;

    public string Description => "Up";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        if (state.CursorOffset <= 0)
        {
            state.CursorOffset = 0;
        }
        else
        {
            state.CursorOffset--;
        }

        return Task.CompletedTask;
    }
}

public sealed class CursorDownInputHandler(State state) : IInputHandler
{
    public Screen[]? ApplicableScreens =>
        Enum.GetValues<Screen>().Where(x => x != Screen.Main).ToArray();

    public ConsoleKey ConsoleKey => ConsoleKey.DownArrow;

    public string Description => $"Down {state.CursorOffset}";

    public Task ExecuteAsync(ConsoleKeyInfo consoleKeyInfo)
    {
        state.CursorOffset++;
        return Task.CompletedTask;
    }
}
