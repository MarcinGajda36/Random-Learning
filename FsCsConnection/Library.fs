namespace FsCsConnection

open ProviderImplementation.ProvidedTypes.BinaryWriter
open PerKeySynchronizers.UnboundedParallelism
open System
open System.Threading
open System.Threading.Tasks

module Say =
    let methodWithIn (a:inref<int>) =
        a + 10

    let useInRef =
        let x = 1
        methodWithIn &x


    let hello name =
        printfn "Hello %s" name

    // https://www.codewars.com/kata/57a1fd2ce298a731b20006a4
    let isPalindrom1 (text: string) =
      let rec loop (rest: List<char>) =
        match rest with 
        | list when list.Length > 2 -> 
            List.head list = List.last list 
            && list |> List.skip 1 |> List.take (list.Length - 2) |> loop
        | [first; second] -> first = second
        | [_] -> true
        | [] -> true
      text |> Seq.toList |> loop

    let isPalindrom (text: string) =
      let rec loop (rest: string) =
        match rest.Length with 
        | 0 | 1 -> true
        | _ -> 
            (rest.[0] = rest.[rest.Length - 1])
            && (loop rest.[1..(rest.Length - 2)])
      text.ToUpperInvariant () |> loop

    let synchronizer = new PerKeySynchronizer<Guid> ()
    let usePerKeyTask = task {
        let key = Guid.NewGuid()
        let operation (token: CancellationToken) = task { 1 + 1 } |> ValueTask
        let! token = Async.CancellationToken
        let! result = synchronizer.SynchronizeAsync (key, operation, token)
        ()
    }

    let usePerKeyAsync = async {
        let key = Guid.NewGuid()
        let operation (token: CancellationToken) = task { 1 + 1 } |> ValueTask
        let! token = Async.CancellationToken
        let! result = (synchronizer.SynchronizeAsync (key, operation, token)).AsTask() |> Async.AwaitTask
        ()
    }
        