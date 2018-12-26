function Invoke-Project {
    param (
        [Parameter(Mandatory)]
        [string]
        $Path,

        [Parameter(Mandatory)]
        $Command

    )

    begin {
        Push-Location $Path
    } process {
        $Command.Invoke()
    } end {
        Pop-Location
    }
}

function Clear-Project {
    param (
        [Parameter(Mandatory)]
        [string]
        $Path
    )

    Invoke-Project -Path $Path -Command {
        Remove-Item -Recurse -Force -ErrorAction Ignore -Path "obj"
        Remove-Item -Recurse -Force -ErrorAction Ignore -Path "bin"
        dotnet.exe clean
    }
}

function Restore-Project {
    param (
        [Parameter(Mandatory)]
        [string]
        $Path
    )

    Invoke-Project -Path $Path -Command {
        dotnet.exe restore
    }
}

function Build-Project {
    param (
        [Parameter(Mandatory)]
        [string]
        $Path
    )

    Invoke-Project -Path $Path -Command {
        dotnet.exe build
    }
}

task clean -depends clean-all
task clean-all -depends clean-library, clean-cli, clean-tests

task clean-library {
    Clear-Project "$PSScriptRoot"
}

task clean-cli {
    Clear-Project "$PSScriptRoot\TodoChecker.CLI"
}

task clean-tests {
    Clear-Project "$PSScriptRoot\TodoChecker.Tests"
}

task restore -depends restore-all
task restore-all -depends restore-library, restore-cli, restore-tests

task restore-library {
    Restore-Project "$PSScriptRoot"
}

task restore-cli {
    Restore-Project "$PSScriptRoot\TodoChecker.CLI"
}

task restore-tests {
    Restore-Project "$PSScriptRoot\TodoChecker.Tests"
}

task build -depends build-all
task build-all -depends build-library, build-cli, build-tests

task build-library -depends restore-library {
    Build-Project "$PSScriptRoot"
}

task build-cli -depends restore-cli {
    Build-Project "$PSScriptRoot\TodoChecker.CLI"
}

task build-tests -depends restore-tests {
    Build-Project "$PSScriptRoot\TodoChecker.Tests"
}

task test -depends run-tests
task run-tests -depends restore-tests {
    Invoke-Project -Path "$PSScriptRoot\TodoChecker.Tests" -Command {
        dotnet.exe run
    }
}
