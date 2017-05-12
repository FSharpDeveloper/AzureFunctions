#r "System.Net.Http"
#r "Newtonsoft.Json"
#r "System.Data"
open System.Net
open System.Net.Http
open System.Data.SqlClient
open System.Threading
open Newtonsoft.Json

type Named = {
    name: string
}
[<CLIMutable>]
[<NoComparison>]
type Parameter = { Name:string; Value: obj }

[<CLIMutable>]
type CommandData = { Sql:String; Parameters:Parameter array; ConnectionString:string }

let ConnectionString = "Server=tcp:netcoredbs.database.windows.net,1433;Initial Catalog=netcoredynamicsDb;Persist Security Info=False;User ID=dbazureuser;Password=Mrullerp!014;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
let InsertQuery = "Insert Into [Member] ([GroupId], [FirstName], [LastName], [EmailAddress], [CreatedAt], [ModifiedAt]) Values (@GroupId, @FirstName, @LastName, @Address, @CreatedAt, @ModifiedAt)"
let Parameters = 
    [
        {Name="@GroupId"; Value=1}; {Name="@FirstName"; Value="firstName"}; 
        {Name="@LastName"; Value="lastName"}; {Name="@Address"; Value="address"};
        {Name="@CreatedAt"; Value="12/12/2012 12:00:00"}; {Name="@ModifiedAt"; Value="12/12/2012 12:00:00"};                
    ]
    |> List.toArray

let ExecuteNonQueryAsync data= 
        use connection = new SqlConnection(data.ConnectionString)
        use command = new SqlCommand(data.Sql, connection)
        data.Parameters
        |> Seq.map (fun i -> SqlParameter(i.Name,i.Value))
        |> Seq.toArray
        |> command.Parameters.AddRange

        connection.Open() |> ignore
        let result = command.ExecuteNonQueryAsync() 
        result

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        log.Info(sprintf 
            "F# HTTP trigger function processed a request.")

        ExecuteNonQueryAsync {Sql=InsertQuery;Parameters=Parameters;ConnectionString=ConnectionString} |> Async.AwaitTask

        // Set name to query string
        let name =
            req.GetQueryNameValuePairs()
            |> Seq.tryFind (fun q -> q.Key = "name")

        match name with
        | Some x ->
            return req.CreateResponse(HttpStatusCode.OK, "Hello " + x.Value);
        | None ->
            let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask

            if not (String.IsNullOrEmpty(data)) then
                let named = JsonConvert.DeserializeObject<Named>(data)
                return req.CreateResponse(HttpStatusCode.OK, "Hello " + named.name);
            else
                return req.CreateResponse(HttpStatusCode.BadRequest, "Specify a Name value");
    } |> Async.RunSynchronously
