using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace GostDOC
{
    public class Command : ICommand
    {
        private Action<object> _execute;
        private readonly ObservableProperty<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public Command(Action<object> execute, ObservableProperty<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;

            if (_canExecute != null)
            {
                _canExecute.PropertyChanged += (o, i) => {
                    RaiseCanExecuteChanged();
                };
            }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute.Value;
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
  
    public class Command<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly ObservableProperty<bool> _canExecute;
                
        /// <summary>
        /// Создано при вызове RaiseCanExecuteChanged.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Создает новую команду, которая всегда может выполняться.
        /// </summary>
        /// <param name="execute">Логика выполнения.</param>
        public Command(Action<T> execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Создает новую команду.
        /// </summary>
        /// <param name="execute">Логика выполнения.</param>
        /// <param name="canExecute">Логика состояния выполнения.</param>
        public Command(Action<T> execute, ObservableProperty<bool> canExecute)
        {
            _execute =  execute ?? throw new ArgumentNullException("execute");
            _canExecute = canExecute;

            if (_canExecute != null)
            {
                _canExecute.PropertyChanged += (o, i) => {
                    RaiseCanExecuteChanged();
                };
            }
        }

        /// <summary>
        /// Определяет, можно ли выполнить эту команду <see cref="Command"/> в текущем состоянии.
        /// </summary>
        /// <param name="parameter">
        /// Данные, используемые командой. Если команда не требует передачи данных, этот объект можно установить равным NULL.
        /// </param>
        /// <returns>true, если команда может быть выполнена; в противном случае - false.</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute.Value;
        }

        /// <summary>
        /// Выполняет <see cref="Command"/> текущей цели команды.
        /// </summary>
        /// <param name="parameter">
        /// Данные, используемые командой. Если команда не требует передачи данных, этот объект можно установить равным NULL.
        /// </param>
        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        /// <summary>
        /// Метод, используемый для создания события <see cref="CanExecuteChanged"/>
        /// чтобы показать, что возвращаемое значение <see cref="CanExecute"/>
        /// метод изменился.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }

    public static class Ext
    {
        public static async void FireAndForget(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception)
            {
            }
        }
    }

    public class AsyncCommand : IAsyncCommand
    {
        public event EventHandler CanExecuteChanged;

        private bool _isExecuting;
        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;

        public AsyncCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute()
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    _isExecuting = true;
                    await _execute();
                }
                finally
                {
                    _isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Explicit implementations
        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute();
        }

        void ICommand.Execute(object parameter)
        {
            ExecuteAsync().FireAndForget();
        }
        #endregion
    }
}
