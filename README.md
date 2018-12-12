# AzurePipelines.TestLogger
Azure Pipelines logger extension for the [Visual Studio Test Platform](https://gtihub.com/microsoft/vstest).

## Usage
AzurePipelines.TestLogger can report test results automatically to the CI build.

1. Add a reference to the [AzurePipelines.TestLogger NuGet package](https://www.nuget.org/packages/AzurePipelines.TestLogger) in your test project
2. Use the following command when running tests
```
> dotnet test --test-adapter-path:. --logger:AzurePipelines
```
3. Test results are automatically reported to the Azure Pipelines CI results

## Credit

This project is based on [appveyor.testlogger](https://github.com/spekt/appveyor.testlogger) and [xunit](https://github.com/xunit/xunit/blob/master/src/xunit.runner.reporters/VstsReporter.cs).