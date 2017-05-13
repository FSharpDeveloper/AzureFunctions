#r "System.Net.Http"
#r "Newtonsoft.Json"
#r "System.Data"
open System.Net
open System.Net.Http
open System.Data
open System.Data.SqlClient
open System.Threading
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type reqParams = {
    id:int
 }
[<CLIMutable>]
type Member = {
        Id:int
        FirstName:string
        LastName:string
        EmailAddress:string
        CreatedAt:DateTime
        ModifiedAt:DateTime
    }
    with 
    static member fromDataReader(rdr:IDataReader) = 
        rdr.Read() |> ignore
        {
            Id = Convert.ToInt32 rdr.["Id"]
            FirstName = Convert.ToString rdr.["FirstName"]
            LastName = Convert.ToString rdr.["LastName"]
            EmailAddress = Convert.ToString rdr.["EmailAddress"]
            CreatedAt = Convert.ToDateTime rdr.["CreatedAt"]
            ModifiedAt = Convert.ToDateTime rdr.["ModifiedAt"]
        }
            

    static member getId(id:int):Member option =
        let connectionString = "Server=tcp:netcoredbs.database.windows.net,1433;Initial Catalog=netcoredynamicsDb;Persist Security Info=False;User ID=dbazureuser;Password=Mrullerp!014;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        let selectQuery = "Select Id, FirstName,LastName, EmailAddress, CreatedAt, ModifiedAt From Member Where Id = @Id"
        //use connection = new SqlConnection(connectionString)
        use command = (new SqlConnection(connectionString)).CreateCommand()        
        command.CommandText <- selectQuery
        command.Parameters.AddWithValue("@Id", id)|> ignore
        command.Connection.Open()
        let reader = command.ExecuteReader()
        if reader.HasRows then 
            Some (Member.fromDataReader(reader))
        else 
            None


let Run(req: HttpRequestMessage, id:int, log: TraceWriter) =
    async {
        log.Info(sprintf 
            "F# HTTP trigger function processed a request.")
        //let ConnectionString = "Server=tcp:netcoredbs.database.windows.net,1433;Initial Catalog=netcoredynamicsDb;Persist Security Info=False;User ID=dbazureuser;Password=Mrullerp!014;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        //let selectQuery = "Select Id, FirstName,LastName, EmailAddress, CreatedAt, ModifiedAt From Member Where Id = @Id"
        // Set name to query string
        let name =
            req.GetQueryNameValuePairs()
            |> Seq.tryFind (fun q -> q.Key = "id")
            

        if id <> 0 then 
           let result = Member.getId(id)                 
           match result with 
           | Some r -> 
                log.Info(sprintf "result %s" r.FirstName)
                return req.CreateResponse(HttpStatusCode.OK, r)
           | None -> 
                return req.CreateResponse(HttpStatusCode.NotFound, "");
        else 
            return req.CreateResponse(HttpStatusCode.BadRequest, "")


    } |> Async.RunSynchronously
