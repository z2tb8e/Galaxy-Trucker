using GalaxyTrucker.Model;
using System;

namespace GalaxyTrucker.Exceptions
{
    /// <summary>
    /// Exception thrown when an alien is attempted to be put in a ship which already contains an alien of that type
    /// </summary>
    public class DuplicateAlienException : Exception
    {
        public Personnel Type { get; set; }

        public (int,int) Index { get; set; }

        public DuplicateAlienException(Personnel type, (int, int) index)
            => (Type, Index) = (type, index);
    }
}
