namespace HybridAngularTodoApp.Services;

using HybridAngularTodoApp.Models;
using SQLite;

public class TodoDatabase
{
    private readonly SQLiteAsyncConnection _database;

    public TodoDatabase(string dbPath)
    {
        _database = new SQLiteAsyncConnection(dbPath);
        _database.CreateTableAsync<TodoItem>().Wait();
    }

    public Task<List<TodoItem>> GetItemsAsync()
    {
        return _database.Table<TodoItem>().ToListAsync();
    }

    //HybridAngularTodoApp/Resources/Raw/todo-app
    //ng build --output-path="../../HybridAngularTodoApp/HybridAngularTodoApp/Resources/Raw/todo-app" --base-href="./"
    

    public Task<int> AddItemAsync(TodoItem item)

    {
        System.Console.WriteLine("Adding item to database: " + item.Title);
        return _database.InsertAsync(item);
    }

    public Task<int> RemoveItem(int id)
    {
        return _database.ExecuteAsync("DELETE FROM TodoItem WHERE Id = ?", id);
    }

    public Task<int> UpdateItem(int id, string title, string description, bool isCompleted)
    {
        return _database.ExecuteAsync("UPDATE TodoItem SET Title = ?, Description = ?, IsCompleted = ? WHERE [Id] = ?", title, description, isCompleted, id);
    }
    

    public async Task<int> ToggleComplete(int id)
    {
        var item = await _database.Table<TodoItem>().Where(x => x.Id == id).FirstOrDefaultAsync();
        if (item != null)
        {
            item.IsCompleted = !item.IsCompleted;
            return await _database.UpdateAsync(item);
        }

        return 0;
    }

    public Task<int> ClearCompleted()
    {
        return _database.ExecuteAsync("DELETE FROM TodoItem WHERE isCompleted = 1");
    }

    public async Task ClearAll()
    {
        var allItems = await GetItemsAsync();
        foreach (var item in allItems)
        {
            await RemoveItem(item.Id);
        }
    }
}