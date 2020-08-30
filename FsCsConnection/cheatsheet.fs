module Cheatsheet

open System.Linq

let negate x = x * -1
let square x = x * x
let print x = printfn "The number is: %d" x

let squareNegateThenPrint' =
    square
    >> negate
    >> print

let ``square, negate, then print`` x =
    x
    |> square
    |> negate
    |> print

let sign x =
    match x with
    | 0 -> 0
    | x when x < 0 -> -1
    | _ -> 1

let zs = List.init 5 (fun i -> 2 * i + 1)
let xs' = Array.fold (fun str n -> sprintf "%s,%i" str n) "" [| 0..9 |]

type Tree<'T> =
    | Node of Tree<'T> * 'T * Tree<'T>
    | Leaf

type Vector(x : float, y : float) =
    let mag = sqrt (x * x + y * y) // (1)
    member this.X = x // (2)
    member this.Y = y
    member this.Mag = mag
    member this.Scale(s) = // (3)
        Vector(x * s, y * s)
    static member (+) (a : Vector, b : Vector) = // (4)
        Vector(a.X + b.X, a.Y + b.Y)

let testVector = Vector(1., 2.)
let testVectorMethod = testVector.Scale 2.

type IVector =
    abstract Scale : float -> IVector

type Vector'(x, y) =

    interface IVector with
        member __.Scale(s) = Vector'(x * s, y * s) :> IVector

    member __.X = x
    member __.Y = y

let testVector1 = Vector'(1., 2.)
let testVector1Upcast = testVector1 :> IVector
let testVectorMethod11 = testVector1Upcast.Scale 2.

let variablesTest =
    let localFunc _ = 0
    let localFunc2 x = x * 2
    let typed : string = ""
    0

let isEven i = i % 2 = 0
let double i = i * 2

let range = 
    [ 0..2..10 ] @ [ 1..3..10 ]
    |> List.where isEven
    |> List.map double

let pipes =
    let list = [0..1]
    let state0 = 0
    (state0, list) ||> List.fold (fun state element ->  state + element)

let reduce =
    let list = [0..1]
    List.reduce (+) list

let rangeMy limit =
    seq { 0..limit }

let forSeq limit =
    seq { for x in 0..2..limit -> double x }

let arrayTest =
    let (evens, odds) = 
        [| 0..100 |]
        |> Array.partition isEven

    let sum = Array.sum evens
    let count = Array.length odds
    [|sum; count|]

let [<Literal>] asdsdf = 1

let refTest (i: inref<int>, j: outref<int>, k: byref<int>) = 
    k <- 1
    j <- 5
    i + 5

