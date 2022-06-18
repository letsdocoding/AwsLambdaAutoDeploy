using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using AwsLambdaAutoDeploy.S3.Infrastructure;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AwsLambdaAutoDeploy.S3;

public class Function
{
    IAmazonS3 S3Client { get; set; }
    private IManifestProvider _manifestProvider;
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
        _manifestProvider = new ManifestProvider();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
        S3Client = s3Client;
        _manifestProvider = new ManifestProvider();
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<string?> FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var s3Event = evnt.Records?[0].S3;
        if(s3Event == null)
        {
            return null;
        }

        
        try
        {
            var response = await S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
            IAmazonLambda lambda = new AmazonLambdaClient();
            var manifests = await _manifestProvider.LoadLambdaManifests(new SourceContext()
                { SourceIdentifier = s3Event.Bucket.Name });
            var a = s3Event.Object.Key.Split("/").ToList();

            if (a.Count > 0) 
                a.RemoveAt(a.Count - 1);
            var path = string.Join('/', a);
            if (!manifests.ContainsKey(path))
            {
                context.Logger.LogWarning($"For file: ${s3Event.Object.Key}, no deployment is defined in manifest");
                return null;
            }

            context.Logger.Log($"Updating {manifests[path]} with {s3Event.Bucket.Name}/{s3Event.Object.Key}");
            var result = await lambda.UpdateFunctionCodeAsync(new UpdateFunctionCodeRequest()
            {
                FunctionName = manifests[path],
                S3Key = s3Event.Object.Key,
                S3Bucket = s3Event.Bucket.Name,
                //S3ObjectVersion = version
            });
            return response.Headers.ContentType;
        }
        catch(Exception e)
        {
            context.Logger.LogInformation($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
            context.Logger.LogInformation(e.Message);
            context.Logger.LogInformation(e.StackTrace);
            throw;
        }
    }
}