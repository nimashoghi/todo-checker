module TodoChecker.CLI

open System
open System.IO
open Argu

open TodoChecker
open TodoChecker.Util

type Arguments =
| [<AltCommandLine "-s">] Single
| [<MainCommand; ExactlyOnce; Last>] FilePath of path: string
with
    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | FilePath _ -> "The path of the fsproj file for the current project."
            | Single -> "If not set, we recursively check fsproj project references."

[<EntryPoint>]
let main argv =
    let errorHandler =
        ProcessExiter(colorizer =
            function
            | ErrorCode.HelpText -> None
            | _ -> Some ConsoleColor.Red)
    let parser = ArgumentParser.Create<Arguments>(programName = "todo-checker", errorHandler = errorHandler)
    let results = parser.ParseCommandLine argv

    let recurse = Option.isNone (results.TryGetResult Single)
    let filePath = results.GetResult FilePath

    match parseFsproj recurse filePath with
    | [||] -> 0
    | todos ->
        // TODO: Clean up this color mess
        Console.ForegroundColor <- ConsoleColor.Red
        eprintfn "%i TODOs found:" (Array.length todos)
        for {location = (TodoLocation (Absolute Path.GetFullPath path, line, column)); comment = comment} in todos do
            Console.ResetColor()
            eprintf "    [%s:%i:%i] " path line column
            Console.ForegroundColor <- ConsoleColor.Red
            eprintfn "%s" comment
        Console.ResetColor()
        1
