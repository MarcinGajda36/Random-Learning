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

let textToNumer = [|
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


let findAllDigits (text: string) = 
    let getIndexAndValue index c = 
        if Char.IsDigit c then (index, Char.GetNumericValue c |> int) else (-1, 0)
    // First and last index are enought, but i still don't like it, i want to find all.
    let fromWordsFirsts = textToNumer |> Seq.map (fun (word, digit) -> (text.IndexOf word, digit))
    let fromWordsLats = textToNumer |> Seq.map (fun (word, digit) -> (text.LastIndexOf word, digit))
    let fromDigits = text |> Seq.mapi getIndexAndValue
    fromDigits 
    |> Seq.append fromWordsFirsts
    |> Seq.append fromWordsLats
    |> Seq.filter (fun (index, _) -> index <> -1)
    |> Seq.sortBy (fun (index, _) -> index)
    |> Seq.toArray

let fullNumber indexedDigits = 
    let ordered = indexedDigits |> Array.map snd
    let first = Array.head ordered
    let last = Array.last ordered
    (string first) + (string last) |> int
    
let numbers = 
    fullInput
    |> Array.map findAllDigits
    |> Array.map fullNumber

Array.iter (printfn "%A") numbers

Array.sum numbers
// Correct: 57345

let digits = findAllDigits "eightwothree" 
let number = fullNumber digits
