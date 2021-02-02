open System
open Suave
open FSharp.Data
open LocalImages
open S3Images
open SyncImages
open Api
open DB
open UploadAndProcess
open Server

open dotenv.net

let netlifyTriggerUrl =
    "https://api.netlify.com/build_hooks/5f048f19c1864101e8c5bd12"

let triggerNetlifyBuild _ =
    Http.AsyncRequest(netlifyTriggerUrl, httpMethod="POST")

[<EntryPoint>]
let main _ =
    DotEnv.Config(false, __SOURCE_DIRECTORY__ + "/.env")


    let config =
        { defaultConfig with
              bindings = [ HttpBinding.create HTTP Net.IPAddress.Any (uint16 4000) ]
              hideHeader = true }

    startWebServer config api
    0 // return an integer exit code
