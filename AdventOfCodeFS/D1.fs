module D1
open System.IO
open System

let inputPath = Path.Join [| Environment.CurrentDirectory; "D1Input.txt" |]
let input = File.ReadAllLines inputPath

let firstNumer text = text |> Seq.find Char.IsDigit
let lastNumber text = text |> Seq.rev |> Seq.find Char.IsDigit
let fullNumber text = 
    let first = firstNumer text 
    let last = lastNumber text
    (string first) + (string last) 
    |> int
    
let sum = input |> Array.map fullNumber |> Array.sum
