using System;

namespace UVOCBot.Exceptions
{
    public class DuplicateItemException<T> : Exception
    {
        public T DuplicateItem { get; }

        public DuplicateItemException()
        {
        }

        public DuplicateItemException(T duplicateItem)
        {
            DuplicateItem = duplicateItem;
        }

        public DuplicateItemException(string message)
            : base(message)
        {
        }

        public DuplicateItemException(string message, T duplicateItem)
            : base (message)
        {
            DuplicateItem = duplicateItem;
        }

        public DuplicateItemException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public DuplicateItemException(string message, T duplicateItem, Exception innerException)
            : base (message, innerException)
        {
            DuplicateItem = duplicateItem;
        }
    }
}
