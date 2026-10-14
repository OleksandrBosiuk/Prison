using System;

namespace Prison.Core
{
    public class InmateEventArgs : EventArgs
    {
        public Prisoner Actor { get; }
        public string Message { get; }

        public InmateEventArgs(Prisoner actor, string message)
        {
            Actor = actor;
            Message = message;
        }
    }
}
