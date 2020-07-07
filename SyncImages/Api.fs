module Api

open FSharp.Json
open S3Setup
open Amazon.S3.Model
open ImageConversion

type SizeNum = string
type S3Path = string

type Orientation =
    | Square
    | Landscape
    | Portrait


let getOrientation w h =
  if w = h then Square
  else if w > h then Landscape
  else Portrait


type SizedImage =
    { Path      : string
      Width     : int
      Height    : int }

type AvailableImage =
    { Name          : string
      OriginalPath  : string
      Orientation   : Orientation
      OtherSizes    : SizedImage list }


type Album =
    { Name      : string
      Images    : AvailableImage list }


type ImageOrAlbum =
    | Image of AvailableImage
    | Album of Album


type Info = { ImagesAndAlbums : ImageOrAlbum list }



let makeAvailImg { LocalName = name; Height = height; Width = width } =
    { AvailableImage.Name = name
      OriginalPath = cdnRoot + s3Path name Original jpg
      Orientation = getOrientation width height
      OtherSizes =
        Size.all
        |> List.map
            (fun size ->
                let (w,h) = scaleMaxTo size.size (width, height)

                let path = cdnRoot + s3Path name (Size size) jpg
                { Path = path
                  Width = w
                  Height = h }) }


let makeInfo localImgsAndAlbums =
    { ImagesAndAlbums =
        localImgsAndAlbums
        |> List.map
            (function
                | LocalImg img ->
                    Image (makeAvailImg img)
                | LocalAlbum { Name = name; Images = imgs } ->
                    Album { Name = name; Images = List.map makeAvailImg imgs }) }




let config = JsonConfig.create(jsonFieldNaming = Json.lowerCamelCase)
let serialise o = Json.serializeEx config o


let uploadJson key content =
    let req = new PutObjectRequest()
    req.BucketName <- bucketName
    req.Key <- key
    req.ContentBody <- content
    req.ContentType <- "application/json"

    client.PutObjectAsync req
    |> Async.AwaitTask


let uploadMetadata = uploadJson (sprintf "%s/metadata.json" jsonDir)
