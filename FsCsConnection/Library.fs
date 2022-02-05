namespace FsCsConnection

module Say =
    let methodWithIn (a:inref<int>) =
        a + 10

    let useInRef =
        let x = 1
        methodWithIn &x


    let hello name =
        printfn "Hello %s" name

