namespace AzurePipelines.TestLogger
{
    internal interface IEnvironmentVariableProvider
    {
        string GetEnvironmentVariable(string name);
    }
}