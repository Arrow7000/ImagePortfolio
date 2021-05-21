module S3Setup

open Amazon
open Amazon.Runtime
open Amazon.S3

let private accessKey = Env.varRequired "ACCESS_KEY"
let private secretKey = Env.varRequired "SECRET_KEY"
let private credentials = new BasicAWSCredentials(accessKey, secretKey);
let client = new AmazonS3Client(credentials, RegionEndpoint.EUWest2)
let bucketName = Env.varRequired "BUCKET_NAME"

[<Literal>]
let imageDir = "photos"
let jsonDir = "metadata"

/// CDN root URL with trailing slash
let cdnRoot = "https://d3ltknfikz7r4w.cloudfront.net/"
