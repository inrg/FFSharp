﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using JetBrains.Annotations;

namespace FFSharp.Native
{
    /// <summary>
    /// Utility base class for implementing an <see cref="IEnumerator{T}"/> that performs a map.
    /// </summary>
    /// <typeparam name="TIn">The input type.</typeparam>
    /// <typeparam name="TOut">The output type.</typeparam>
    internal abstract class MapEnumerator<TIn, TOut> : IEnumerator<TOut>
    {
        [NotNull]
        readonly IEnumerator<TIn> FParent;

        /// <summary>
        /// Create a new <see cref="MapEnumerator{TIn,TOut}"/> instance.
        /// </summary>
        /// <param name="AParent">The parent <see cref="IEnumerator{T}"/>.</param>
        protected MapEnumerator([NotNull] IEnumerator<TIn> AParent)
        {
            Debug.Assert(
                AParent != null,
                "Parent is null.",
                "This indicates a severe logic error."
            );

            FParent = AParent;
        }

        /// <summary>
        /// Map an input to an output element.
        /// </summary>
        /// <param name="AIn">The input element.</param>
        /// <returns>The output element.</returns>
        /// <remarks>
        /// Guaranteed to be performed only once for each input element during a non-resetted
        /// enumeration.
        /// </remarks>
        protected abstract TOut Map(TIn AIn);

        #region IDisposable
        /// <inheritdoc />
        public void Dispose()
        {
            FParent.Dispose();
        }
        #endregion

        #region IEnumerator<TOut>
        /// <inheritdoc />
        public bool MoveNext()
        {
            if (!FParent.MoveNext())
            {
                return false;
            }

            Current = Map(FParent.Current);
            return true;
        }
        /// <inheritdoc />
        public void Reset()
        {
            FParent.Reset();
        }

        /// <inheritdoc />
        public TOut Current { get; private set; }
        #endregion

        #region IEnumerator
        object IEnumerator.Current => Current;
        #endregion
    }
}
