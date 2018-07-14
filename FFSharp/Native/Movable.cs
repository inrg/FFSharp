﻿using System;
using System.Diagnostics;

using JetBrains.Annotations;

namespace FFSharp.Native
{
    /// <summary>
    /// Value type wrapping a relocatable pointer to a native struct.
    /// </summary>
    /// <typeparam name="T">The pointed-to struct type.</typeparam>
    /// <remarks>
    /// Use this instead of a <c>T**</c> pointer to better represent the intention and to statically
    /// check contracts. Allows passing pointers through safe contexts.
    /// </remarks>
    // ReSharper disable errors
    internal readonly unsafe struct Movable<T> :
        IEquatable<Movable<T>>
        // cannot implement IEquatable<T**>
        where T : unmanaged
    {
        /// <summary>
        /// The null pointer <see cref="Movable{T}"/>.
        /// </summary>
        public static readonly Movable<T> Null = default;

        /// <summary>
        /// Initialize a <see cref="Movable{T}"/>.
        /// </summary>
        /// <param name="ARaw">The target pointer pointer.</param>
        public Movable([CanBeNull] T** ARaw)
        {
            Raw = ARaw;
        }

        /// <summary>
        /// Get the non-<see langword="null"/> value of this or a default.
        /// </summary>
        /// <param name="ADefault">The default <see cref="Movable{T}"/>.</param>
        /// <returns>
        /// This if <see cref="IsNull"/> is <see langword="false"/>; otherwise
        /// <paramref name="ADefault"/>.
        /// </returns>
        [Pure]
        public Movable<T> Or(Movable<T> ADefault) => !IsNull ? this : ADefault;
        /// <summary>
        /// Get the present <see cref="Target"/> of this or a default.
        /// </summary>
        /// <param name="ADefault">The default target <see cref="Fixed{T}"/>.</param>
        /// <returns>
        /// This if <see cref="IsPresent"/> is <see langword="true"/>; otherwise
        /// <paramref name="ADefault"/>.
        /// </returns>
        [Pure]
        public Fixed<T> TargetOr(Fixed<T> ADefault) => IsPresent ? new Fixed<T>(*Raw) : ADefault;

        /// <summary>
        /// Cast to a different underlying struct type.
        /// </summary>
        /// <typeparam name="TTo">The struct type to cast to.</typeparam>
        /// <returns>A casted <see cref="Movable{T}"/>.</returns>
        /// <remarks>
        /// This cast can never fail, but cannot be checked for semantic correctness. Use only when
        /// dynamical correctness is known.
        /// </remarks>
        [Pure]
        public Movable<TTo> Cast<TTo>() where TTo : unmanaged => (TTo**) Raw;

        #region IEquatable<Movable<T>>
        /// <inheritdoc />
        [Pure]
        public bool Equals(Movable<T> ARef) => Raw == ARef.Raw;
        #endregion

        /// <summary>
        /// Check whether this <see cref="Movable{T}"/> is equal to the specified pointer.
        /// </summary>
        /// <param name="APtr">The pointer.</param>
        /// <returns>
        /// <see langword="true"/> if <see cref="Raw"/> is equal to <paramref name="APtr"/>;
        /// otherwise <see langword="false"/>.
        /// </returns>
        [Pure]
        public bool Equals(T** APtr) => Raw == APtr;

        #region System.Object overrides
        /// <inheritdoc />
        public override bool Equals(object AObject)
        {
            switch (AObject)
            {
                case Movable<T> movable:
                    return Equals(movable);

                default:
                    return false;
            }
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Address.GetHashCode();
        }
        /// <inheritdoc />
        public override string ToString()
        {
            return $"Movable<{typeof(T).Name}>(0x{Address.ToUInt64():X16})";
        }
        #endregion

        /// <summary>
        /// Get the pointer to the pointer to the struct.
        /// </summary>
        [CanBeNull]
        public T** Raw { get; }
        /// <summary>
        /// Get the address of <see cref="Raw"/> as an <see cref="IntPtr"/>.
        /// </summary>
        public IntPtr Address => (IntPtr)Raw;

        /// <summary>
        /// Set the target pointer.
        /// </summary>
        /// <param name="ATarget">The target <see cref="Fixed{T}"/>.</param>
        /// <remarks>
        /// Calling this method when <see cref="IsNull"/> is <see langword="true"/> results in
        /// undefined behaviour!
        /// </remarks>
        public void SetTarget(Fixed<T> ATarget)
        {
            Debug.Assert(
                !IsNull,
                "Movable is null.",
                "This indicates a severe logic error in the code."
            );

            *Raw = ATarget.Raw;
        }

        /// <summary>
        /// Get the target pointer to the struct.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Getting this property when <see cref="IsNull"/> is <see langword="true"/> results in
        /// undefined behaviour!
        /// </para>
        /// <para>
        /// To set the target, use <see cref="SetTarget(Fixed{T})"/>. Setters are not allowed on
        /// immutable structs, so one has to use a method instead.
        /// </para>
        /// </remarks>
        public Fixed<T> Target
        {
            get
            {
                Debug.Assert(
                    !IsNull,
                    "Movable is null.",
                    "This indicates a severe logic error in the code."
                );

                return new Fixed<T>(*Raw);
            }
        }

        /// <summary>
        /// Get a value indicating whether <see cref="Raw"/> is <see langword="null"/>.
        /// </summary>
        public bool IsNull => Raw == null;
        /// <summary>
        /// Get a value indicating whether the target pointer is not null.
        /// </summary>
        /// <remarks>
        /// Only <see langword="true"/> if not <see cref="IsNull"/> and <see cref="Target"/> not
        /// <see langword="null"/>.
        /// </remarks>
        public bool IsPresent => !IsNull && !Target.IsNull;

        /// <summary>
        /// Check pointer equality for two <see cref="Movable{T}"/> structs.
        /// </summary>
        /// <param name="ALhs">The left hand side.</param>
        /// <param name="ARhs">The right hand side.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="ALhs"/> and <paramref name="ARhs"/> point
        /// to the same target pointer; otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator ==(Movable<T> ALhs, Movable<T> ARhs) => ALhs.Equals(ARhs);
        /// <summary>
        /// Check pointer inequality for two <see cref="Movable{T}"/> structs.
        /// </summary>
        /// <param name="ALhs">The left hand side.</param>
        /// <param name="ARhs">The right hand side.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="ALhs"/> and <paramref name="ARhs"/> point
        /// to a different target pointer; otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator !=(Movable<T> ALhs, Movable<T> ARhs) => !ALhs.Equals(ARhs);

        /// <summary>
        /// Check pointer equality for a <see cref="Movable{T}"/> struct and a pointer.
        /// </summary>
        /// <param name="ALhs">The left hand side.</param>
        /// <param name="ARhs">The right hand side.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="ALhs"/> and <paramref name="ARhs"/> point
        /// to the same target pointer; otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator ==(Movable<T> ALhs, [CanBeNull] T** ARhs) => ALhs.Raw == ARhs;
        /// <summary>
        /// Check pointer inequality for a <see cref="Movable{T}"/> struct and a pointer.
        /// </summary>
        /// <param name="ALhs">The left hand side.</param>
        /// <param name="ARhs">The right hand side.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="ALhs"/> and <paramref name="ARhs"/> point
        /// to a different target pointer; otherwise <see langword="false"/>.
        /// </returns>
        public static bool operator !=(Movable<T> ALhs, [CanBeNull] T** ARhs) => ALhs.Raw != ARhs;

        /// <summary>
        /// Implicitly convert a <see cref="Movable{T}"/> to it's !<see cref="IsNull"/>.
        /// </summary>
        /// <param name="AMovable">The <see cref="Movable{T}"/>.</param>
        public static implicit operator bool(Movable<T> AMovable) => !AMovable.IsNull;

        /// <summary>
        /// Implicitly convert a pointer to a <see cref="Movable{T}"/>.
        /// </summary>
        /// <param name="APtr">The pointer.</param>
        public static implicit operator Movable<T>([CanBeNull] T** APtr) => new Movable<T>(APtr);
        /// <summary>
        /// Implicitly convert a <see cref="Movable{T}"/> to a pointer.
        /// </summary>
        /// <param name="AMovable">The <see cref="Movable{T}"/>.</param>
        [CanBeNull]
        public static implicit operator T**(Movable<T> AMovable) => AMovable.Raw;

        /// <summary>
        /// Implicitly convert an <see cref="IntPtr"/> to a <see cref="Movable{T}"/>.
        /// </summary>
        /// <param name="AAddress">The address.</param>
        public static implicit operator Movable<T>(IntPtr AAddress)
            => new Movable<T>((T**)AAddress);
        /// <summary>
        /// Implicitly convert a <see cref="Movable{T}"/> to it's <see cref="Address"/>.
        /// </summary>
        /// <param name="AMovable">The <see cref="Movable{T}"/>.</param>
        public static implicit operator IntPtr(Movable<T> AMovable) => AMovable.Address;
    }
    // ReSharper restore errors
}
