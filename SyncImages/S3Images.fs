module S3Images


open Amazon.S3.Model
open FSharp.Text.RegexProvider
open S3Setup



let rec private requestUntilDone results (req : ListObjectsRequest) =
    async {
        let! result = client.ListObjectsAsync req |> Async.AwaitTask
        let resultsSoFar =
            result
            |> List.singleton
            |> List.append results

        let thereAreMoreResults =
            result.IsTruncated && result.NextMarker <> null

        let! allResults =
            if thereAreMoreResults then
                req.Marker <- result.NextMarker
                requestUntilDone resultsSoFar req
            else
                Async.result resultsSoFar

        return allResults
    }


let getAllS3ObjectPaths () =
    let req = new ListObjectsRequest()
    req.BucketName <- bucketName
    let allresults = requestUntilDone List.Empty req

    allresults
    |> Async.map
        (Seq.collect (fun r -> r.S3Objects)
         >> Seq.map (fun o -> o.Key)
         >> Seq.toList)

[<Literal>]
let regex = "^(?<name>\w+)\/\w+(?:-(?<size>\d+))?\.jpg$"

type S3ImageProvider = Regex<regex>

let getS3ImgFromStr str =
    match S3ImageProvider().TryTypedMatch str with
    | None -> failwithf "File '%s' doesn't match expected conventon" str
    | Some m -> { S3Name = m.name.Value; Size = Size.parse m.size.Value }

let getAllS3Imgs = getAllS3ObjectPaths >> Async.map (List.map getS3ImgFromStr)

