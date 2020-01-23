module ImageConversion


open LocalImages


open SixLabors.ImageSharp
open SixLabors.ImageSharp.Processing

open System.IO

let resizeImg maxSideSize (path : string) =
    use image = Image.Load(path)

    let (newWidth,newHeight) =
        let w = image.Width
        let h = image.Height
        if w > h then
            maxSideSize, h / w * maxSideSize
        else w / h * maxSideSize, maxSideSize

    use copy = image.Clone()

    let operation =
        fun (x : IImageProcessingContext<PixelFormats.Rgba32>) -> x.Resize(newWidth, newHeight) |> ignore

    copy.Mutate operation

    let stream = new MemoryStream()
    let encoder = new Formats.Jpeg.JpegEncoder()
    copy.Save(stream, encoder)

    stream
