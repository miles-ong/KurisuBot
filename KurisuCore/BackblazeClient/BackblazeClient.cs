using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace KurisuCore.BackblazeClient
{
    public sealed class BackblazeClient
    {
        private readonly IAmazonS3 _client;
        private readonly string _bucketName;

        public BackblazeClient()
        {
            var credentials = new BasicAWSCredentials(BackblazeConfig.KeyId, BackblazeConfig.AppKey);

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{BackblazeConfig.Endpoint}",
                ForcePathStyle = true
            };

            _client = new AmazonS3Client(credentials, config);
            _bucketName = BackblazeConfig.BucketName;
        }

        public string GetUrl(string fileName)
            => $"https://images.kurisu-bot.com/{fileName}";

        public async Task UploadAsync(
            string fileName,
            Stream stream,
            string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = stream,
                ContentType = contentType
            };

            await _client.PutObjectAsync(request);
        }

        public async Task DeleteAsync(string fileName)
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };

            await _client.DeleteObjectAsync(request);
        }

        public async Task<bool> ExistsAsync(string fileName)
        {
            try
            {
                await _client.GetObjectMetadataAsync(_bucketName, fileName);

                return true;
            }
            catch (AmazonS3Exception ex)
                when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }
    }
}
