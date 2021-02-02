module Functionality

open System
open System.IO
open System.Security.Cryptography
open System.Text
open DB
open UploadAndProcess


let private hashOfStream stream =
    use md5 = MD5.Create()
    let hash =
        (StringBuilder(), md5.ComputeHash(inputStream = stream))
        ||> Array.fold (fun sb b -> sb.Append(b.ToString("x2")))
        |> string

    stream.Seek(0L, SeekOrigin.Begin) |> ignore

    hash


let uploadNewImagesAndPutInDb slug title description stream =
    let guid = Guid.NewGuid()
    let id = guid.ToString()
    let hash = hashOfStream stream |> OrigImgHash

    uploadAllRequiredImgs hash stream
    |> Async.bind (fun _ -> addNewPhotoToDb id hash slug title description)

