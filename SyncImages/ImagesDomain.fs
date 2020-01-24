[<AutoOpen>]
module ImagesDomain

open ImageConversion

[<Literal>]
let jpg = "jpg"

type Size =
    | Original
    | Px2000
    | Px1000
    | Px400

    member this.numSuffix =
        match this with
        | Original -> None
        | Px2000 -> Some 2000
        | Px1000 -> Some 1000
        | Px400 -> Some 400

    static member parse =
        function
        | "" -> Original
        | "2000" -> Px2000
        | "1000" -> Px1000
        | "400" -> Px400
        | _ as str ->
            failwithf "'%s' doesn't match any of the allowed sizes" str

/// Unused for now
type RawExtension =
    | RAF

    static member parse =
        function
        | "RAF" | "raf" -> RAF
        | _ as str -> failwithf "%s is not a currently recognised raw image file extension" str


let makeSizeMap orig px2k px1k px4c =
    function
    | Original -> orig
    | Px2000 -> px2k
    | Px1000 -> px1k
    | Px400 -> px4c

let allSizes = [ Original; Px2000; Px1000; Px400 ]


type LocalImg =
    { LocalName : string // name without file extension, e.g. "DSC01234"
      Extension : string // e.g. "jpg" without the dot
      FullPath  : string } // to be used for uploads


type S3Image =
    { S3Name    : string // full S3 path, e.g. DSC01234/DSC01234-2000.jpg
      Size      : Size }



type ToUpload =
    | ToUpload of LocalImg * size : Size

    member this.stream =
        let (ToUpload (localImg,size)) = this
        resizeImg size.numSuffix localImg.FullPath

    member this.uploadStr =
        let (ToUpload (localImg,size)) = this
        let sizeSuffix =
            match size.numSuffix with
            | None -> ""
            | Some n -> sprintf "-%i" n
        sprintf "%s/%s%s.%s" localImg.LocalName localImg.LocalName sizeSuffix localImg.Extension


type UploadStatus =
    | Uploaded of s3Path : string
    | ToUpload of ToUpload


type SyncImage =
    { Name          : string
      UploadGetter  : Size -> UploadStatus }

