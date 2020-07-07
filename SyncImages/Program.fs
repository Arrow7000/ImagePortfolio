open LocalImages
open S3Images
open SyncImages
open Api

open dotenv.net


[<EntryPoint>]
let main argv =
    DotEnv.Config(false, __SOURCE_DIRECTORY__ + "/.env")

    async {
        let! s3imgs = getAllS3Imgs ()
        
        let syncImgs = getSyncImgs localImages s3imgs

        do!
            syncImgs
            |> getToUploads
            |> uploadAllFiles
            |> Async.Ignore

        let metadata = makeInfo localImages

        do!
            serialise metadata
            |> tee (printfn "%A")
            |> uploadMetadata
            |> Async.Ignore

    } |> Async.RunSynchronously

    0 // return an integer exit code
