[<AutoOpen>]
module NewHelpers

open S3Setup

/// Temp path to uploaded file
//type TempFilePath = TempFilePath of string

/// S3 path, e.g. photos/abcdef/abcdef-1000.jpg
type S3Path = S3Path of string

/// Full URL to image, including CDN root
type ImageUrl = ImageURL of string

/// Hash of the original photo file
type OrigImgHash = OrigImgHash of string

type OrigOrSizeNew =
    | Original
    | Size of int

type FileSize = FileSize of int64


let jpg = "jpg"


let makeOrigImgName name ext = sprintf "%s/%s/%s.%s" imageDir name name ext |> S3Path

let makeImgName name size ext = sprintf "%s/%s/%s-%i.%s" imageDir name name size ext |> S3Path


let getS3Path (OrigImgHash origHash) (origOrSize : OrigOrSizeNew) =
    let name = string origHash
    
    match origOrSize with
    | Original -> makeOrigImgName name jpg
    | Size size -> makeImgName name size jpg



let getImageUrl origHash origOrSize =
    let (S3Path s3Path) = getS3Path origHash origOrSize
    cdnRoot + s3Path |> ImageURL
