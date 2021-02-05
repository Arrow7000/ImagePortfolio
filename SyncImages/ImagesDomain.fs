[<AutoOpen>]
module ImagesDomain

open ImageConversion
open S3Setup
open FSharp.Reflection

[<Literal>]
let jpg = "jpg"

type Size =
    | Px2000
    | Px1000
    | Px400

    member this.size =
        match this with
        | Px2000 -> 2000
        | Px1000 -> 1000
        | Px400 -> 400

    static member all =
        FSharpType.GetUnionCases(typeof<Size>)
        |> List.ofSeq
        |> List.map (fun c -> FSharpValue.MakeUnion(c, [||]) :?> Size)


type OrigOrSize =
    | Original
    | Size of Size

    member this.size =
        match this with 
        | Original -> None
        | Size size -> Some size.size

    static member parse =
        function
        | "" -> Original
        | "2000" -> Size Px2000
        | "1000" -> Size Px1000
        | "400" -> Size Px400
        | _ as str ->
            failwithf "'%s' doesn't match any of the allowed sizes" str

    static member blankMap =
            Size.all
            |> List.map (fun s -> Size s, ())
            |> Map.ofSeq
            |> Map.add Original ()

/// Unused for now
//type RawExtension =
//    | RAF

//    static member parse =
//        function
//        | "RAF" | "raf" -> RAF
//        | _ as str -> failwithf "%s is not a currently recognised raw image file extension" str


let makeSizeMap orig px2k px1k px4c =
    function
    | Original -> orig
    | Size size ->
        match size with
        | Px2000 -> px2k
        | Px1000 -> px1k
        | Px400 -> px4c



let s3Path name (size : OrigOrSize) ext =
    let sizeSuffix =
        match size.size with
        | None -> ""
        | Some n -> sprintf "-%i" n
    sprintf "%s/%s/%s%s.%s" imageDir name name sizeSuffix ext



type LocalImg =
    { LocalName : string // name without file extension, e.g. "DSC01234"
      Extension : string // e.g. "jpg" without the dot
      FullPath  : string // to be used for uploads
      Height    : int
      Width     : int }


type LocalAlbum =
    { Name      : string
      Images    : LocalImg list }

type LocalImgOrAlbum =
    | LocalImg of LocalImg
    | LocalAlbum of LocalAlbum


type S3Image =
    { S3Name        : string // full S3 path, e.g. DSC01234/DSC01234-2000.jpg
      LongestSize   : OrigOrSize }




type ToUpload =
    | ToUpload of LocalImg * longestSize : OrigOrSize

    member this.stream =
        let (ToUpload (localImg,size)) = this
        resizeImg size.size (TempFilePath localImg.FullPath)

    member this.s3Key =
        let (ToUpload (localImg,size)) = this
        s3Path localImg.LocalName size localImg.Extension


type UploadStatus =
    | Uploaded of s3Path : string
    | ToUpload of ToUpload


type SyncImage =
    { Name              : string
      UploadStatuses    : Map<OrigOrSize,UploadStatus> }
