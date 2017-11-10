using System;

namespace DBLint.DBLintGui
{

    public class ExitAction : IDisposable
    {
        private class ExitAction0 : IDisposable
        {
            private Action _action;

            public ExitAction0(Action action)
            {
                this._action = action;
            }
            public void Dispose()
            {
                _action();
            }
        }
        private class ExitAction1<T> : IDisposable
        {
            private Action<T> _action;
            private T _origValue;

            public ExitAction1(Action<T> action, T origValue)
            {
                this._action = action;
                this._origValue = origValue;
            }
            public void Dispose()
            {
                _action(_origValue);
            }
        }

        private IDisposable _action;

        public static ExitAction Create(Action action)
        {
            var ea = new ExitAction();
            ea._action = new ExitAction0(action);
            return ea;
        }

        public static ExitAction Create<T>(Action<T> action, T origValue)
        {
            var ea = new ExitAction();
            ea._action = new ExitAction1<T>(action, origValue);
            return ea;
        }

        private ExitAction()
        { }

        public void Dispose()
        {
            this._action.Dispose();
        }
    }


}