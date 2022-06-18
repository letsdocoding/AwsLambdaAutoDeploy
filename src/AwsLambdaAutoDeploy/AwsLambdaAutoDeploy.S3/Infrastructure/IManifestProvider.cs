namespace AwsLambdaAutoDeploy.S3.Infrastructure;

internal interface IManifestProvider
{
    Task<Dictionary<string, string>> LoadLambdaManifests(SourceContext context, CancellationToken token = default);
}