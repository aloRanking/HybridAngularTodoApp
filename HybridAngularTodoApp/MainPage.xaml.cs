using System.Text.Json.Serialization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using HybridAngularTodoApp.Models;
using HybridAngularTodoApp.Services;

namespace HybridAngularTodoApp;

public partial class MainPage : ContentPage
{
    private readonly TodoDatabase _db;
    private TodoJSInvokeTarget _jsInvokeTarget; // Keep a reference to the target

    public MainPage(TodoDatabase db)
    {
        InitializeComponent();
        _db = db;

        _jsInvokeTarget = new TodoJSInvokeTarget(this, _db); // Instantiate it
        TodoWebView.SetInvokeJavaScriptTarget<TodoJSInvokeTarget>(_jsInvokeTarget);

        BindingContext = this;

        // NEW: Call GetTodoItems after the page is loaded/initialized to push initial data
        // You might want to do this in a Loaded event handler for the page or WebView
        this.Loaded += MainPage_Loaded;
    }

    private async void MainPage_Loaded(object sender, EventArgs e)
    {
        // This ensures the WebView is ready before attempting to get data
        // and push it to JS.
        Console.WriteLine("MainPage_Loaded event fired. Requesting initial data for Angular.");
        _jsInvokeTarget.GetTodoItems();
    }


    private async void SendUpdatedTasksToJS(IList<TodoItem> tasks)
    {
        Console.WriteLine($"C# SendUpdatedTasksToJS called. Sending {tasks.Count} items to JS.");
        _ = await MainThread.InvokeOnMainThreadAsync(async () =>
            _ = await TodoWebView.InvokeJavaScriptAsync<string>(
                methodName: "globalSetData",
                returnTypeJsonTypeInfo: TodoAppJsContext.Default.String, // String is fine, as it's not truly returning a useful string
                paramValues: [tasks],
                paramJsonTypeInfos: [TodoAppJsContext.Default.IListTodoItem]));
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

        public async void GetTodoItems()
        {
            Console.WriteLine("JS invoked GetTodoItems (C#). Fetching from DB...");
            var items = await _todoDatabase.GetItemsAsync();
            Console.WriteLine($"Fetched {items.Count} items. Sending to JS.");
            _mainPage.SendUpdatedTasksToJS(items);
        }

        public async void AddTodo(TodoItem todo)
        {
            Console.WriteLine($"JS invoked AddTodo (C#) for title: {todo.Title}");
            // Removed manual ID assignment as database should handle it.
            // var maxIndex = items.Any() ? items.Max(x => x.Id) : 0;
            // todo.Id = maxIndex + 1;

            await _todoDatabase.AddItemAsync(todo);

            var updatedItems = await _todoDatabase.GetItemsAsync();
            _mainPage.SendUpdatedTasksToJS(updatedItems); // Ensure this is uncommented and called
            Console.WriteLine("Todo added and updated items sent to JS.");
        }

        public async void RemoveTodoById(int id)
        {
            Console.WriteLine($"JS invoked RemoveTodoById (C#) for ID: {id}");
            await _todoDatabase.RemoveItem(id);
            var items = await _todoDatabase.GetItemsAsync();
            _mainPage.SendUpdatedTasksToJS(items);
            await _mainPage.ShowToast("Item deleted");
            Console.WriteLine("Todo removed and updated items sent to JS.");
        }

        public async void UpdateDesc(int id, string title, string desc, bool IsCompleted) // Fixed typo in 'title'
        {
            Console.WriteLine($"JS invoked UpdateDesc (C#) for ID: {id}, Title: {title}");
            await _todoDatabase.UpdateItem(id, title, desc, IsCompleted);
            var items = await _todoDatabase.GetItemsAsync(); // Fetch updated items
            _mainPage.SendUpdatedTasksToJS(items); // Send them to JS
            Console.WriteLine("Todo updated and updated items sent to JS.");
        }

        public async void ToggleComplete(int id) // Changed parameter name from index to id for clarity
        {
            Console.WriteLine($"JS invoked ToggleComplete (C#) for ID: {id}");
            await _todoDatabase.ToggleComplete(id); // Assuming TodoDatabase.ToggleComplete takes ID
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
    }

    [JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]

    [JsonSerializable(typeof(IList<TodoItem>))]
    [JsonSerializable(typeof(string))] // Still needed for the InvokeJavaScriptAsync signature
    [JsonSerializable(typeof(TodoItem))] // Needed for AddTodo to deserialize correctly
    internal partial class TodoAppJsContext : JsonSerializerContext
    {
    }
}