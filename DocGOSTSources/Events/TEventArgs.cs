using System;

namespace GostDOC.Events
{
    public class TEventArgs<T> : EventArgs
    {
        public T Arg { get; set; }

        public TEventArgs(T aArg)
        {
            Arg = aArg;
        }
    }

    public class TEventArgs<T, V> : EventArgs
    {
        public T Arg { get; set; }

        public V Arg2 { get; set; }

        public TEventArgs(T aArg1, V aArg2)
        {
            Arg = aArg1;
            Arg2 = aArg2;
        }
    }
}
