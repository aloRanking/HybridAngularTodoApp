using System.Text.Json.Serialization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using HybridAngularTodoApp.Models;
using HybridAngularTodoApp.Services;
// REMOVED: using CommunityToolkit.Maui.Views; 

namespace HybridAngularTodoApp;

public partial class MainPage : ContentPage
{
    private readonly TodoDatabase _db;
    private TodoJSInvokeTarget _jsInvokeTarget;

    public MainPage(TodoDatabase db)
    {
        InitializeComponent();
        _db = db;

        // Check is removed, assuming XAML is now correct.
        // If TodoWebView is still null here, the XAML file is not associated with this code-behind.
        _jsInvokeTarget = new TodoJSInvokeTarget(this, _db);
        
        // This is the critical line that injects the JS functions into the WebView
        // The compile error is on the definition of TodoWebView in the generated file.
        TodoWebView.SetInvokeJavaScriptTarget<TodoJSInvokeTarget>(_jsInvokeTarget);

        BindingContext = this;
    }

    public async void SendUpdatedTasksToJS(IList<TodoItem> tasks)
    {
        Console.WriteLine($"Sending {tasks.Count} items to Angular...");
        
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await TodoWebView.InvokeJavaScriptAsync(
                methodName: "globalSetData",
                returnTypeJsonTypeInfo: TodoAppJsContext.Default.String, 
                paramValues: [tasks],
                paramJsonTypeInfos: [TodoAppJsContext.Default.IListTodoItem]
            );
        });
    }

    public async Task ShowToast(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show();
        });
    }

    private sealed class TodoJSInvokeTarget
    {
        private readonly MainPage _mainPage;
        private readonly TodoDatabase _todoDatabase;

        public TodoJSInvokeTarget(MainPage page, TodoDatabase db)
        {
            _mainPage = page;
            _todoDatabase = db;
        }

        // Angular calls this when it's ready
        public async void GetTodoItems()
        {
            await Task.Delay(500);
            Console.WriteLine("JS invoked GetTodoItems (C#). Fetching from DB...");
            var items = await _todoDatabase.GetItemsAsync();
            Console.WriteLine($"Fetched {items.Count} items. Sending to JS.");
            _mainPage.SendUpdatedTasksToJS(items);
        }

        public async void AddTodo(TodoItem todo)
        {
            Console.WriteLine($"JS invoked AddTodo (C#) for title: {todo.Title}");
            await _todoDatabase.AddItemAsync(todo);

            var updatedItems = await _todoDatabase.GetItemsAsync();
            _mainPage.SendUpdatedTasksToJS(updatedItems);
            Console.WriteLine("Todo added and updated items sent to JS.");
        }

        public async void RemoveTodoById(String todoId)
        {
            if (!int.TryParse(todoId, out int id))
            {
                Console.WriteLine($"Invalid ID received from JS: {todoId}");
                return;
            }
        {
            Console.WriteLine($"JS invoked RemoveTodoById (C#) for ID: {id}");
            await _todoDatabase.RemoveItem(id);
            var items = await _todoDatabase.GetItemsAsync();
            _mainPage.SendUpdatedTasksToJS(items);
            await _mainPage.ShowToast("Item deleted");
            Console.WriteLine("Todo removed and updated items sent to JS.");
        }
        }

        public async void UpdateDesc(TodoItem todo) 
        {
            Console.WriteLine($"JS invoked UpdateDesc (C#) for ID: {todo.Id}, Title: {todo.Title}");
            await _todoDatabase.UpdateItem(todo.Id, todo.Title, todo.Description, todo.IsCompleted);
            var items = await _todoDatabase.GetItemsAsync(); 
            _mainPage.SendUpdatedTasksToJS(items); 
            Console.WriteLine("Todo updated and updated items sent to JS.");
        }

        public async void RemoveTodo(TodoItem todo) 
        {
            Console.WriteLine($"JS invoked RemoveTodo (C#) for ID: {todo.Id}");
            await _todoDatabase.RemoveItem(todo.Id);
            var items = await _todoDatabase.GetItemsAsync();
            _mainPage.SendUpdatedTasksToJS(items);
            await _mainPage.ShowToast("Item deleted");
            Console.WriteLine("Todo removed and updated items sent to JS.");
        }
        
        // This is a cleaner way to handle toggle completion if you pass the full object.
        public async void ToggleComplete(TodoItem todo) 
        {
            Console.WriteLine($"JS invoked ToggleComplete (C#) for ID: {todo.Id}");
            // Assuming TodoDatabase.ToggleComplete updates the item's IsCompleted status
            await _todoDatabase.UpdateItem(todo.Id, todo.Title, todo.Description, todo.IsCompleted);
            var items = await _todoDatabase.GetItemsAsync();
            _mainPage.SendUpdatedTasksToJS(items);
            Console.WriteLine("Todo completion toggled and updated items sent to JS.");
        }


        public async void ClearCompleted()
        {
            Console.WriteLine("JS invoked ClearCompleted (C#).");
            await _todoDatabase.ClearCompleted();
            var items = await _todoDatabase.GetItemsAsync();
            _mainPage.SendUpdatedTasksToJS(items);
            Console.WriteLine("Completed todos cleared and updated items sent to JS.");
        }

        public async void ClearAll()
        {
            Console.WriteLine("JS invoked ClearAll (C#).");
           await _todoDatabase.ClearAll();
            var items = await _todoDatabase.GetItemsAsync();
            _mainPage.SendUpdatedTasksToJS(items);
            Console.WriteLine("All todos cleared and updated items sent to JS.");
        }
    }

    [JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]

    [JsonSerializable(typeof(IList<TodoItem>))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(TodoItem))]
    [JsonSerializable(typeof(int))] // Need to serialize primitive types if sent via DotNet
    internal partial class TodoAppJsContext : JsonSerializerContext
    {
    }
}