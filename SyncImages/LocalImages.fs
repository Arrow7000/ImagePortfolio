module LocalImages

open System.IO
open FSharp.Text.RegexProvider

let getSiblingDir dirName =
    //Path.Combine(Directory.GetParent(__SOURCE_DIRECTORY__).FullName, dirName)
    Path.Combine(__SOURCE_DIRECTORY__, dirName)

let currentImgDir = getSiblingDir "current"

let archiveImgDir = getSiblingDir "archive"



let localImgPaths =
    Directory.EnumerateFiles currentImgDir
    |> Seq.append (Directory.EnumerateFiles archiveImgDir)
    |> List.ofSeq



[<Literal>]
let regex = "(?<name>\w+)\.(?<extension>[a-zA-Z]+)$"
type LocalImageProvider = Regex<regex>

let getLocalName path =
    let m = LocalImageProvider().TypedMatch(path)
    { LocalName = m.name.Value
      Extension = m.extension.Value
      FullPath = path }


let localImages = localImgPaths |> List.map getLocalName
