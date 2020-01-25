open LocalImages
open S3Images
open SyncImages
open Api

[<EntryPoint>]
let main argv =
    async {
        let! s3imgs = getAllS3Imgs ()
        
        let syncImgs = getSyncImgs localImages s3imgs

        let! _ =
            syncImgs
            |> getToUploads
            |> uploadAllFiles

        printfn "%A" (makeInfo syncImgs |> serialise)

    } |> Async.RunSynchronously

    0 // return an integer exit code
