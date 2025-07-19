using System.Text.Json.Serialization;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using HybridAngularTodoApp.Models;
using HybridAngularTodoApp.Services;

namespace HybridAngularTodoApp;

public partial class MainPage : ContentPage
{
    private readonly TodoDatabase _db;
    private TodoJSInvokeTarget _jsInvokeTarget;

    public MainPage(TodoDatabase db)
    {
        InitializeComponent();
        _db = db;

        _jsInvokeTarget = new TodoJSInvokeTarget(this, _db);
        TodoWebView.SetInvokeJavaScriptTarget(_jsInvokeTarget);

        // Set up the hybrid webview with proper event handlers
        TodoWebView.Loaded += OnWebViewLoaded;
        
        BindingContext = this;
    }

    private async void OnWebViewLoaded(object sender, EventArgs e)
    {
        Console.WriteLine("WebView loaded. Waiting for Angular to be ready...");
        
        // Wait a bit for Angular to initialize
        await Task.Delay(2000);
        
        // Send initial data to Angular
        await SendInitialDataToJS();
    }

   private async Task SendInitialDataToJS()
    {
        try
        {
            Console.WriteLine("Sending initial data to Angular...");
            var items = await _db.GetItemsAsync();
            await SendUpdatedTasksToJS(items);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending initial data: {ex.Message}");
        }
    }

    // private async Task SendUpdatedTasksToJS(IList<TodoItem> tasks)
    // {
    //     try
    //     {
    //         Console.WriteLine($"C# SendUpdatedTasksToJS called. Sending {tasks.Count} items to JS.");
            
    //         await MainThread.InvokeOnMainThreadAsync(async () =>
    //         {
    //             await TodoWebView.InvokeJavaScriptAsync(
    //                 $"if (window.globalSetData) {{ window.globalSetData({System.Text.Json.JsonSerializer.Serialize(tasks, TodoAppJsContext.Default.IListTodoItem)}); }}"
    //             );
    //         });
    //     }
    //     catch (Exception ex)
    //     {
    //         Console.WriteLine($"Error sending tasks to JS: {ex.Message}");
    //     }
    // }


    private async Task SendUpdatedTasksToJS(IList<TodoItem> tasks)
    {
        try
        {
            Console.WriteLine($"C# SendUpdatedTasksToJS called. Sending {tasks.Count} items to JS.");
            _ = await MainThread.InvokeOnMainThreadAsync(async () =>
            _ = await TodoWebView.InvokeJavaScriptAsync<string>(
                methodName: "globalSetData",
                returnTypeJsonTypeInfo: TodoAppJsContext.Default.String,
                paramValues: [tasks],
                paramJsonTypeInfos: [TodoAppJsContext.Default.IListTodoItem]));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending tasks to JS: {ex.Message}");
        }
    }


    public async Task ShowToast(string message)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var toast = Toast.Make(message, ToastDuration.Short, 14);
            await toast.Show();
        });
    }

    // JavaScript callable methods - these will be called from Angular via HybridWebView.InvokeMethod
    public class TodoJSInvokeTarget
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
            await _mainPage.SendUpdatedTasksToJS(items);
        }

        public async void AddTodo(TodoItem todo)
        {
            Console.WriteLine($"JS invoked AddTodo (C#) for title: {todo.Title}");
            
            await _todoDatabase.AddItemAsync(todo);
            
            var updatedItems = await _todoDatabase.GetItemsAsync();
            await _mainPage.SendUpdatedTasksToJS(updatedItems);
            Console.WriteLine("Todo added and updated items sent to JS.");
            
            await _mainPage.ShowToast("Todo added successfully!");
        }

        public async void RemoveTodoById(int id)
        {
            Console.WriteLine($"JS invoked RemoveTodoById (C#) for ID: {id}");
            
            await _todoDatabase.RemoveItem(id);
            var items = await _todoDatabase.GetItemsAsync();
            await _mainPage.SendUpdatedTasksToJS(items);
            await _mainPage.ShowToast("Item deleted");
            Console.WriteLine("Todo removed and updated items sent to JS.");
        }

        public async void UpdateDesc(int id, string title, string desc, bool isCompleted)
        {
            Console.WriteLine($"JS invoked UpdateDesc (C#) for ID: {id}, Title: {title}");
            
            await _todoDatabase.UpdateItem(id, title, desc, isCompleted);
            var items = await _todoDatabase.GetItemsAsync();
            await _mainPage.SendUpdatedTasksToJS(items);
            Console.WriteLine("Todo updated and updated items sent to JS.");
            
            await _mainPage.ShowToast("Todo updated successfully!");
        }

        public async void ToggleComplete(int id)
        {
            Console.WriteLine($"JS invoked ToggleComplete (C#) for ID: {id}");
            
            await _todoDatabase.ToggleComplete(id);
            var items = await _todoDatabase.GetItemsAsync();
            await _mainPage.SendUpdatedTasksToJS(items);
            Console.WriteLine("Todo completion toggled and updated items sent to JS.");
        }

        public async void ClearCompleted()
        {
            Console.WriteLine("JS invoked ClearCompleted (C#).");
            
            await _todoDatabase.ClearCompleted();
            var items = await _todoDatabase.GetItemsAsync();
            await _mainPage.SendUpdatedTasksToJS(items);
            Console.WriteLine("Completed todos cleared and updated items sent to JS.");
            
            await _mainPage.ShowToast("Completed todos cleared!");
        }
    }

    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(IList<TodoItem>))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(TodoItem))]
    internal partial class TodoAppJsContext : JsonSerializerContext
    {
    }
}