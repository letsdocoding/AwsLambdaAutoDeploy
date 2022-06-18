using Amazon.S3;
using Amazon.S3.Model;
using System.Text.Json;

namespace AwsLambdaAutoDeploy.S3.Infrastructure
{
    internal class ManifestProvider : IManifestProvider
    {
        public async Task<Dictionary<string, string>> LoadLambdaManifests(SourceContext context, CancellationToken token = default)
        {
            var client = new AmazonS3Client();
            var manifestFileName = "manifest.json";
            GetObjectResponse manifest;
            try
            {
                manifest = await client.GetObjectAsync(context.SourceIdentifier,
                    manifestFileName);
            }
            catch (AmazonS3Exception)
            {
                //return empty
                return new Dictionary<string, string>();
            }


            await using Stream amazonStream = manifest.ResponseStream;
            var amazonStreamReader = new StreamReader(amazonStream);
            var data = await amazonStreamReader.ReadToEndAsync();
            if (string.IsNullOrEmpty(data))
            {
                return new Dictionary<string, string>();
            }

            Dictionary<string, string>? allManifests;
            try
            {
                allManifests = JsonSerializer.Deserialize<Dictionary<string, string>>(data);
            }
            catch
            {
                return new Dictionary<string, string>();
            }

            //would never be null in execution path
            return allManifests!;
        }
    }
}
