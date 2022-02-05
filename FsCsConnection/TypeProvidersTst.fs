module TypeProvidersTst

//open FSharp.Data

//[<Literal>]
//let coinmarketcap = "https://api.coinmarketcap.com/v1/ticker/?limit=10"

//[<Literal>]
//let coinmarketcap2 = "https://api.coinmarketcap.com/v2/ticker/?limit=10"

//type Coins = JsonProvider<coinmarketcap>

//type Coins2 = JsonProvider<coinmarketcap2>

//let GetCoins url =
//    async {
//        let! load = Coins.AsyncLoad url
        
//        let first = load.[0]
//        let id = first.Id

//        printf "%A" first
        
//        return first.Id
//    }

//let GetCoinsTask url =
//    GetCoins url |> Async.StartAsTask
