module Server

open System
open System.IO
open Suave
open Suave.Filters
open Suave.Operators
open Api
open DB
open Functionality


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
                    let fileStream = new FileStream(img.tempFilePath, FileMode.Open)

                    let! photo =
                        uploadNewImagesAndPutInDb slug title descr fileStream

                    let (OrigImgHash hash) = photo.PhotoHash
                    let (S3Path s3Path) = getS3Path photo.PhotoHash Original

                    return!
                        Successful.OK (sprintf "photo %s with hash %s created and uploaded to %s 👍" photo.Id hash s3Path) ctx
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


let api =
    choose
        [ POST >=> path "/api/upload" >=> uploadImgEndpoint
          GET >=> pathScan "/api/photo/%s" getSinglePhotoEndpoint
          GET >=> path "/api/photos" >=> getAllPhotosEndpoint
          RequestErrors.NOT_FOUND "Path doesn't exist" ]
        >=> makeJson
