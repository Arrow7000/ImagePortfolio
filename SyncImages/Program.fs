open System
open Suave
open Server
open dotenv.net


[<EntryPoint>]
let main _ =
    DotEnv.Config(false, __SOURCE_DIRECTORY__ + "/.env")

    let portInt =
        Env.var "PORT"
        |> Option.map uint16
        |> Option.defaultValue (uint16 4000)

    let config =
        { defaultConfig with
              bindings = [ HttpBinding.create HTTP Net.IPAddress.Any portInt ]
              hideHeader = true }

    startWebServer config api
    0 // return an integer exit code
