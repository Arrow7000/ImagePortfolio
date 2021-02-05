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
    use image = Image.Load(path)

    let (newWidth,newHeight) =
        let w = image.Width
        let h = image.Height
        match maxSideSizeOpt with
        | Some maxSideSize -> scaleMaxTo maxSideSize (w,h)
        | None -> w, h

    use copy = image.Clone()

    let operation =
        fun (x : IImageProcessingContext<PixelFormats.Rgba32>) -> x.Resize(newWidth, newHeight) |> ignore

    copy.Mutate operation

    let stream = new MemoryStream()
    let encoder = new Formats.Jpeg.JpegEncoder()
    copy.Save(stream, encoder)

    stream



let getImageDimensions (TempFilePath path) =
    use image = Image.Load(path)
    let w = image.Width
    let h = image.Height

    { Height = h; Width = w }




let resizeImgFromStream maxSideSizeOpt (stream : Stream) =
    stream.Seek(0L, SeekOrigin.Begin) |> ignore // just in case
    use image = Image.Load(stream)

    let w = image.Width
    let h = image.Height
    let (newWidth,newHeight) =
        match maxSideSizeOpt with
        | Some maxSideSize -> scaleMaxTo maxSideSize (w,h)
        | None -> w, h

    use copy = image.Clone()

    let operation =
        fun (x : IImageProcessingContext<PixelFormats.Rgba32>) -> x.Resize(newWidth, newHeight) |> ignore

    copy.Mutate operation

    let stream = new MemoryStream()
    let encoder = new Formats.Jpeg.JpegEncoder()
    copy.Save(stream, encoder)

    stream
