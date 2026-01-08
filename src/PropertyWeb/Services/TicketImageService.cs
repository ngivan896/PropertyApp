using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PropertyWeb.Services;

public class TicketImageApiOptions
{
    public string PresignEndpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string ApiKeyHeader { get; set; } = "x-api-key";
    public string? BucketName { get; set; }
    public string? Region { get; set; }
}

public record TicketImageUploadRequest(
    Guid TicketId,
    Guid PropertyId,
    string FileName,
    string ContentType,
    Guid UploaderId);

public record TicketImageUploadResponse(
    string UploadUrl,
    string Key,
    string Bucket,
    int ExpiresInSeconds);

public interface ITicketImageService
{
    Task<TicketImageUploadResponse?> RequestUploadUrlAsync(
        TicketImageUploadRequest request,
        CancellationToken cancellationToken = default);
    
    string? GetPresignedViewUrl(string? imageUrl, int expiresInSeconds = 3600);
}

public class TicketImageService : ITicketImageService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private const int DefaultExpirationSeconds = 300;

    private readonly HttpClient? _httpClient;
    private readonly TicketImageApiOptions _options;
    private readonly ILogger<TicketImageService> _logger;
    private readonly IAmazonS3? _s3Client;

    public TicketImageService(
        HttpClient? httpClient,
        IOptions<TicketImageApiOptions> options,
        ILogger<TicketImageService> logger,
        IAmazonS3? s3Client = null)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _s3Client = s3Client;
        
        // Log initialization status for debugging
        if (_s3Client == null)
        {
            _logger.LogWarning("TicketImageService initialized without S3 Client. BucketName: {BucketName}", _options.BucketName);
        }
        else
        {
            _logger.LogInformation("TicketImageService initialized with S3 Client. BucketName: {BucketName}", _options.BucketName);
        }
    }

    public async Task<TicketImageUploadResponse?> RequestUploadUrlAsync(
        TicketImageUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        // 优先使用API Gateway方式（符合Part 2 serverless架构要求）
        if (!string.IsNullOrWhiteSpace(_options.PresignEndpoint) && _httpClient != null)
        {
            var result = await RequestPresignedUrlFromApiAsync(request, cancellationToken);
            if (result != null)
            {
                return result;
            }
            _logger.LogWarning("API Gateway request failed, falling back to direct S3 mode");
        }

        // 回退到直接S3方式（临时方案，当API Gateway不可用时）
        if (!string.IsNullOrWhiteSpace(_options.BucketName) && _s3Client != null)
        {
            return await GeneratePresignedUrlDirectlyAsync(request, cancellationToken);
        }

        _logger.LogWarning("TicketImageService: Neither API Gateway endpoint nor S3 bucket is configured.");
        return null;
    }

    private Task<TicketImageUploadResponse?> GeneratePresignedUrlDirectlyAsync(
        TicketImageUploadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var bucketName = _options.BucketName!;
            var sanitizedFileName = SanitizeFileName(request.FileName);
            var objectKey = $"tickets/{request.TicketId}/{Guid.NewGuid()}-{sanitizedFileName}";

            var presignRequest = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddSeconds(DefaultExpirationSeconds),
                ContentType = request.ContentType ?? "application/octet-stream"
            };

            var uploadUrl = _s3Client!.GetPreSignedURL(presignRequest);
            
            return Task.FromResult<TicketImageUploadResponse?>(
                new TicketImageUploadResponse(
                    uploadUrl,
                    objectKey,
                    bucketName,
                    DefaultExpirationSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL directly from S3");
            return Task.FromResult<TicketImageUploadResponse?>(null);
        }
    }

    private async Task<TicketImageUploadResponse?> RequestPresignedUrlFromApiAsync(
        TicketImageUploadRequest request,
        CancellationToken cancellationToken)
    {
        // Serialize request with camelCase property names to match Lambda function expectations
        var requestJson = JsonSerializer.Serialize(new
        {
            ticketId = request.TicketId.ToString(),
            propertyId = request.PropertyId.ToString(),
            fileName = request.FileName,
            contentType = request.ContentType,
            uploaderId = request.UploaderId.ToString()
        }, SerializerOptions);
        
        _logger.LogWarning("Sending request to API Gateway: {RequestJson}", requestJson);
        _logger.LogWarning("Request details - TicketId: {TicketId}, FileName: {FileName}", 
            request.TicketId, request.FileName);
        
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.PresignEndpoint)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            var header = string.IsNullOrWhiteSpace(_options.ApiKeyHeader) ? "x-api-key" : _options.ApiKeyHeader;
            httpRequest.Headers.TryAddWithoutValidation(header, _options.ApiKey);
        }

        using var response = await _httpClient!.SendAsync(httpRequest, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "TicketImageService: Presign endpoint returned {StatusCode}",
                (int)response.StatusCode);
            return null;
        }

        // Lambda Proxy Integration returns: { "statusCode": 200, "body": "{\"uploadUrl\":\"...\"}" }
        // Need to parse the outer response first, then parse the body string
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogInformation("Raw API Gateway response: {Response}", responseText);
        
        try
        {
            // Try to parse as Lambda Proxy Integration response
            using var outerDoc = JsonDocument.Parse(responseText);
            
            // Log the structure
            _logger.LogInformation("Response structure: {Structure}", outerDoc.RootElement.ToString());
            
            if (outerDoc.RootElement.TryGetProperty("body", out var bodyElement))
            {
                // Body is a JSON string, parse it
                var bodyString = bodyElement.GetString();
                _logger.LogInformation("Body string: {BodyString}", bodyString);
                
                if (!string.IsNullOrWhiteSpace(bodyString))
                {
                    var proxyResult = JsonSerializer.Deserialize<TicketImageUploadResponse>(bodyString, SerializerOptions);
                    _logger.LogInformation("Parsed result - UploadUrl: {UploadUrl}, Key: {Key}, Bucket: {Bucket}", 
                        proxyResult?.UploadUrl, proxyResult?.Key, proxyResult?.Bucket);
                    return proxyResult;
                }
            }
            
            // If not Lambda Proxy format, try direct deserialization
            _logger.LogInformation("Trying direct deserialization");
            var directResult = JsonSerializer.Deserialize<TicketImageUploadResponse>(responseText, SerializerOptions);
            _logger.LogInformation("Direct deserialization result - UploadUrl: {UploadUrl}, Key: {Key}, Bucket: {Bucket}", 
                directResult?.UploadUrl, directResult?.Key, directResult?.Bucket);
            return directResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse API Gateway response. Status: {StatusCode}, Response: {Response}", 
                (int)response.StatusCode, responseText);
            return null;
        }
    }

    public string? GetPresignedViewUrl(string? imageUrl, int expiresInSeconds = 3600)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return imageUrl;
        }

        // If the URL already contains query parameters (presigned URL), return as-is
        if (imageUrl.Contains("?") && (imageUrl.Contains("X-Amz-") || imageUrl.Contains("AWSAccessKeyId")))
        {
            _logger.LogInformation("URL is already a presigned URL, returning as-is");
            return imageUrl;
        }

        if (_s3Client == null || string.IsNullOrWhiteSpace(_options.BucketName))
        {
            _logger.LogError("S3 client or bucket name not configured. S3Client is null: {IsNull}, BucketName: {BucketName}", 
                _s3Client == null, _options.BucketName);
            _logger.LogWarning("Returning original URL, which may cause AccessDenied errors");
            return imageUrl; // Return original URL if we can't generate presigned URL
        }

        try
        {
            // Extract key from URL
            // URL format: https://bucket.s3.region.amazonaws.com/key
            // or: https://bucket.s3.amazonaws.com/key
            var key = ExtractKeyFromUrl(imageUrl, _options.BucketName);
            if (string.IsNullOrWhiteSpace(key))
            {
                _logger.LogWarning("Could not extract key from URL: {ImageUrl}", imageUrl);
                return imageUrl; // Return original if we can't extract key
            }

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds)
            };

            var presignedUrl = _s3Client.GetPreSignedURL(request);
            _logger.LogInformation("Successfully generated presigned view URL for key: {Key}, expires in {ExpiresIn} seconds", 
                key, expiresInSeconds);
            return presignedUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned view URL for {ImageUrl}, returning original URL", imageUrl);
            return imageUrl; // Return original URL on error
        }
    }

    private static string? ExtractKeyFromUrl(string url, string bucketName)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        // Try to extract key from various S3 URL formats
        var patterns = new[]
        {
            $@"https://{bucketName}\.s3\.[^/]+/(.+)",
            $@"https://{bucketName}\.s3\.amazonaws\.com/(.+)",
            $@"https://s3\.[^/]+/{bucketName}/(.+)",
            $@"https://s3\.amazonaws\.com/{bucketName}/(.+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(url, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        // If URL doesn't match patterns, assume it's already a key
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            return url;
        }

        return null;
    }

    private static string SanitizeFileName(string fileName)
    {
        return fileName
            .Trim()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace("/", "-")
            .Replace("\\", "-");
    }
}

