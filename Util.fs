module TodoChecker.Util

open System
open System.Collections.Generic
open System.IO

let (|Absolute|) getAbsolutePath x = Absolute (getAbsolutePath x)

let (|Contains|_|) x (set: HashSet<_>) =
    if set.Contains x
    then Some Contains
    else None

/// **Description**
///   * Concatenates two sets together
let (@@) lhs rhs =
    let set = HashSet<'a>()
    set.UnionWith lhs
    set.UnionWith rhs
    set

module HashSet =
    /// **Description**
    ///   * Converts a `HashSet<'t>` to a `'t []`.
    let toArray (x: HashSet<'a>) =
        let array = Array.zeroCreate<'a> x.Count
        x.CopyTo array
        array

module Array =

    /// **Description**
    ///   * Gets the `i`th element of the array `x`.
    let getIndex i (x: _ []) = x.[i]

    /// **Description**
    ///   * Maps `x` to an `option` type.
    ///   * Filters the result to get `Some` values only.
    ///   * Unwraps the `option`s.
    let mapSome f x =
        x
        |> Array.map f
        |> Array.filter Option.isSome
        |> Array.map Option.get


    /// **Description**
    ///   * Returns a new array with all duplicates in `x` removed.
    ///   * Details:
    ///     * Create a `HashSet` from the array.
    ///     * Convert our `HashSet` back to an array.
    let uniqueHash (x: _ []) = HashSet x |> HashSet.toArray

module String =
    /// **Description**
    ///   * Splits a string into a maximum number of substrings based on the strings in an array. You can specify whether the substrings include empty array elements.
    let split (separator: string) (x: string) = x.Split (separator.ToCharArray())

module Seq =
    /// **Description**
    ///   * Flattens `x` from `seq (seq _)` to `seq _`.
    let flatten (x: seq<#seq<_>>) = Seq.collect id x
