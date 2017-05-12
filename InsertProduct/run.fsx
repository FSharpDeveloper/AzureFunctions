#r "System.Net.Http"
#r "Newtonsoft.Json"
#r "System.Data"
#r "System.Data.SqlClient.dll"
#r "System.Transactions.dll"
#r "System.Configuration"
#r "System.Threading"

open System.Net
open System.Net.Http
open System.Threading
open System.Data
open System.Data.SqlClient
open System.Linq
open System.Transactions
open System.Configuration
open Newtonsoft.Json
open Newtonsoft.Json.Linq

type Named = {
    name: string
}
 
[<CLIMutable>]
type Member = {
    FirstName: string
    LastName: string
    EmailAddress: string
}

[<CLIMutable>]
type Parameter = {Name:string; Value:obj}

[<CLIMutable>]
type CommandData = {SqlQuery:string;Parameters:SqlParameter array;CommandType:CommandType;ConnectionString:string}

let commandFun (commandData:CommandData) (command:SqlCommand)=
    command.CommandText <- commandData.SqlQuery
    commandData.Parameters 
    //|> Seq.map (fun i -> new SqlParameter(i.Name, i.Value))
    //|> Seq.toArray
    |> command.Parameters.AddRange    

    command.Connection.Open()                    
    let result = command.ExecuteNonQuery()
    result  

let connectionFun (connection:SqlConnection) (commandData:CommandData)= 
    connection.CreateCommand()
    |> commandFun commandData

let execNonQueryAsync commandData= 
    async {
        return using(new SqlConnection(commandData.ConnectionString))
            connectionFun commandData
    }  

// let connectionString = "Server=tcp:netcoredbs.database.windows.net,1433;Initial Catalog=netcoredynamicsDb;Persist Security Info=False;User ID=dbazureuser;Password=Mrullerp!014;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
let insertQuery = "Insert Into [Member] ([GroupId], [FirstName], [LastName], [EmailAddress], [CreatedAt], [ModifiedAt]) Values (@GroupId, @FirstName, @LastName, @EmailAddress, @CreatedAt, @ModifiedAt)"
let parameters: Parameter list = 
    [
        {Name="@GroupId"; Value=1}; {Name="@FirstName"; Value="firstName"}; 
        {Name="@LastName"; Value="lastName"}; {Name="@EmailAddress"; Value="address"};
        {Name="@CreatedAt"; Value="12/12/2012 12:00:00"}; {Name="@ModifiedAt"; Value="12/12/2012 12:00:00"};                
    ]
    //|> Seq.map (fun i -> new SqlParameter(i.Name, i.Value))
    //|> Seq.toArray
let toSqlParameterArray items= 
    items 
    |> Seq.map (fun i -> new SqlParameter(parameterName=i.Name, value=i.Value))
    |> Seq.toArray

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {   
        let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask
        let json = JsonConvert.DeserializeObject<Member>(data)
        let newMember = {FirstName=json.FirstName;LastName=json.LastName;EmailAddress=json.EmailAddress}
        //req.
        let connectionString = "Server=tcp:netcoredbs.database.windows.net,1433;Initial Catalog=netcoredynamicsDb;Persist Security Info=False;User ID=dbazureuser;Password=Mrullerp!014;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
        log.Info(sprintf "%A" (parameters)) ////////
        let parameters = 
           [
                {Name="@GroupId"; Value=1}; {Name="@FirstName"; Value=newMember.FirstName}; 
                {Name="@LastName"; Value=newMember.LastName}; {Name="@EmailAddress"; Value=newMember.EmailAddress};
                {Name="@CreatedAt"; Value="12/12/2012 12:00:00"}; {Name="@ModifiedAt"; Value="12/12/2012 12:00:00"};                
            ]
            |> toSqlParameterArray

        //let! execInsert = execNonQueryAsync {SqlQuery=insertQuery; Parameters=parameters; CommandType=CommandType.Text; ConnectionString=connectionString }    

        use connection = new SqlConnection(connectionString)
        connection.Open()
        use transaction = connection.BeginTransaction()                            
        use command = connection.CreateCommand()
        
        command.Transaction <- transaction
        command.CommandText <- insertQuery
        parameters
        |> command.Parameters.AddRange
        
        if (transaction.Connection.State = ConnectionState.Closed) then transaction.Connection.Open()
        let! result = command.ExecuteNonQueryAsync() |> Async.AwaitTask                        
        
        transaction.Commit()
        
        log.Info(sprintf "%i" (result) )//(execInsert))
        
        return req.CreateResponse(HttpStatusCode.OK, result)
    } |> Async.RunSynchronously
