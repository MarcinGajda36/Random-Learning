module OptionBuilderTests

type OptionBuilder() =
  member _.Bind(opt, binder) =
    match opt with Some value -> binder value | None -> None
  member __.Return(value) = Some value

let option = OptionBuilder()

let opttttt yy = 
    option {
        let! x = Some 0
        let! y = yy 
        let! z = Some 1
        // Code will only hit this point if the three
        // operations above return Some
     return x + y + z
    }

let opttttion op =
    op 
    |> Option.map double
    |> Option.isSome
