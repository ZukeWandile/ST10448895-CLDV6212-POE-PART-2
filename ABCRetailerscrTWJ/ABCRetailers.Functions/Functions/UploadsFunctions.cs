using ABCRetailers.Functions.Helpers;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;

namespace ABCRetailers.Functions.Functions;

// Azure Function for handling proof-of-payment uploads
public class UploadsFunctions
{
    private readonly string _conn;      // Storage connection string
    private readonly string _proofs;    // Blob container name for payment proofs
    private readonly string _share;     // Azure File Share name
    private readonly string _shareDir;  // Subdirectory in file share

    // Load config values from local.settings.json
    public UploadsFunctions(IConfiguration cfg)
    {
        _conn = "DefaultEndpointsProtocol=https;AccountName=part2stuff;AccountKey=JkUrOV2PdqXiRQSX92ujDoKpGywMwlvdIdfuQsCt2exH4vvEVGB5LjSArdDoEPZgaalHurdjkMn2+ASt4i8vHg==;EndpointSuffix=core.windows.net";
        _proofs = cfg["BLOB_PAYMENT_PROOFS"] ?? "payment-proofs";
        _share = cfg["FILESHARE_CONTRACTS"] ?? "contracts";
        _shareDir = cfg["FILESHARE_DIR_PAYMENTS"] ?? "payments";
    }

    // POST /api/uploads/proof-of-payment — handles file upload
    [Function("Uploads_ProofOfPayment")]
    public async Task<HttpResponseData> Proof(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "uploads/proof-of-payment")] HttpRequestData req)
    {
        // Check for multipart/form-data
        var contentType = req.Headers.TryGetValues("Content-Type", out var ct) ? ct.First() : "";
        if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            return HttpJson.Bad(req, "Expected multipart/form-data");

        // Parse form data
        var form = await MultipartHelper.ParseAsync(req.Body, contentType);
        var file = form.Files.FirstOrDefault(f => f.FieldName == "ProofOfPayment");
        if (file is null || file.Data.Length == 0)
            return HttpJson.Bad(req, "ProofOfPayment file is required");

        var orderId = form.Text.GetValueOrDefault("OrderId");
        var customerName = form.Text.GetValueOrDefault("CustomerName");

        // Upload to Blob Storage
        var container = new BlobContainerClient(_conn, _proofs);
        await container.CreateIfNotExistsAsync();
        var blobName = $"{Guid.NewGuid():N}-{file.FileName}";
        var blob = container.GetBlobClient(blobName);
        await using (var s = file.Data) await blob.UploadAsync(s);

        // Write metadata to Azure File Share
        var share = new ShareClient(_conn, _share);
        await share.CreateIfNotExistsAsync();
        var root = share.GetRootDirectoryClient();
        var dir = root.GetSubdirectoryClient(_shareDir);
        await dir.CreateIfNotExistsAsync();

        var fileClient = dir.GetFileClient(blobName + ".txt");
        var meta = $"UploadedAtUtc: {DateTimeOffset.UtcNow:O}\nOrderId: {orderId}\nCustomerName: {customerName}\nBlobUrl: {blob.Uri}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(meta);
        using var ms = new MemoryStream(bytes);
        await fileClient.CreateAsync(ms.Length);
        await fileClient.UploadAsync(ms);

        // Return success response with blob info
        return HttpJson.Ok(req, new { fileName = blobName, blobUrl = blob.Uri.ToString() });
    }
}