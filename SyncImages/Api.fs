module Api

open Microsoft.FSharp.Reflection
open FSharp.Json
open S3Setup

let s3Root = sprintf "https://%s.s3-ap-southeast-2.amazonaws.com/" bucketName


type SizeNum = string
type S3Path = string

type AvailableImage =
    { Name          : string
      Original      : string
      OtherSizes    : Map<SizeNum,S3Path> }

type Info =
    { Sizes     : int list
      Images    : AvailableImage list }



let allSizes =
    FSharpType.GetUnionCases(typeof<Size>)
    |> Array.map (fun case -> FSharpValue.MakeUnion(case, [| |]) :?> Size)
    |> List.ofArray


let sizeNums = allSizes |> List.choose (fun s -> s.size)


let makeAvailImg name =
    { AvailableImage.Name = name
      Original = s3Root + s3Path name Original jpg
      OtherSizes =
        allSizes
        |> List.choose
            (fun size ->
                size.size
                |> Option.map
                    (fun sizeNum ->
                        string sizeNum, s3Root + s3Path name size jpg))
        |> Map.ofList }

let makeInfo syncImgs =
    { Sizes = sizeNums
      Images =
        syncImgs
        |> List.map
            (fun {SyncImage.Name = name} -> makeAvailImg name) }



let config = JsonConfig.create(jsonFieldNaming = Json.lowerCamelCase)
let serialise o = Json.serializeEx config o
