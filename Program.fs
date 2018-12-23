open System
open System.Collections.Generic
open System.IO
open System.Text.RegularExpressions
open Argu
open FSharp.Data
open Iris.Option.Builders

type Fsproj = XmlProvider<"./Mock.fsproj.template">

type Arguments =
| [<AltCommandLine "-s">] Single
| [<MainCommand; ExactlyOnce; Last>] FilePath of path: string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | FilePath _ -> "The path of the fsproj file for the current project."
            | Single -> "If not set, we recursively check fsproj project references."

type TodoLocation = TodoLocation of path: string * line: int * column: int

type Todo = {
    location: TodoLocation
    comment: string
}

[<AutoOpen>]
module internal Utils =
    let (|Absolute|) x = Path.GetFullPath x

    let (|Contains|_|) x (set: HashSet<_>) =
        if set.Contains x
        then Some Contains
        else None

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
        let mapSome f x =
            x
            |> Array.map f
            |> Array.filter Option.isSome
            |> Array.map Option.get


        /// **Description**
        ///   * Returns a new array with all duplicates in `x` removed.
        ///   * Details:
        ///     * Create a HashSet from the array.
        ///     * Convert our HashSet back to an array.
        let uniqueHash (x: _ []) = HashSet x |> HashSet.toArray

/// **Description**
///   * Validates a particular path. If it exists, returns `Some path`. Otherwise, returns `None`.
let validate path = if not (File.Exists path) then None else Some path


/// **Description**
///   * Gets the `input` path relative to `base`.
let mergePaths (``base``: string) input =
    sprintf "%s%c%s" (Path.GetDirectoryName ``base``) Path.DirectorySeparatorChar input

/// **Description**
///   * Reads and parses the XML from `path`.
///   * Goes through all the `ItemGroup` elements in search of `Compile` and `ProjectReference` elements.
///   * Returns an array of .fs file paths.
let readXml recurse path =
    let rec run set (Absolute path) =
        match set with
        | Contains path -> printfn "ignoring %s" path; [||]
        | _ ->
            let content = File.ReadAllText path
            let xml = Fsproj.Parse content

            // Get all the `<Compile Include="*" />` and `<None Inlcude="*" />` elements
            let files =
                xml.ItemGroups
                |> Array.collect (fun itemGroup ->
                    Array.concat [
                        itemGroup.Compiles |> Array.map (fun compile -> mergePaths path compile.Include)
                        itemGroup.Nones |> Array.map (fun none -> mergePaths path none.Include)
                    ])
                |> Array.map (|Absolute|)

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


// TODO: make this cleaner
let getIndexPosition index (content: string) =
    let mutable line = 1
    let mutable lineIndex = 0
    for i in 0 .. index - 1 do
        if content.[i] = '\n' then
            line <- line + 1
            lineIndex <- i
    line, index - lineIndex

/// **Description**
///   * Gets all the TODO comments within the file provided.
///   * Details:
///     * Reads the file.
///     * Looks for TODO patterns.
///     * Returns all until none matches anymore.
let getTodos (Absolute path) =
    let content = File.ReadAllText path
    let matches =
        Regex.Matches(
            input = content,
            pattern = @"(?<keyword>TODO|FIXME)\s*:\s*(?<comment>.+)$",
            options = (RegexOptions.Compiled ||| RegexOptions.Multiline))

    matches
    |> Seq.toArray
    |> Array.filter (fun ``match`` -> ``match``.Success)
    |> Array.mapSome (fun ``match`` ->
        if ``match``.Groups.["comment"].Success
        then Some (``match``.Groups.["comment"].Value, getIndexPosition ``match``.Groups.[0].Index content)
        else None)
    |> Array.map (fun (comment, (line, column)) -> {comment = comment; location = TodoLocation (path, line, column)})

/// **Description**
///   * Parses an fsproj file and returns an array of `Todo` objects.
///   * Details:
///     * Read through the XML of the fsproj
///     * If we find other project references *and* `Single` is set to false, we recursively call `parseFsproj` on those too.
let parseFsproj (results: ParseResults<Arguments>) = maybeOr [||] {
    let recurse = Option.isNone (results.TryGetResult Single)
    let! path = results.TryGetResult FilePath
    let! path = validate path
    let fsFiles = readXml recurse path
    let todos =
        fsFiles
        |> Array.collect getTodos
    return todos
}

[<EntryPoint>]
let main argv =
    let errorHandler =
        ProcessExiter(colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<Arguments>(programName = "todo-checker", errorHandler = errorHandler)
    let results = parser.ParseCommandLine argv

    match parseFsproj results with
    | [||] -> 0
    | todos ->
        // TODO: Clean up this color mess
        Console.ForegroundColor <- ConsoleColor.Red
        eprintfn "%i TODOs found:" (Array.length todos)
        for {location = (TodoLocation (Absolute path, line, column)); comment = comment} in todos do
            Console.ResetColor()
            eprintf "    [%s:%i:%i] " path line column
            Console.ForegroundColor <- ConsoleColor.Red
            eprintfn "%s" comment
        1
