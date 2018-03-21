FormatTaskName "------- Executing Task: {0} -------"
Framework "4.6" #.NET framework version

properties {
    $build_config = "Debug"
    $verbosity = "normal" # q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]
    $build_artifacts = Join-Path $root "Artifacts"
    $test_logs = Join-Path $build_artifacts "TestLogs"
    $build_logs = Join-Path $build_artifacts "BuildLogs"
    $solution = Join-Path $root "..\Gravity\Gravity.sln"
	# MSBUILD VS 2017 - 'C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe'
	# MSBUILD VS2015 - C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe
	$msbuild = "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
}

task default -Depends LocalBuild
task LocalBuild -Depends Compile, IntegrationTest

task NuGetRestore -Description "Restore NuGet packages for the solution" {
	Write-Host "msbuild :  $msbuild"
    Write-Verbose "Solution :  $solution"
    exec { & $nuget_exe @('restore', $solution) }
}

task CompileInitialize -Description "Cleanup pre-existing build artifacts, if they exist" {
    InitializeDirectory $build_logs
}

task TestInitialize -Description "Cleanup pre-existing test artifacts, if they exist" {
    InitializeDirectory $test_logs
}

task Clean -Description "Clean the build artifacts and build tools" {
	#Uncomment this line if you need to delete the build and test logs
	#Remove-Item -Force -Recurse $build_artifacts -ErrorAction Ignore
	exec { & $msbuild @($solution,
					('/t:Clean'),
					('/property:Configuration=' + $build_config),
					('/verbosity:' + $verbosity))
		}
}

task Compile -Depends CompileInitialize, NuGetRestore -Description "Compile the solution" {
    Write-Verbose "Configuration: $build_config"
    Write-Verbose "Verbosity: $verbosity"

    # https://msdn.microsoft.com/en-us/library/ms164311.aspx
    exec { & $msbuild  @($solution,
            ("/property:Configuration=$build_config")
            ("/verbosity:$verbosity"),
            ('/nologo'),
            ('/maxcpucount'),
            ('/nodeReuse:false'),
            ('/distributedfilelogger'),
            ("/flp:LogFile=$build_logs\build.log"),
            ("/flp1:warningsonly;LogFile=$build_logs\buildwarnings.log"),
            ("/flp2:errorsonly;LogFile=$build_logs\builderrors.log"),
            ("/logger:StructuredLogger,$logger;$build_logs\structured.buildlog"))
    } -errorMessage "If Compile fails, check the registry HKEY_LOCAL_MACHINE -> SOFTWARE -->Microsoft --> MSBuild -->ToolsVersions -- > 14  --> MSBuildToolsPath and make sure it is set to the Visual Studio 2017 executable. Gravity runs on VS 2017! MSBuildToolsPath(VS2017) should be at C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\"
}

task UnitTest -Alias Test -Depends TestInitialize -Description "Run NUnit unit tests" {
    exec { & $nunit_exe $solution --where "class=~/^.+\.UnitTests\..+$/" --result="$test_logs\UnitTests.xml;format=nunit2" } -errorMessage "Unit tests failed!"
}

task IntegrationTest -Depends TestInitialize -Description "Run NUnit integration unit tests. " {
    $testDir = Join-Path $root "..\Gravity\Gravity.Test.Integration"
    Write-Host "Test directory is : $testDir"
    $configSource = "..\Gravity\Gravity.Test.Integration\App.config"
    Write-Host "configSource is : $configSource"
    $configDestination = Join-Path $root "..\Gravity\Gravity.Test.Integration\bin\Debug\Gravity.Test.Integration.dll.config"
    $testAssembly = Join-Path $root "..\Gravity\Gravity.Test.Integration\bin\Debug\Gravity.Test.Integration.dll"
    Write-Host "Test assembly : $testAssembly"

    Copy-Item $configSource $configDestination -Verbose:$VerbosePreference

    exec { & $nunit_exe $testAssembly --result="$test_logs\IntegrationTest.xml;format=nunit2" } -errorMessage "Integration tests failed!"
}
	
Function InitializeDirectory($directory) {
    If (Test-Path $directory) {
        Remove-Item -Force -Recurse $directory
    }
    New-Item -ItemType Directory -Force -Path $directory
}
