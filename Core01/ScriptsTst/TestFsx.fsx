open System.IO

//Ctrl + a -> Alt + Enter

#time
let src = Directory.GetFiles @"D:\Src"
let dst = DirectoryInfo @"D:\Dst"
if not dst.Exists then dst.Create()

let copyToDst src =
    let srcInfo = FileInfo src
    let dstName = Path.Combine(dst.FullName, srcInfo.Name)
    srcInfo.CopyTo(dstName, true) |> ignore

src |> Array.Parallel.iter copyToDst;
#time