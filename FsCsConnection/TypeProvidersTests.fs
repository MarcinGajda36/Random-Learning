module TypeProvidersTests

//let [<Literal>] JSON_URL = "https://jsonplaceholder.typicode.com/todos"

//// Type is created automatically from the url
//type public Todos = Fable.JsonProvider.Generator<JSON_URL>

//let result = async { 
//    let! (_, res) = Fable.SimpleHttp.Http.get JSON_URL
//    let todos = Todos.ParseArray res
//    for todo in todos do
//        // If the JSON schema changes, this will fail compilation
//        printfn "ID %f, USER: %f, TITLE %s, COMPLETED %b"
//            todo.id
//            todo.userId
//            todo.title
//            todo.completed
//}