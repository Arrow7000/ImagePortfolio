module Auth


open System
open Suave
open Suave.RequestErrors
open JWT
open JWT.Builder
open JWT.Algorithms
open FSharp.Data.LiteralProviders
open JWT.Exceptions


let serverKey = YoLo.Env.varRequired "SERVER_KEY"
let expiryInDays = 30.
let expiry =
    DateTimeOffset.UtcNow.AddDays(expiryInDays).ToUnixTimeSeconds()


type Password = Password of string

type Role = Admin

type User =
    { Username  : string
      Role      : Role }


type Session = 
    | NoSession
    | UserLoggedOn of User


let adminUsername = "aron@adler.dev"

/// Hash of the password I'm using
let hashedPw =
    "0f413ebfbc7d01a1ce77d130c695da27c218f21de29e5ce2a14ba8b21dd1d47c"


let hashPw (Password pw) =
    UTF8.bytes pw
    |> Bytes.sha256
    |> Bytes.toHex



let validateUser username pw =
    if username = adminUsername && hashPw pw = hashedPw
    then Some { Username = username; Role = Admin }
    else None



let makeTokenForUser { Username = username; Role = Admin } =
    let token =
        (new JwtBuilder())
            .WithAlgorithm(new HMACSHA256Algorithm()) // symmetric
            .WithSecret(serverKey)
            .AddClaim("exp", expiry)
            .AddClaim("username", username)
            .AddClaim("role", "Admin")
            .Encode()

    token




let logonHandler : WebPart =
    request
        (fun req ->
            let usernameOpt = req.Item "username"
            let pwOpt =
                req.Item "password"
                |> Option.map Password

            match usernameOpt, pwOpt with
            | Some username, Some pw ->
                match validateUser username pw with
                | Some user ->
                    makeTokenForUser user |> Successful.OK
                | None ->
                    UNAUTHORIZED "Wrong username or password"
            | _ ->
                UNAUTHORIZED "Need to incldue both username and password fields" )



let authRoute (authedWp : WebPart) : WebPart =
    request
        (fun req ctx ->
            match req.header "Authorization" with
            | Choice1Of2 bearerToken ->
                let token =
                    bearerToken
                    |> String.substring (String.length "Bearer ")

                try
                    // Do something with token if required
                    let _ =
                        (new JwtBuilder())
                            .WithAlgorithm(new HMACSHA256Algorithm()) // symmetric
                            .WithSecret(serverKey)
                            .MustVerifySignature()
                            .Decode(token)

                    authedWp ctx

                with 
                | :? TokenExpiredException ->
                    UNAUTHORIZED "Token has expired" ctx
                | :? SignatureVerificationException ->
                    UNAUTHORIZED "Token has invalid signature" ctx
                | :? FormatException ->
                    UNAUTHORIZED "Token is invalid" ctx


            | Choice2Of2 _ ->
                RequestErrors.UNAUTHORIZED "Authorization token is required for this route" ctx)
