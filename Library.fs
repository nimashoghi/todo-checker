[<AutoOpen>]
module TodoChecker.Library

open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions
open FSharp.Data
open Iris.Option.Builders

open Util

type Fsproj = XmlProvider<"""C:\Users\nimas\Repositories\todo-checker\Mock.fsproj.template""">
type TodoLocation = TodoLocation of path: string * line: int * column: int
type Todo = {
    location: TodoLocation
    comment: string
}

let validate' fileExists path = if not (fileExists path) then None else Some path

/// **Description**
///   * Validates a particular path. If it exists, returns `Some path`. Otherwise, returns `None`.
let inline validate path = validate' File.Exists path

let mergePaths' (``base``: string) input =
    Path.Combine(Path.GetDirectoryName ``base``, input)

/// **Description**
///   * Gets the `input` path relative to the directory of `base`.
let inline mergePaths ``base`` input = mergePaths' ``base`` input

let readXml' getFullPath readAllText validate recurse path =
    let rec run set (Absolute getFullPath path) =
        match set with
        | Contains path -> printfn "ignoring %s" path; [||]
        | _ ->
            let content = readAllText path
            let xml = Fsproj.Parse content

            // Get all the `<Compile Include="*" />` and `<None Inlcude="*" />` elements
            let files =
                xml.ItemGroups
                |> Array.collect (fun itemGroup ->
                    Array.concat [
                        itemGroup.Compiles |> Array.map (fun compile -> mergePaths path compile.Include)
                        itemGroup.Nones |> Array.map (fun none -> mergePaths path none.Include)
                    ])
                |> Array.map ((|Absolute|) getFullPath)

            if not recurse then files else
            // Get all the `<ProjectReference Include="*" />` elements.
            let references =
                xml.ItemGroups
                |> Array.collect (fun itemGroup ->
                    itemGroup.ProjectReferences
                    |> Array.map (fun compile -> compile.Include))
                |> Array.mapSome validate

            // Recursively get all `ProjectReference` elements from child projects.
            // TODO: Test to make sure circular dependency safeguards work properly.
            let referenceFiles =
                references
                |> Array.collect (run (set @@ [path] @@ references))

            Array.concat [
                files
                referenceFiles
            ] |> Array.uniqueHash

    run (HashSet()) path

/// **Description**
///   * Reads and parses the XML from `path`.
///   * Goes through all the `ItemGroup` elements in search of `Compile` and `ProjectReference` elements.
///   * Returns an array of .fs file paths.
let inline readXml recurse path = readXml' Path.GetFullPath File.ReadAllText validate recurse path

[<Struct>]
type CharacterPosition = {
    character: char
    column: int
    line: int
}

let indexToPosition' index (content: string) =
    let lines = String.split "\n" content
    lines
    |> Seq.mapi (fun lineNumber line ->
        line
        |> Seq.mapi (fun columnNumber character -> {
            character = character
            column = columnNumber
            line = lineNumber
        }))
    |> Seq.flatten
    |> Seq.toArray
    |> Array.getIndex index

/// **Description**
///   * Converts an index into a string to a line and column number.
let inline indexToPosition index (content: string) = indexToPosition' index content

let getTodos' getFullPath (Absolute getFullPath path) =
    let content = File.ReadAllText path
    let matches =
        Regex.Matches(
            input = content,
            pattern = @"(?<keyword>TODO|FIXME)\s*:\s*(?<comment>.+)$",
            options = (RegexOptions.Compiled ||| RegexOptions.Multiline))

    matches
    |> Seq.cast<Match>
    |> Seq.toArray
    |> Array.filter (fun ``match`` -> ``match``.Success)
    |> Array.mapSome (fun ``match`` ->
        if ``match``.Groups.["comment"].Success
        then Some (``match``.Groups.["comment"].Value, indexToPosition ``match``.Groups.[0].Index content)
        else None
    )
    |> Array.map (fun (comment, {line = line; column = column}) -> {
        comment = comment
        location = TodoLocation (path, line, column)
    })

/// **Description**
///   * Gets all the TODO comments within the file provided.
///   * Details:
///     * Reads the file.
///     * Looks for TODO patterns.
///     * Returns all until none matches anymore.
let inline getTodos path = getTodos' Path.GetFullPath path

let parseFsproj' recurse path = maybeOr [||] {
    let! path = validate path
    let fsFiles = readXml recurse path
    let todos =
        fsFiles
        |> Array.collect getTodos
    return todos
}

/// **Description**
///   * Parses an fsproj file and returns an array of `Todo` objects.
///   * Details:
///     * Read through the XML of the fsproj.
///     * If we find other project references *and* `recurse` is set to true, we recursively call `parseFsproj` on those too.
let inline parseFsproj recurse filePath = parseFsproj' recurse filePath
