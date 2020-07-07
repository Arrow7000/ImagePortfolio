module SyncImages

open Amazon.S3.Model
open S3Setup
open LocalImages



let groupS3Imgs s3Imgs =
    List.groupBy (fun { S3Name = name } -> name) s3Imgs
    |> Map.ofList
    |> Map.map
        (fun _ allSizes ->
            allSizes
            |> List.groupBy (fun { LongestSize = size } -> size)
            |> List.choose
                (fun (size,imgs) ->
                    match imgs with
                    | [] -> None
                    | [ one ] -> Some (size, one)
                    | _ -> failwithf "Each image should at most have one S3 entry for each size")
            |> Map.ofList)



let getSyncImgs localImgOrAlbums s3Imgs =
    let groupedS3Imgs = groupS3Imgs s3Imgs

    let localImgs = flattenAlbums localImgOrAlbums

    localImgs
    |> List.map
        (fun localImg ->
                let { LocalName = localName } = localImg

                match Map.tryFind localName groupedS3Imgs with
                | Some sizeMap ->
                    let uploadStatuses =
                        OrigOrSize.blankMap
                        |> Map.map
                            (fun size () ->
                                match Map.tryFind size sizeMap with
                                | Some s3Img -> Uploaded s3Img.S3Name
                                | None ->
                                    (localImg, size)
                                    |> ToUpload.ToUpload
                                    |> ToUpload)
                    
                    { Name = localName
                      UploadStatuses = uploadStatuses }

                | None ->
                    let uploadStatuses =
                        OrigOrSize.blankMap
                        |> Map.map
                            (fun size () ->
                                ToUpload.ToUpload (localImg, size)
                                |> ToUpload)

                    { Name = localName
                      UploadStatuses = uploadStatuses })

let getToUploads syncImgs =
    syncImgs
    |> List.collect
        (fun { UploadStatuses = getterMap } ->
            getterMap
            |> Map.map
                (fun _ uploadStatus ->
                    match uploadStatus with
                    | Uploaded _ -> None
                    | ToUpload toUpload -> Some toUpload)
            |> Map.toList
            |> List.choose snd)






let uploadStream (toUpload : ToUpload) =
    let req = new PutObjectRequest()
    req.BucketName <- bucketName
    req.Key <- toUpload.s3Key
    req.InputStream <- toUpload.stream

    client.PutObjectAsync req
    |> Async.AwaitTask


let uploadAllFiles =
    List.map uploadStream
    >> Async.Parallel
    >> Async.map
        (Array.toList >> List.map (fun res -> res.ResponseMetadata.RequestId))

