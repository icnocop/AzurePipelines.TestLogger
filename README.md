Azure Pipelines logger extension for the [Visual Studio Test Platform](https://github.com/microsoft/vstest).

**NuGet**
* [AzurePipelines.TestLogger](https://www.nuget.org/packages/AzurePipelines.TestLogger)

---

## What Is It?

This logger extensions allows you to send test results from a `dotnet test` session directly to Azure Pipelines in real-time as the tests are executed. It also talks directly to the Azure DevOps REST API and as a result can better represent your tests using Azure Pipelines conventions over other post-processing methods such as logging to a TRX file and processing with the `PublishTestResults` Azure Pipelines task.

## Usage

In order for the logger to optionally authenticate against the Azure DevOps API using an access token you'll need to [expose the access token via an environment variable](https://docs.microsoft.com/en-us/azure/devops/pipelines/process/variables?view=vsts&tabs=yaml%2Cbatch#systemaccesstoken) in your Azure Pipelines `.yml` file:

```
    env:
      SYSTEM_ACCESSTOKEN: $(System.AccessToken)
```

### Parameters

| Key                         | Possible Values                                  | Required | Default Value | Description                                                                                                        |
|-----------------------------|:------------------------------------------------:|:--------:|:-------------:|--------------------------------------------------------------------------------------------------------------------|
| Verbose                     | True<br>False                                    | False    | False         | Indicates whether or not to output verbose information to the console.                                             |
| UseDefaultCredentials       | True<br>False                                    | False    | False         | Indicates whether or not to use default credentials to authenticate against the Azure DevOps API.                  |
| ApiVersion                  | \{Major}.\{Minor}[-preview[.\{ResourceVersion}]] | False    | 5.0           | The value passed to the `api-version` parameter in the query string when communicating with the Azure DevOps API. |
| GroupTestResultsByClassName | True<br>False                                    | False    | True          | Indicates whether or not to group test results by their class name.                                                |

Pass parameters to the logger using the following command line syntax:  
`--logger AzurePipelines;Verbose=true;UseDefaultCredentials=true`

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

## Limitations

Note that right now, the Azure Pipelines test summary will only show statistics for top-level tests. That's not ideal for a logger that nests results like this one, but the clarity of grouping tests under their fixture is more valuable than listing a correct total in the test summary in my opinion. Thankfully the pass/fail will still "bubble up" so even though the summary may show fewer tests than actually exist, it'll still correctly indicate if any tests are failing (which would then require a drill-down to figure out which ones are failing).

[There's an open feature suggestion here for showing all nested tests in the summary](https://developercommunity.visualstudio.com/content/idea/409015/show-all-tests-in-the-hierarchy-in-test-summary.html).

## Credit

This project is based on [appveyor.testlogger](https://github.com/spekt/appveyor.testlogger) and [xunit](https://github.com/xunit/xunit/blob/master/src/xunit.runner.reporters/VstsReporter.cs).
