using System;

namespace ToDoTools.sources
{
    public class ToDException : Exception
    {
        public ToDException(string message, Exception inner)
            : base(message, inner)
        {}

        public ToDException(string message)
            : base(message)
        {}
    }
}
