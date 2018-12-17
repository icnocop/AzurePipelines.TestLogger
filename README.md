# AzurePipelines.TestLogger

Azure Pipelines logger extension for the [Visual Studio Test Platform](https://gtihub.com/microsoft/vstest).

## Why Do I Need This?

This logger extensions allows you to send test results from a `dotnet test` session directly to Azure Pipelines in real-time as the tests are executed. It also talks directly to the Azure DevOps REST API and as a result can better represent your tests using Azure Pipelines conventions over other post-processing methods such as logging to a TRX file and processing with the `PublishTestResults` Azure Pipelines task.

## Usage

In order for the logger to authenticate against the Azure DevOps API you'll need to [expose the access token via an environment variable](https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=vsts&tabs=yaml%2Cbatch#systemaccesstoken) in your Azure Pipelines `.yml` file:

```
    env:
      SYSTEM_ACCESSTOKEN: $(System.AccessToken)
```

### Installing Into Your Project

* Add a reference to the [AzurePipelines.TestLogger NuGet package](https://www.nuget.org/packages/AzurePipelines.TestLogger) in your test project
* Use the following command when running tests
```
> dotnet test --test-adapter-path:. --logger:AzurePipelines
```
* Test results are automatically reported to the Azure Pipelines CI results

### Using Cake

An alternative to installing the logger directly into your test project is installing it as a tool in Cake:

```
#tool "AzurePipelines.TestLogger&version=1.0.0"
```

Then you can specify the logger during test runs when running on your CI server (the following is example code, your Cake build script may look or behave a little differently):

```
Task("Test")
    .Description("Runs all tests.")
    .IsDependentOn("Build")
    .DoesForEach(GetFiles("./tests/*Tests/*.csproj"), project =>
    {
        DotNetCoreTestSettings testSettings = new DotNetCoreTestSettings()
        {
            NoBuild = true,
            NoRestore = true,
            Configuration = configuration
        };
        if (isRunningOnBuildServer)
        {
            testSettings.Filter = "TestCategory!=ExcludeFromBuildServer";
            testSettings.Logger = "AzurePipelines";
            testSettings.TestAdapterPath = GetDirectories($"./tools/AzurePipelines.TestLogger.*/contentFiles/any/any").First();
        }

        Information($"Running tests in {project}");
        DotNetCoreTest(MakeAbsolute(project).ToString(), testSettings);
    })
    .DeferOnError();
```

## Credit

This project is based on [appveyor.testlogger](https://github.com/spekt/appveyor.testlogger) and [xunit](https://github.com/xunit/xunit/blob/master/src/xunit.runner.reporters/VstsReporter.cs).