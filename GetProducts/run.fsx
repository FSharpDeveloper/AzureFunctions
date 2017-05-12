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
    name: string
}

[<CLIMutable>]
type person = {
    Id: int
    FirstName: string
    LastName: string
} with
    static member fromReader(rdr:IDataReader) = {
        Id = rdr.GetInt32 0
        FirstName = rdr.GetString 5
        LastName = rdr.GetString 6
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
        //"Server=tcp:knetg8pe2f.database.windows.net,1433;Initial Catalog=netcoredynamicsdb;Persist Security Info=False;User ID=newsdbadmin;Password=Mrullerp!014;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"       
            //configurationSection.ConnectionString
        let! selectMembers =
            async{
                let cnt = new SqlConnection(connectionString)
                
                cnt.Open() |> ignore
                let command = new SqlCommand("Select * From Member", cnt)    
                let! reader = command.ExecuteReaderAsync() |> Async.AwaitTask
                let result = person.asSeq(reader)
                return result      
            }  
        let members = 
            selectMembers
            |> Seq.toList
        // Set name to query string
        let name =
            req.GetQueryNameValuePairs()
            |> Seq.tryFind (fun q -> q.Key = "name")

        match name with
        | Some x ->
            return req.CreateResponse(HttpStatusCode.OK, members)//, "Hello " + x.Value);
        | None ->
            let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask

            if not (String.IsNullOrEmpty(data)) then
                let named = JsonConvert.DeserializeObject<Named>(data)
                return req.CreateResponse(HttpStatusCode.OK, members)//"Hello " + named.name);
            else
                return req.CreateResponse(HttpStatusCode.BadRequest, "Specify a Name value");
    } |> Async.RunSynchronously
