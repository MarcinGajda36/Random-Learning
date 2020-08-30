module Types

type Person = {
    Name: string
    LastName: string
    Age: int
}


let Marcin = { Name = "Marcin"; LastName = "Gajda"; Age = 1 }
let Michal = { Marcin with Name = "Michal" }

let inrefMethod (x: inref<Person>) =
    &x

let refTest =
    let Meteusz = { Michal with Name = "Mateusz"; LastName = "Nawrot" }
    inrefMethod &Meteusz

type SomeOther (i: int, a: string) = 
    member _.i = i
    member _.a = a
    member _.RandomTuple = (i,a)
    member _.RandomMethod arg = arg + 10

type TestConstructors () =
    new (i: int) as this = 
        TestConstructors() then
        this.I <- i

    new (i: int, s: string) =
        TestConstructors(i) then
        printf "%s" s

    member val I : int = 0 with get, set

type TestConstructors2 public (i: int) =
    member _.J = i
    member val I : int = 0 with get, set

let testConstructors = 
    let testConstructors = TestConstructors(5,"abc")
    let testConstructorss = TestConstructors(5)
    let testConstructors2 = TestConstructors2(1)
    testConstructors.I <- 1
    testConstructors2.I <- 2
    //testConstructors2.J <- 3 //get only
    0

let testOption i =
    i |> Option.map (fun x -> x * 3)
