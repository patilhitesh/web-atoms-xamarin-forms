using System;
using System.Linq;
using System.Windows.Input;

namespace WebAtoms
{
    public class AtomCommand : ICommand
    {
        readonly Action action;
        public AtomCommand(Action action)
        {
            this.action = action;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            this.action();
        }
    }
}
