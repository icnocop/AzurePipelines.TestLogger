# PipelinesTestLogger
Azure Pipelines logger extension for the [Visual Studio Test Platform](https://gtihub.com/microsoft/vstest).

## Usage
PipelinesTestLogger can report test results automatically to the CI build.

1. Add a reference to the [PipelinesTestLogger NuGet package](https://www.nuget.org/packages/PipelinesTestLogger) in your test project
2. Use the following command when running tests
```
> dotnet test --test-adapter-path:. --logger:PipelinesTestLogger
```
3. Test results are automatically reported to the Azure Pipelines CI results

## Credit

This project is based on [appveyor.testlogger](https://github.com/spekt/appveyor.testlogger) and [xunit](https://github.com/xunit/xunit/blob/master/src/xunit.runner.reporters/VstsReporter.cs).