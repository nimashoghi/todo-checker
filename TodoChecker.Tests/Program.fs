module TodoChecker.Tests

open System
open System.IO
open Expecto
open TodoChecker.Library

[<Tests>]
let tests =
    testList "all" [
        testList "validate" [
            test "validate false" {
                let fileExists _ = false
                let validate = validate' fileExists
                Expect.isNone (validate "") "validate false = None"
            }

            test "validate true" {
                let fileExists _ = true
                let validate = validate' fileExists
                let result = validate "test"
                Expect.isSome result "validate (true) = Some"
                Expect.equal result (Some "test") "validate (true) value check"
            }
        ]

        test "mergePaths" {
            let mergePaths = mergePaths'
            let currentFile = Path.Combine (__SOURCE_DIRECTORY__, __SOURCE_FILE__)
            Expect.equal (mergePaths' currentFile "test.txt") (Path.Combine (__SOURCE_DIRECTORY__, "test.txt")) "test.txt"
        }

        test "readXml" {
            let firstDir = @"C:\Dir\First.fsproj"
            let secondDir = @"C:\Dir\Second.fsproj"

            let first = """
                <Project Sdk="Microsoft.NET.Sdk">

                    <PropertyGroup>
                        <OutputType>Exe</OutputType>
                        <TargetFramework>netcoreapp2.2</TargetFramework>
                    </PropertyGroup>

                    <ItemGroup>
                        <Compile Include="Program.fs" />
                        <None Include="Second.fs" />
                    </ItemGroup>

                    <ItemGroup>
                        <ProjectReference Include=".\Second.fsproj" />
                    </ItemGroup>

                </Project>
            """

            let second = """
                <Project Sdk="Microsoft.NET.Sdk">

                    <PropertyGroup>
                        <OutputType>Library</OutputType>
                        <TargetFramework>netstandard2.0</TargetFramework>
                    </PropertyGroup>

                    <ItemGroup>
                        <Compile Include="SecondProgram.fs" />
                    </ItemGroup>

                </Project>
            """

            let readAllText =
                function
                | dir when dir = firstDir -> first
                | dir when dir = secondDir -> second
                | _ -> failwith "Invalid file"

            let (|Relative|_|) (path: string) =
                if not (Path.IsPathFullyQualified path) then Some (Relative path) else None

            let getFullPath =
                function
                | Relative path -> Uri(sprintf @"C:\Dir\%s" path).LocalPath
                | path -> path

            let validate =
                validate' <| fun path ->
                    try
                        readAllText (getFullPath path) |> ignore
                        true
                    with _ -> false
            let readXml = readXml' getFullPath readAllText validate

            Expect.equal (readXml true firstDir) [|
                @"C:\Dir\Program.fs"
                @"C:\Dir\Second.fs"
                @"C:\Dir\SecondProgram.fs"
            |] "recurse"

            Expect.equal (readXml false firstDir) [|
                @"C:\Dir\Program.fs"
                @"C:\Dir\Second.fs"
            |] "no recurse"
        }
    ]

[<EntryPoint>]
let main argv = runTestsInAssembly defaultConfig argv
