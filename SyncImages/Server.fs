module Server

open System
open System.IO
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Logging
open ImageConversion
open Api
open DB
open Functionality


type EditPhotoDto =
    { Title : string option
      Slug : string option
      Description : string option }


let makeJson = Writers.setMimeType "application/json"


let uploadImgEndpoint =
    request
        (fun req ctx ->
            let imgOpt = List.tryFind (fun file -> file.fieldName = "image") req.files
            let slugOpt = req.Item "slug"
            let titleOpt = req.Item "title"
            let descriptionOpt = req.Item "description"

            match imgOpt, slugOpt, titleOpt, descriptionOpt with
            | Some img, Some slug, Some title, Some descr ->
                async {
                    let! photo =
                        uploadNewImageAndPutInDb slug title descr (TempFilePath img.tempFilePath)

                    let! sizes = getSizes ()
                    
                    return!
                        (makeFullPhoto sizes photo
                         |> serialise
                         |> Successful.OK) ctx
                }
            | _ ->
                RequestErrors.NOT_FOUND (sprintf "Not all required fields are attached. Required fields are image, slug, title, description") ctx)


let getSinglePhotoEndpoint id ctx =
    async {
        let! sizes = getSizes ()
        let! photo = getSinglePhoto id
        return!
            (makeFullPhoto sizes photo
             |> serialise
             |> Successful.OK) ctx
    }


let getSinglePhotoBySlugEndpoint slug ctx =
    async {
        let! sizes = getSizes ()
        let! photoResult = getSinglePhotoBySlug slug
        match photoResult with
        | Ok photo ->
            return!
                (makeFullPhoto sizes photo
                 |> serialise
                 |> Successful.OK) ctx
        | Result.Error _ ->
            return!
                RequestErrors.NOT_FOUND (sprintf "Couldn't find photo with slug '%s'" slug) ctx
    }


let getAllPhotosEndpoint ctx =
    async {
        let! sizes = getSizes ()
        let! photos = getAllPhotos ()
        return!
            (photos
             |> List.map (makeFullPhoto sizes)
             |> serialise
             |> Successful.OK) ctx
    }



let editPhotoEndpoint id (ctx : HttpContext) =
    async {
        let req = ctx.request
        let! sizes = getSizes ()

        let imgOpt =
            List.tryFind (fun file -> file.fieldName = "image") req.files
            |> Option.map (fun upload -> TempFilePath upload.tempFilePath)

        let slugOpt = req.Item "slug"
        let titleOpt = req.Item "title"
        let descriptionOpt = req.Item "description"

        let! photo = editImage id titleOpt slugOpt descriptionOpt imgOpt
        return!
            (makeFullPhoto sizes photo
             |> serialise
             |> Successful.OK) ctx
    }

let reorderPhotosEndpoint (ctx : HttpContext) =
    async {
        let req = ctx.request
        let! sizes = getSizes ()

        let orderedIds =
            req.multiPartFields
            |> List.sortBy (snd >> int)
            |> List.map fst

        let! photos = reorderPhotos orderedIds

        return!
            (photos
             |> List.map (makeFullPhoto sizes)
             |> serialise
             |> Successful.OK) ctx
    }


let deletePhotoEndpoint id ctx =
    deletePhoto id
    |> Async.bind (fun id -> Successful.OK id ctx)

/// Used to prevent the backend from going to sleep
let keepAlive = Successful.OK "Stayin' alive 🎶"


let allowCors =
    Writers.addHeader "Access-Control-Allow-Origin" "*"
    >=> Writers.addHeader "Access-Control-Allow-Methods" "*"

let api =
    allowCors
    >=> choose
        [ POST >=> path "/api/upload" >=> uploadImgEndpoint
          GET >=> pathScan "/api/photo/slug/%s" getSinglePhotoBySlugEndpoint
          GET >=> pathScan "/api/photo/%s" getSinglePhotoEndpoint
          PATCH >=> pathScan "/api/photo/%s" editPhotoEndpoint
          GET >=> path "/api/photos" >=> getAllPhotosEndpoint
          PATCH >=> path "/api/photos/reorder" >=> reorderPhotosEndpoint
          DELETE >=> pathScan "/api/photo/%s" deletePhotoEndpoint
          GET >=> path "/api/stayalive" >=> keepAlive
          OPTIONS >=> Successful.OK "CORS is good"
          RequestErrors.NOT_FOUND "Path doesn't exist" ]
    >=> makeJson
    >=> logStructured (Targets.create LogLevel.Verbose [||]) logFormatStructured

