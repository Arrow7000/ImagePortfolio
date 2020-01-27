module S3Setup

open Amazon
open Amazon.Runtime
open Amazon.S3

let private accessKey = "AKIA5IHOEO5RL4BYFZUC"
let private secretKey = "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"
let private credentials = new BasicAWSCredentials(accessKey, secretKey);
let client = new AmazonS3Client(credentials, RegionEndpoint.APSoutheast2)
let bucketName = "aron-photo-portfolio"

[<Literal>]
let imageDir = "photos"
let jsonDir = "metadata"
