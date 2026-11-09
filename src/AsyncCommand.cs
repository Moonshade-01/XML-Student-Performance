using System.Windows.Input;

namespace StudentPerformance;

public sealed class AsyncCommand(Func<Task> action, Func<bool> canExecute) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute();
    public async void Execute(object? parameter)
    {
        if (CanExecute(parameter)) await action();
    }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
