using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace XenogermPlanner.Genes
{
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        internal static ReferenceEqualityComparer<T> Instance { get; } = new ReferenceEqualityComparer<T>();

        private ReferenceEqualityComparer()
        {
        }

        public bool Equals(T left, T right)
        {
            return ReferenceEquals(left, right);
        }

        public int GetHashCode(T value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }
    }
}