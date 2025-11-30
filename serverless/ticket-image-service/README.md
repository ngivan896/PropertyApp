## Ticket Image Service

Lambda handler that returns an S3 pre-signed URL for uploading maintenance ticket images. The function is designed to be invoked via Amazon API Gateway.

### Environment variables

| Name | Description | Example |
| ---- | ----------- | ------- |
| `BUCKET_NAME` | Target S3 bucket that stores ticket images | `property-ticket-images-dev` |
| `UPLOAD_URL_EXPIRATION_SECONDS` | (Optional) Pre-signed URL validity in seconds. Defaults to 300. | `300` |
| `AWS_REGION` / `AWS_DEFAULT_REGION` | Region for the S3 client. | `ap-southeast-1` |

### Handler

- File: `src/generatePresignedUrl.js`
- Handler: `generatePresignedUrl.handler`
- Runtime: `nodejs20.x` (or `nodejs18.x`)

### Deployment checklist

1. Install dependencies:
   ```bash
   cd serverless/ticket-image-service
   npm install
   ```
2. Package the Lambda source with `node_modules`, upload via your preferred tooling (SAM/Serverless Framework/Terraform/CDK).
3. In API Gateway, create a `POST /tickets/images/presigned-url` method with Lambda proxy integration.
4. Configure CORS on API Gateway or rely on the handler’s permissive headers during development.
5. Provide the API endpoint to `PropertyWeb`, which submits `ticketId`, `propertyId`, `fileName`, and `contentType` to obtain the upload URL.

### Request / Response contract

**Request body**

```json
{
  "ticketId": "TCK-12345",
  "propertyId": "PROP-21",
  "fileName": "door-damage.jpg",
  "contentType": "image/jpeg",
  "uploaderId": "USR-9"
}
```

**Response body**

```json
{
  "uploadUrl": "https://s3.amazonaws.com/...",
  "key": "tickets/TCK-12345/2ab1f83c-door-damage.jpg",
  "bucket": "property-ticket-images-dev",
  "expiresInSeconds": 300
}
```

Use the returned `uploadUrl` for a direct `PUT` from the browser. Persist the `key` in RDS to retrieve the image later.

