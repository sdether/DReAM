using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MindTouch {
    /// <summary>
    /// Equality computation delegate.
    /// </summary>
    /// <typeparam name="T">Type of the values to be compared</typeparam>
    /// <param name="left">Left-hand value</param>
    /// <param name="right">Right-hand value</param>
    /// <returns><see langword="True"/> if left and right are the same value as determined by the delegate implementation.</returns>
    public delegate bool Equality<T>(T left, T right);
}
