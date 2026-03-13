using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using TodoApi.Models;

namespace TodoApi.Services;

public class TodoService
{
    private readonly IMongoCollection<TodoItem>? _todoCollection;
    private readonly bool _useInMemoryStore;
    private static readonly List<TodoItem> InMemoryTodos = [];
    private static readonly object InMemoryLock = new();

    public TodoService(IOptions<TodoDatabaseSettings> dbSettings, IConfiguration configuration)
    {
        _useInMemoryStore = configuration.GetValue<bool>("UseInMemoryStore");
        if (_useInMemoryStore)
        {
            return;
        }

        var settings = dbSettings.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        _todoCollection = database.GetCollection<TodoItem>(settings.TodoCollectionName);
    }

    public async Task<List<TodoItem>> GetAsync()
    {
        if (_useInMemoryStore)
        {
            lock (InMemoryLock)
            {
                return InMemoryTodos.Select(t => new TodoItem { Id = t.Id, Name = t.Name, IsComplete = t.IsComplete }).ToList();
            }
        }

        return await _todoCollection!.Find(_ => true).ToListAsync();
    }

    public async Task<TodoItem?> GetAsync(string id)
    {
        if (_useInMemoryStore)
        {
            lock (InMemoryLock)
            {
                var todo = InMemoryTodos.FirstOrDefault(x => x.Id == id);
                return todo is null ? null : new TodoItem { Id = todo.Id, Name = todo.Name, IsComplete = todo.IsComplete };
            }
        }

        return await _todoCollection!.Find(x => x.Id == id).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(TodoItem item)
    {
        if (_useInMemoryStore)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                item.Id = ObjectId.GenerateNewId().ToString();
            }

            lock (InMemoryLock)
            {
                InMemoryTodos.Add(new TodoItem { Id = item.Id, Name = item.Name, IsComplete = item.IsComplete });
            }

            await Task.CompletedTask;
            return;
        }

        await _todoCollection!.InsertOneAsync(item);
    }

    public async Task<bool> UpdateAsync(string id, TodoItem item)
    {
        if (_useInMemoryStore)
        {
            lock (InMemoryLock)
            {
                var existing = InMemoryTodos.FirstOrDefault(x => x.Id == id);
                if (existing is null)
                {
                    return false;
                }

                existing.Name = item.Name;
                existing.IsComplete = item.IsComplete;
                return true;
            }
        }

        item.Id = id;
        var result = await _todoCollection!.ReplaceOneAsync(x => x.Id == id, item);
        return result.MatchedCount > 0;
    }

    public async Task<bool> RemoveAsync(string id)
    {
        if (_useInMemoryStore)
        {
            lock (InMemoryLock)
            {
                var existing = InMemoryTodos.FirstOrDefault(x => x.Id == id);
                if (existing is null)
                {
                    return false;
                }

                InMemoryTodos.Remove(existing);
                return true;
            }
        }

        var result = await _todoCollection!.DeleteOneAsync(x => x.Id == id);
        return result.DeletedCount > 0;
    }
}
