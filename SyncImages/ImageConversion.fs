module ImageConversion


open System.IO
open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing


let scaleMaxTo max (w,h) : int * int =
    if w > h then
        max, (float h / float w) * float max |> round |> int
    else (float w / float h) * float max |> round |> int, max



let resizeImg maxSideSizeOpt (path : string) =
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
