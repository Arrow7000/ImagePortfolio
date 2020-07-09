open LocalImages
open S3Images
open SyncImages
open Api
open FSharp.Data

open dotenv.net

let netlifyTriggerUrl =
    "https://api.netlify.com/build_hooks/5f048f19c1864101e8c5bd12"

let triggerNetlifyBuild _ =
    Http.AsyncRequest(netlifyTriggerUrl, httpMethod="POST")

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
            |> Async.map triggerNetlifyBuild
            |> Async.Ignore

    } |> Async.RunSynchronously

    0 // return an integer exit code
