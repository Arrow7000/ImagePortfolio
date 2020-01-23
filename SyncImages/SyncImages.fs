module SyncImages

open LocalImages



let groupS3Imgs s3Imgs =
    List.groupBy (fun {S3Name = name} -> name) s3Imgs
    |> Map.ofList
    |> Map.map
        (fun _ allSizes ->
            allSizes
            |> List.groupBy (fun {Size=size} -> size)
            |> List.choose
                (fun (size,imgs) ->
                    match imgs with
                    | [] -> None
                    | [ one ] -> Some (size, one)
                    | _ -> failwithf "Each image should at most have one S3 entry for each size")
            |> Map.ofList)



let getSyncImgs localImgs s3Imgs =
    let groupedS3Imgs = groupS3Imgs s3Imgs

    localImgs
    |> List.map
        (fun localImg ->
                let { LocalName = localName } = localImg

                match Map.tryFind localName groupedS3Imgs with
                | Some sizeMap ->
                    let tryFindInMap size =
                        match Map.tryFind size sizeMap with
                        | Some s3Img -> Uploaded s3Img.S3Name
                        | None ->
                            (localImg, size)
                            |> ToUpload.ToUpload
                            |> ToUpload
                    
                    { Name = localName
                      UploadGetter = tryFindInMap }

                | None ->
                    let makeToUpload size =
                        ToUpload.ToUpload (localImg, size)
                        |> ToUpload

                    { Name = localName
                      UploadGetter =
                        makeSizeMap
                            (makeToUpload Original)
                            (makeToUpload Px2000)
                            (makeToUpload Px1000)
                            (makeToUpload Px400)
                    }
        )

let getToUploads syncImgs =
    syncImgs
    |> List.collect
        (fun { UploadGetter = getter } ->
            List.map getter allSizes
            |> List.choose
                (function
                 | Uploaded _ -> None
                 | ToUpload toUpload -> Some toUpload))







open Amazon.S3.Model
open S3Images

let uploadFile (toUpload : ToUpload) =
    let req = new PutObjectRequest()
    req.BucketName <- bucketName
    req.Key <- toUpload.uploadStr
    req.FilePath <- toUpload.localPath

    client.PutObjectAsync req
    |> Async.AwaitTask


let uploadAllFiles =
    List.map uploadFile >> Async.Parallel >> Async.map Array.toList
