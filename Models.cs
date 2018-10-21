using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using static ServerlessFuncs.Todo;

namespace ServerlessFuncs
{
    public class Todo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("n");
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public string MissionDescription { get; set; }
        public bool IsCompleted { get; set; }

    }

    public class TodoCreateModel
    {
        public string MissionDescription { get; set; }
    }

    public class TodoUpdateModel
    {
        public string MissionDescription { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class TodoTableEntity : TableEntity
    {
        public DateTime CreatedTime { get; set; }
        public string MissionDescription { get; set; }
        public bool IsCompleted { get; set; }
    }

    public static class Mappings
    {
        public static TodoTableEntity ToTableEntity(this Todo todo)
        {
            return new TodoTableEntity()
            {
                PartitionKey = "TODO",
                RowKey = todo.Id,
                CreatedTime = todo.CreatedTime,
                IsCompleted = todo.IsCompleted,
                MissionDescription = todo.MissionDescription
            };
        }

        public static Todo ToTodo(this TodoTableEntity todo)
        {
            return new Todo()
            {
                Id = todo.RowKey,
                CreatedTime = todo.CreatedTime,
                IsCompleted = todo.IsCompleted,
                MissionDescription = todo.MissionDescription
            };
        }

    }

 }
