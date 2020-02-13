open LocalImages
open S3Images
open SyncImages
open Api

[<EntryPoint>]
let main argv =
    async {
        let! s3imgs = getAllS3Imgs ()
        
        let syncImgs = getSyncImgs localImages s3imgs

        do!
            syncImgs
            |> getToUploads
            |> uploadAllFiles
            |> Async.Ignore

        let metadata = makeInfo localImages
        printfn "%A" metadata
        do! (serialise metadata |> uploadMetadata |> Async.Ignore)

    } |> Async.RunSynchronously

    0 // return an integer exit code
