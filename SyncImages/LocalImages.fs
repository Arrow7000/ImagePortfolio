module LocalImages

open System.IO
open FSharp.Text.RegexProvider



let flattenAlbums imgsAndAlbums =
    imgsAndAlbums
    |> List.collect
        (function
         | LocalImg img -> List.singleton img
         | LocalAlbum { Images = imgs } -> imgs)




let getTopLevelDir dirName =
    Path.Combine(Directory.GetParent(__SOURCE_DIRECTORY__).FullName, dirName)

let currentImgDir = getTopLevelDir "current"
let archiveImgDir = getTopLevelDir "archived"


let getFiles = Directory.EnumerateFiles >> List.ofSeq
let getFolders = Directory.EnumerateDirectories >> List.ofSeq


[<Literal>]
let regex = "(?<name>\w+)\.(?<extension>[a-zA-Z]+)$"
type LocalImageProvider = Regex<regex>

let makeLocalImg path =
    let m = LocalImageProvider().TypedMatch(path)
    { LocalName = m.name.Value
      Extension = m.extension.Value
      FullPath = path }


let makeLocalAlbum name imgs =
    { Name = name
      Images = imgs }


let getSingles = getFiles >> List.map makeLocalImg

let getAlbums =
    getFolders
    >> List.map
        (fun dir ->
            let name = Path.GetFileName dir
            let imgs = getSingles dir
            makeLocalAlbum name imgs)


let getSinglesAndAlbums topLevelDir =
    (getSingles topLevelDir |> List.map LocalImg)
    @ (getAlbums topLevelDir |> List.map LocalAlbum)





let tee f x =
    f x
    x

let localImages =
    getSinglesAndAlbums currentImgDir @ getSinglesAndAlbums archiveImgDir
    |> tee
        (fun everything ->
            let imgs = flattenAlbums everything
            let distinct = List.distinctBy (fun {LocalName=name} -> name) imgs
            if List.length distinct <> List.length imgs then
                failwithf "Several images have the same filename which is not allowed because they are stored on S3 in a flat list.")

