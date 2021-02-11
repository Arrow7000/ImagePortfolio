module UploadAndProcess

open System
open System.IO
open Amazon.S3.Model
open S3Setup
open ImageConversion
open DB




let uploadSingleFile (S3Path s3Path) (stream : Stream) =
    let req = new PutObjectRequest()
    req.BucketName <- bucketName
    req.Key <- s3Path
    req.InputStream <- stream

    client.PutObjectAsync req
    |> Async.AwaitTask


let convertAndUploadSingleImg s3Path origOrSize path =
    async {
        let sizeOpt =
            match origOrSize with
            |Original -> None
            | Size size -> Some size

        use! stream = resizeImg sizeOpt path
        let! uploadResult = uploadSingleFile s3Path stream

        stream.Dispose()

        return uploadResult
    }



let getS3ImgStream (S3Path s3Path) =
    async {
        let req = new Amazon.S3.Model.GetObjectRequest()
        req.BucketName <- bucketName
        req.Key <- s3Path

        let! obj =
            client.GetObjectAsync req
            |> Async.AwaitTask

        return obj.ResponseStream
    }


let checkIfImageExistsInS3 (S3Path s3Path) =
    let req = new Amazon.S3.Model.GetObjectRequest()
    req.BucketName <- bucketName
    req.Key <- s3Path

    client.GetObjectAsync req
    |> Async.AwaitTask
    |> Async.Catch
    |> Async.map (Choice.toResult >> function Ok obj -> Some obj | _ -> None)



let convertAndUploadImgIfNotAlreadyInS3 s3Path origOrSize path =
    async {
        let! result = checkIfImageExistsInS3 s3Path

        let! opResult =
            match result with
            | Some _ -> Async.result None
            | None ->
                convertAndUploadSingleImg s3Path origOrSize path
                |> Async.map Some

        return opResult
    }


let getAllImageFiles origHash =
    async {
        let! sizes = getSizes()

        let sizedImgPaths =
            seq { for size in sizes -> getS3Path origHash (Size size), Size size }
            |> List.ofSeq
        let origPath = getS3Path origHash Original, Original

        return origPath :: sizedImgPaths
    }




let uploadImgsIdempotently s3PathsList filePath =
    async {
        do!
            s3PathsList
            |> List.map
                (fun (path, origOrSize) ->
                    convertAndUploadImgIfNotAlreadyInS3 path origOrSize filePath)
            |> Async.Sequential
            |> Async.map ignore
    }


let uploadAllRequiredImgs hash stream =
    async {
        let! imgPaths = getAllImageFiles hash
        do! uploadImgsIdempotently imgPaths stream
    }

