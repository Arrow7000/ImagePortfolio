open System.IO

open LocalImages
open S3Images
open SyncImages
open ImageConversion

[<EntryPoint>]
let main argv =
    async {
        let! s3imgs = getAllS3Imgs ()
        
        let! results =
            getSyncImgs localImages s3imgs
            |> getToUploads
            |> uploadAllFiles

        printf "%A" results
    } |> Async.RunSynchronously

    0 // return an integer exit code
