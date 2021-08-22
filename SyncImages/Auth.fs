module Auth


open System
open Suave
open Suave.Operators
open Suave.Cookie
open JWT
open JWT.Builder
open JWT.Algorithms
open JWT.Exceptions


let serverKey = YoLo.Env.varRequired "SERVER_KEY"
let expiryInDays = 30.
let makeExpiry () =
    DateTimeOffset.UtcNow.AddDays(expiryInDays)


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
            .AddClaim("exp", (makeExpiry()).ToUnixTimeSeconds())
            .AddClaim("username", username)
            .AddClaim("role", "Admin")
            .Encode()

    token



/// In contrast to UNAUTHORIZED this one doesn't include the www-authenticate header
let lessSuckyUnauthorized body =
    body
    |> UTF8.bytes
    |> Response.response HttpCode.HTTP_401




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
                    let token = makeTokenForUser user
                    let cookieWp =
                        setCookie
                            { name = "token"
                              value = token
                              httpOnly = true
                              expires = makeExpiry() |> Some
                              path = Some "/"
                              domain = None
                              secure = false
                              sameSite = None }

                    cookieWp >=> Successful.OK "Token set in cookie"
                | None ->
                    lessSuckyUnauthorized "Wrong username or password"
            | _ ->
                lessSuckyUnauthorized "Need to incldue both username and password fields")



let authRoute (authedWp : WebPart) : WebPart =
    request
        (fun req ->
            match Map.tryFind "token" req.cookies with
            | Some tokenCookie ->
                let token = tokenCookie.value

                try
                    // Do something with token if required
                    let _ =
                        (new JwtBuilder())
                            .WithAlgorithm(new HMACSHA256Algorithm())
                            .WithSecret(serverKey)
                            .MustVerifySignature()
                            .Decode(token)

                    authedWp

                with 
                | :? TokenExpiredException ->
                    lessSuckyUnauthorized "Token has expired"
                | :? SignatureVerificationException ->
                    lessSuckyUnauthorized "Token has invalid signature"
                | :? FormatException ->
                    lessSuckyUnauthorized "Token is invalid"

            | None ->
                 lessSuckyUnauthorized "Authorization token is required for this route")


let checkAuthState = authRoute (Successful.OK "Logged in")
