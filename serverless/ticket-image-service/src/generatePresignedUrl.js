const crypto = require("crypto");
const { S3Client, PutObjectCommand, GetObjectCommand } = require("@aws-sdk/client-s3");
const { getSignedUrl } = require("@aws-sdk/s3-request-presigner");

const {
  BUCKET_NAME,
  UPLOAD_URL_EXPIRATION_SECONDS = "300",
  AWS_REGION,
  AWS_DEFAULT_REGION,
} = process.env;

const region = AWS_REGION || AWS_DEFAULT_REGION || "ap-southeast-1";

const s3 = new S3Client({ region });

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Methods": "OPTIONS,POST",
  "Access-Control-Allow-Headers": "Content-Type,X-Amz-Date,Authorization,X-Api-Key,X-Amz-Security-Token",
};

const sanitizeFileName = (fileName = "") =>
  fileName
    .trim()
    .replace(/[^\w.\-]+/g, "-")
    .replace(/-+/g, "-");

const parseBody = (event) => {
  if (!event?.body) return {};
  if (typeof event.body === "string") {
    try {
      return JSON.parse(event.body);
    } catch (err) {
      throw new Error("Invalid JSON body");
    }
  }
  return event.body;
};

exports.handler = async (event) => {
  // Log the incoming event for debugging
  console.log("Received event:", JSON.stringify(event, null, 2));
  console.log("Event body type:", typeof event?.body);
  console.log("Event body value:", event?.body);

  if (event?.httpMethod === "OPTIONS") {
    return { statusCode: 204, headers: corsHeaders };
  }

  const bucket = BUCKET_NAME;
  if (!bucket) {
    return {
      statusCode: 500,
      headers: corsHeaders,
      body: JSON.stringify({ message: "BUCKET_NAME env var is not configured" }),
    };
  }

  let payload;
  try {
    payload = parseBody(event);
    console.log("Parsed payload:", JSON.stringify(payload, null, 2));
  } catch (err) {
    console.error("Failed to parse body:", err);
    return {
      statusCode: 400,
      headers: corsHeaders,
      body: JSON.stringify({ message: err.message }),
    };
  }

  const { ticketId, propertyId, fileName, contentType = "application/octet-stream", uploaderId } = payload;
  console.log("Extracted values - ticketId:", ticketId, "fileName:", fileName);

  if (!ticketId || !fileName) {
    console.error("Missing required fields - ticketId:", ticketId, "fileName:", fileName);
    return {
      statusCode: 400,
      headers: corsHeaders,
      body: JSON.stringify({ message: "ticketId and fileName are required" }),
    };
  }

  const safeFileName = sanitizeFileName(fileName);
  const objectKey = `tickets/${ticketId}/${crypto.randomUUID()}-${safeFileName}`;

  const expiresIn = Number.parseInt(UPLOAD_URL_EXPIRATION_SECONDS, 10) || 300;

  const command = new PutObjectCommand({
    Bucket: bucket,
    Key: objectKey,
    ContentType: contentType,
    Metadata: {
      ticketId,
      propertyId: propertyId ?? "",
      uploaderId: uploaderId ?? "",
    },
  });

  try {
    const uploadUrl = await getSignedUrl(s3, command, { expiresIn });
    
    // Also generate a presigned GET URL for viewing (longer expiration)
    const getCommand = new GetObjectCommand({
      Bucket: bucket,
      Key: objectKey,
    });
    const viewUrl = await getSignedUrl(s3, getCommand, { expiresIn: 3600 }); // 1 hour for viewing
    
    return {
      statusCode: 200,
      headers: corsHeaders,
      body: JSON.stringify({
        uploadUrl,
        viewUrl, // Add view URL for immediate viewing
        key: objectKey,
        bucket,
        expiresInSeconds: expiresIn,
      }),
    };
  } catch (err) {
    console.error("Failed to generate presigned URL", err);
    return {
      statusCode: 500,
      headers: corsHeaders,
      body: JSON.stringify({ message: "Failed to generate upload URL" }),
    };
  }
};

