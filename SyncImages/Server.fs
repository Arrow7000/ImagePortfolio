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
open Auth
open CORS



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

                    triggerNetlifyBuild ()
                    |> Async.Ignore
                    |> Async.Start

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
    triggerNetlifyBuild ()
    |> Async.Ignore
    |> Async.Start

    deletePhoto id
    |> Async.bind (fun id -> Successful.OK id ctx)

/// Used to prevent the backend from going to sleep
let keepAlive = Successful.OK "Stayin' alive 🎶"



let api =
    choose
        [ POST >=> path "/auth/login" >=> logonHandler
          GET >=> path "/auth/check" >=> checkAuthState

          // Non-authed paths
          GET >=> pathScan "/api/photo/slug/%s" getSinglePhotoBySlugEndpoint
          GET >=> path "/api/photos" >=> getAllPhotosEndpoint
          OPTIONS >=> Successful.OK "CORS is good" // not sure if this is still needed
          GET >=> path "/api/stayalive" >=> keepAlive

          // Authed paths
          POST >=> path "/api/upload" >=> authRoute uploadImgEndpoint
          PATCH >=> path "/api/photos/reorder" >=> authRoute reorderPhotosEndpoint
          pathStarts "/api/photo/" // this ensures that not all unauthed paths stop at the first pathScan route
          >=> choose
            [ GET >=> authRoute (pathScan "/api/photo/%s" getSinglePhotoEndpoint)
              PATCH >=> authRoute (pathScan "/api/photo/%s" editPhotoEndpoint)
              DELETE >=> authRoute (pathScan "/api/photo/%s" deletePhotoEndpoint) ]

          // Fallbacks
          RequestErrors.NOT_FOUND "Path doesn't exist" ]
    >=> makeJson
    >=> logStructured (Targets.create LogLevel.Verbose [||]) logFormatStructured

