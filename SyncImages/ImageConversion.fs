module ImageConversion


open System.IO
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing


type OrigDimensions =
    { Height    : int
      Width     : int }

/// Temp path to uploaded file
type TempFilePath = TempFilePath of string


let scaleMaxTo max (w,h) : int * int =
    if w > h then
        max, (float h / float w) * float max |> round |> int
    else (float w / float h) * float max |> round |> int, max



let resizeImg maxSideSizeOpt (TempFilePath path) =
    async {
        use imgStream = new FileStream(path, FileMode.Open)

        let! image,_ =
            Image.LoadWithFormatAsync(imgStream)
            |> Async.AwaitTask

        let (newWidth,newHeight) =
            let w = image.Width
            let h = image.Height
            match maxSideSizeOpt with
            | Some maxSideSize -> scaleMaxTo maxSideSize (w,h)
            | None -> w, h

        use copy = image.Clone (fun x -> x.Resize(newWidth, newHeight) |> ignore)

        let stream = new MemoryStream()
        let encoder = new Formats.Jpeg.JpegEncoder()
        do! copy.SaveAsync(stream, encoder) |> Async.AwaitTask

        return stream
    }



let getImageDimensions (TempFilePath path) =
    async {
        let! image = Image.IdentifyAsync path |> Async.AwaitTask

        let w = image.Width
        let h = image.Height

        return { Height = h; Width = w }
    }
