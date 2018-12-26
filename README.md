# TODO Checker <!-- omit in toc -->

### A small program that checks all your csproj/fsproj/psproj files during build time and prints all pending "TODO" notes. <!-- omit in toc -->

## Table of Contents <!-- omit in toc -->

- [Demo](#demo)
- [Getting Started](#getting-started)
- [Command Line Arguments](#command-line-arguments)
- [Building](#building)
- [Running Tests](#running-tests)

### Demo

![Demo](https://i.imgur.com/mfo67Yy.gif)

### Getting Started

1. Get the latest version from the [releases](https://) page.
2. Execute the program: `todo-checker .\path\to\project.fsproj`

### Command Line Arguments

    USAGE: todo-checker [--help] [--single] <path>

    FILEPATH:

        <path>                The path of the fsproj file for the current project.

    OPTIONS:

        --single, -s          If not set, we recursively check fsproj project references.
        --help                display this list of options.

### Building

1. Install [PowerShell](https://github.com/PowerShell/PowerShell) and [PSake](https://github.com/psake/psake)
2. Clone this repository and `cd` to that directory.
3. Run `Invoke-psake build` for a basic build and `Invoke-psake publish` for generating a release executable.

### Running Tests

1. Install PowerShell and PSake from the [Building](#building) section.
2. Run `Invoke-psake test`.
