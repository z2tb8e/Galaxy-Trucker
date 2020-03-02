using System;

namespace Client.Exceptions
{
    /// <summary>
    /// Exception thrown when a part at the supplied indices is null, or does not have the neccessary subclass or does not meet the criteria required by the method.
    /// </summary>
    public class InvalidIndexException: Exception
    {
        public InvalidIndexException(string s) : base(s) { }
        public InvalidIndexException() : base() { }
    }
}
