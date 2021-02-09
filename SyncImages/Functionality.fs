module Functionality

open System
open System.IO
open System.Security.Cryptography
open System.Text
open FSharp.Data
open DB
open UploadAndProcess
open ImageConversion


let private hashOfPath (TempFilePath path) =
    use fileStream = new FileStream(path, FileMode.Open)
    use md5 = MD5.Create()
    let hash =
        (StringBuilder(), md5.ComputeHash(fileStream))
        ||> Array.fold (fun sb b -> sb.Append(b.ToString("x2")))
        |> string

    hash


let uploadNewImageAndPutInDb slug title description path =
    async {
        let guid = Guid.NewGuid()
        let hash = hashOfPath path |> OrigImgHash
        let! dimensions = getImageDimensions path

        do! uploadAllRequiredImgs hash path
        return! addNewPhotoToDb guid hash (dimensions.Height, dimensions.Width) slug title description
    }


let editImage id titleOpt slugOpt descrOpt pathOpt =
    async {
        let! hashDimsOpt =
            match pathOpt with
            | Some path ->
                async {
                    let hash = hashOfPath path |> OrigImgHash
                    let! dimensions = getImageDimensions path
                    do! uploadAllRequiredImgs hash path
                    return Some (hash,dimensions) }
            | None -> Async.result None

        return! changePhotoFields id titleOpt slugOpt descrOpt hashDimsOpt
    }



let private netlifyTriggerUrl =
    "https://api.netlify.com/build_hooks/601d5105361c93149a828c74"

/// @TODO: actually use this to trigger changes – debounced though
let triggerNetlifyBuild () =
    Http.AsyncRequest(netlifyTriggerUrl, httpMethod="POST")
