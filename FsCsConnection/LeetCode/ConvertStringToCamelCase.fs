module ConvertStringToCamelCase

open System;
open System.Buffers;
open System.Text;

let wordDelimiters = SearchValues.Create [|'-'; '_'|]

// Untested, codewars couldn't compile it
let toCamelCase (text : string) =
    let rec core (soFar: StringBuilder) (left: string) =
        let index = left.AsSpan().IndexOfAny wordDelimiters;
        match (index, soFar.Length) with 
            | (-1, _) -> soFar.Append(left)
            | (index, 0) -> core (soFar.Append(left[..index])) left[index..];
            | (index, _) -> core (soFar.Append((Char.ToUpper(left[0]))).Append(left[1..index])) left[index..];
    let result = core (new StringBuilder()) text;
    result.ToString()