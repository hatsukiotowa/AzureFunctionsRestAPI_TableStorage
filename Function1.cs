using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Threading.Tasks;
using static ServerlessFuncs.Todo;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

namespace ServerlessFuncs
{
    public static class TodoApi
    {
        static List<Todo> items = new List<Todo>();
        
        [FunctionName("CreateTodo")]
        public static async Task<IActionResult> CreateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "missions")]HttpRequest req, 
            [Table("Todos", Connection = "AzureWebJobsStorage" )] IAsyncCollector<TodoTableEntity> todoTable,
            TraceWriter log)
        {
            log.Info("Creating a new todo list item");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var input = JsonConvert.DeserializeObject<TodoCreateModel>(requestBody);

            var todo = new Todo() { MissionDescription = input.MissionDescription };
            await todoTable.AddAsync(todo.ToTableEntity());
            return new OkObjectResult(todo);
        }

        [FunctionName("GetTodos")]
        public static async Task<IActionResult> GetTodos(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "missions")]HttpRequest req,
            [Table("Todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            TraceWriter log)
        {
            log.Info("Getting todo list items");
            var query = new TableQuery<TodoTableEntity>();
            var segment = await todoTable.ExecuteQuerySegmentedAsync(query, null);
            return new OkObjectResult(segment.Select(Mappings.ToTodo));
        }

        [FunctionName("GetTodoById")]
        public static async Task<IActionResult> GetTodoById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mission/{id}")]HttpRequest req,
            [Table("Todos", "TODO", "{id}", Connection = "AzureWebJobsStorage")] TodoTableEntity todo,
            TraceWriter log, string id)
        {
            log.Info("Getting todo item by id");
            if (todo == null)
            {
                log.Info($"Item {id} not found");
                return new NotFoundResult();
            }
            return new OkObjectResult(todo.ToTodo());
        }

        [FunctionName("UpdateTodo")]
        public static async Task<IActionResult> UpdateTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "mission/{id}")]HttpRequest req,
            [Table("Todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            TraceWriter log, string id)
        {


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var updated = JsonConvert.DeserializeObject<TodoUpdateModel>(requestBody);

            var findOperation = TableOperation.Retrieve<TodoTableEntity>("TODO", id);
            var findResult = await todoTable.ExecuteAsync(findOperation);
            if (findResult.Result == null)
            {
                return new NotFoundResult();
            }

            var existingRow = (TodoTableEntity)findResult.Result;
            existingRow.IsCompleted = updated.IsCompleted;
            if (!string.IsNullOrEmpty(updated.MissionDescription))
            {
                existingRow.MissionDescription = updated.MissionDescription;
            }
            var replaceOperation = TableOperation.Replace(existingRow);
            await todoTable.ExecuteAsync(replaceOperation);

            return new OkObjectResult(existingRow.ToTodo());
        }

        [FunctionName("DeleteTodo")]
        public static async Task<IActionResult> DeleteTodo(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "mission/{id}")]HttpRequest req,
            [Table("Todos", Connection = "AzureWebJobsStorage")] CloudTable todoTable,
            TraceWriter log, string id)
        {
            var deleteOperation = TableOperation.Delete(new TableEntity()
            { PartitionKey = "TODO", RowKey = id, ETag = "*" });
            try
            {
                var deleteResult = await todoTable.ExecuteAsync(deleteOperation);
            }catch(StorageException e) when (e.RequestInformation.HttpStatusCode == 404)
            {
                return new NotFoundResult();
            }
            return new OkResult();
        }
    }
}
