using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Altus.Core.Presentation.Commands
{
    public delegate void ExecutingCommandEventHandler(object sender, ExecutingCommandEventArgs e);
    public delegate void ExecutedCommandEventHandler(object sender, ExecutedCommandEventArgs e);

    public class ExecutingCommandEventArgs : ExecutedCommandEventArgs
    {
        public ExecutingCommandEventArgs(Command command, object parm = null, bool cancel = false) : base(command, parm)
        {
            this.Cancel = cancel;
        }
        public bool Cancel { get; set; }
    }

    public class ExecutedCommandEventArgs : EventArgs
    {
        public ExecutedCommandEventArgs(Command command, object parm = null)
        {
            this.Command = command;
            this.Parameter = parm;
        }
        public object Parameter { get; set; }
        public Command Command { get; private set; }
    }

    public class Command : ICommand
    {
        public event EventHandler CanExecuteChanged;
        public event ExecutingCommandEventHandler Executing;
        public event ExecutingCommandEventHandler Executed;

        [ThreadStatic]
        private static Command _current;

        public static Command Current
        {
            get { return _current; }
        }

        public Command(Action action, bool canExecute = true)
        {
            this.Action = action;
            this.CanExecute = canExecute;
        }

        public Command(Action<object> action, bool canExecute = true)
        {
            this.Action = action;
            this.CanExecute = canExecute;
        }

        public Command(Action action, ICommandCanExecuteChangedNotifier canExecuteNotifier)
        {
            this.Action = action;
            this.Notifier = canExecuteNotifier;
            this.Notifier.CanExecuteChanged += Notifier_CanExecuteChanged;
            this.CanExecute = this.Notifier.CanExecute();
        }

        public Command(Action<object> action, ICommandCanExecuteChangedNotifier canExecuteNotifier)
        {
            this.Action = action;
            this.Notifier = canExecuteNotifier;
            this.Notifier.CanExecuteChanged += Notifier_CanExecuteChanged;
            this.CanExecute = this.Notifier.CanExecute();
        }

        void Notifier_CanExecuteChanged(object sender, EventArgs e)
        {
            this.CanExecute = this.Notifier.CanExecute();
        }

        public Delegate Action { get; private set; }
        public object Tag { get; set; }
        private ICommandCanExecuteChangedNotifier Notifier { get; set; }

        private bool _canExecute = true;
        public bool CanExecute 
        {
            get { return _canExecute; }
            set
            {
                if (value != _canExecute)
                {
                    _canExecute = value;
                    if (this.CanExecuteChanged != null)
                    {
                        this.CanExecuteChanged(this, new EventArgs());
                    }
                }
            }
        }

        bool ICommand.CanExecute(object parameter)
        {
            return CanExecute;
        }


        void ICommand.Execute(object parameter)
        {
            if (CanExecute)
            {
                _current = this;
                ExecutingCommandEventArgs e = new ExecutingCommandEventArgs(this, parameter);
                if (this.Executing != null)
                {
                    this.Executing(this, e);
                }

                if (!e.Cancel)
                {
                    if (this.Action.GetType().Equals(typeof(Action)))
                    {
                        ((Action)this.Action)();
                    }
                    else
                    {
                        ((Action<object>)this.Action)(e.Parameter);
                    }

                    if (this.Executed != null)
                    {
                        this.Executed(this, e);
                    }
                }
                _current = null;
            }
        }
    }

    public interface ICommandCanExecuteChangedNotifier
    {
        event EventHandler CanExecuteChanged;
        Func<bool> CanExecute { get; }
    }

    public class PropertyChangedCommandCanExecuteNotifier : ICommandCanExecuteChangedNotifier
    {
        public PropertyChangedCommandCanExecuteNotifier(INotifyPropertyChanged source, string propertyName, Func<bool> canExecuteCallback)
        {
            CanExecute = canExecuteCallback;
            Source = source;
            PropertyName = propertyName;
            Source.PropertyChanged += Source_PropertyChanged;
        }

        void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(PropertyName))
            {
                if (CanExecuteChanged != null)
                    CanExecuteChanged(Source, new EventArgs());
            }
        }

        public event EventHandler CanExecuteChanged;
        public INotifyPropertyChanged Source { get; private set; }
        public string PropertyName { get; private set; }

        public Func<bool> CanExecute
        {
            get;
            private set;
        }
    }
}
