using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace KOL24Server.Services;

public class TaskManagerService : TaskManager.TaskManagerBase
{
    public override Task<TaskResponse> AddTask(AddTaskRequest request, ServerCallContext context)
    {
        var task = new TaskItem
        {
            Id = Zadaci.Instanca().SledeciId++,
            Description = request.Description,
            Completed = false
        };

        Zadaci.Instanca().Lista.Add(task);

        return Task.FromResult(new TaskResponse
        {
            Success = true,
            Message = "Zadatak je dodat.",
            Task = task
        });
    }

    public override Task<TaskList> ListTasks(Empty request, ServerCallContext context)
    {
        var response = new TaskList();
        response.Tasks.AddRange(Zadaci.Instanca().Lista);

        return Task.FromResult(response);
    }

    public override Task<TaskResponse> MarkTaskAsCompleted(TaskIdRequest request, ServerCallContext context)
    {
        var task = Zadaci.Instanca().Lista.FirstOrDefault(z => z.Id == request.Id);

        if (task == null)
        {
            return Task.FromResult(new TaskResponse
            {
                Success = false,
                Message = "Zadatak ne postoji."
            });
        }

        task.Completed = true;

        return Task.FromResult(new TaskResponse
        {
            Success = true,
            Message = "Zadatak je oznacen kao zavrsen.",
            Task = task
        });
    }
}
