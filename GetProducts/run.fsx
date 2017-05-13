#r "System.Net.Http"
#r "Newtonsoft.Json"
#r "FSharp.Data"
#r "System.Data"
#r "System.Data.SqlClient"

open System.Net
open System.Net.Http
open System.Data
open System.Data.SqlClient
open Newtonsoft.Json
open FSharp.Data

type Named = {
    name:string
}
[<CLIMutable>]
type Member = {
    Id: int
    FirstName: string
    LastName: string
    // EmailAddress: string
    // CreatedAt:DateTime
    // ModifiedAt:DateTime
} with
    static member fromReader(rdr:IDataReader) = {
        {
            Id = Convert.ToInt32 rdr.["Id"]
            FirstName = Convert.ToString rdr.["FirstName"]
            LastName = Convert.ToString rdr.["LastName"]
            // EmailAddress = Convert.ToString rdr.["EmailAddress"]
            // CreatedAt = Convert.ToDateTime rdr.["CreatedAt"]
            // ModifiedAt = Convert.ToDateTime rdr.["ModifiedAt"]
        }
    }

    static member asSeq(rdr:IDataReader) = seq {
        while rdr.Read() do
            yield person.fromReader rdr
    }


let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        log.Info(sprintf 
            "F# HTTP trigger function processed a request.")
                
        let connectionString = "Server=tcp:netcoredbs.database.windows.net,1433;Initial Catalog=netcoredynamicsDb;Persist Security Info=False;User ID=dbazureuser;Password=Mrullerp!014;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        let selectQuery = "Select Id, FirstName, LastName, EmailAddress, CreatedAt, ModifiedAt From Member"
        let! selectMembers =
            async{
                let cnt = new SqlConnection(connectionString)
                
                cnt.Open() |> ignore
                let command = new SqlCommand(selectQuery, cnt)    
                let! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
                let result = Member.asSeq(reader)
                return result      
            }  
        let members = 
            selectMembers
            |> Seq.toArray

        return req.CreateResponse(HttpStatusCode.OK, members)//"Hello " + named.name);
    } |> Async.RunSynchronously
