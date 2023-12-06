module D1
open System.IO
open System

let inputPath = Path.Join [| Environment.CurrentDirectory; "D1Input.txt" |]
let fullInput = File.ReadAllLines inputPath

let shortInput = [| 
    "two1nine";
    "eightwothree";
    "abcone2threexyz";
    "xtwone3four";
    "4nineeightseven2";
    "zoneight234";
    "7pqrstsixteen";
|]

let wordToNumer = [|
    ("one", 1); 
    ("two", 2); 
    ("three", 3); 
    ("four", 4); 
    ("five", 5); 
    ("six", 6); 
    ("seven", 7); 
    ("eight", 8); 
    ("nine", 9); 
|]


let getAllWordIndexes (word: string) (text: string) =
    let rec loop (startIndex: int) soFar =
        let index = text.IndexOf (word, startIndex)
        if index = -1 
        then soFar
        else loop (index + word.Length) (soFar |> List.append [index])
    loop 0 []

let findAllDigits text = 
    let getIndexAndValue index c = 
        if Char.IsDigit c then (index, Char.GetNumericValue c |> int) else (-1, 0)
    let words = 
        wordToNumer |> Seq.collect (fun (word, digit) -> getAllWordIndexes word text |> Seq.map (fun index -> (index, digit)))
    let digits = text |> Seq.mapi getIndexAndValue
    digits 
    |> Seq.append words
    |> Seq.filter (fun (index, _) -> index <> -1)

let fullNumber indexedDigits = 
    let ordered = 
        indexedDigits 
        |> Seq.sortBy (fun (index, _) -> index)
        |> Seq.map snd
        |> Seq.toArray
    let first = Array.head ordered
    let last = Array.last ordered
    (string first) + (string last) |> int
    
let numbers = 
    fullInput
    |> Array.map findAllDigits
    |> Array.map fullNumber

// Array.iter (printfn "%A") numbers

Array.sum numbers
// Correct: 57345

let digits = findAllDigits "eightwothree" 
let number = fullNumber digits
