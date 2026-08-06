# NCM - Nylon Command Manager

> **Valve-inspired command system for .NET applications**

NCM is a lightweight, powerful command manager that brings Source Engine-style console commands and UI binding to your .NET applications. Built with simplicity and flexibility in mind.

---

## ✨ Features

- 🔌 **Command Registry** – Register commands with names, aliases, and categories
- 🏷️ **Attributes** – Define commands with `[COMMAND]` attribute for automatic registration
- 🔄 **Automatic UI Binding** – Bind any WinForms event to commands with a single line of code
- 🏷️ **Aliases** – Multiple names for the same command (e.g., `Build`, `b`, `make`)
- 📂 **Categories** – Organize commands into logical groups
- 🔍 **Case-Insensitive** – `Build` and `build` are the same command
- 🧩 **Service Registry** – Built-in DI container for services
- 📦 **Command Queue** – Optional async command queue for game loops and batching
- 🎯 **Smart Argument Conversion** – Commands can accept any parameter type (string, int, Button, EventArgs, etc.)

---

## 🚀 Quick Start

### 1. Define a Command with Attribute

```csharp
[COMMAND("change_text", "Changes text in textBox1", "UI Test", "ct")]
private static void ChangeTextCommand(EventArgs args)
{
    var form = UI.Form as Form1;
    if (form == null) return;

    // Get the button from the event args (if needed)
    // Or use the sender via UI.Form
    form.textBox1.Text = "Hello from NCM!";
}
```

### 2. Automatic Registration

```csharp
public Form1()
{
    InitializeComponent();
    UI.Initialize(this);
    
    // Automatically registers all [COMMAND] methods
    NCM.RegisterAttributes();
}
```

### 3. Execute It

```csharp
// From anywhere
NCM.Execute("change_text", eventArgs);

// With alias
NCM.Execute("ct", eventArgs);

// Without arguments
NCM.Execute("Build");
```

---

## 🖥️ UI Binding (WinForms)

### Automatic binding of all controls

```csharp
public Form1()
{
    InitializeComponent();
    UI.Initialize(this);
    NCM.RegisterAttributes();

    UI.Bind(btnBuild, "Click", "Build");

    // Or manually
    btnBuild.Click += (s, e) =>
    {
        NCM.Execute("Build", e); // e is EventArgs
    };
}
```

### Access UI from commands

```csharp
[COMMAND("set_status", "Sets status bar text", "UI")]
private static void StatusCommand(EventArgs args)
{
    var form = UI.Form as MainForm;
    if (form == null) return;

    form.statusLabel.Text = "Clicked!";
}
```

## 📦 Services (Dependency Injection)

```csharp
// Register services
SERVICES.Register(new AssetPipeline());
SERVICES.Register(new BuildService());

// Use them in commands
[COMMAND("build", "Builds all assets", "Pipeline", "b")]
private static void BuildCommand()
{
    var pipeline = SERVICES.Get<AssetPipeline>();
    var build = SERVICES.Get<BuildService>();
    
    // Use services...
}
```

---

## 🎯 Smart Argument Conversion
Commands can accept any parameter type. NCM automatically converts the argument to the expected type.

```csharp
// No parameter
[COMMAND("build")]
private static void BuildCommand() { }

// String parameter (from console)
[COMMAND("echo")]
private static void EchoCommand(string text) { }

// EventArgs parameter (from UI)
[COMMAND("change_text")]
private static void ChangeTextCommand(EventArgs args) { }

// TreeViewEventArgs parameter (from TreeView)
[COMMAND("tree_select")]
private static void SelectAssetCommand(TreeViewEventArgs args) { }

// Integer parameter
[COMMAND("set_value")]
private static void SetValueCommand(int value) { }
```

## 🔧 Command Queue (Async / Game Loop)

### Event binding with context

```csharp
// Add commands to queue (they won't execute immediately)
NCM.Enqueue("Build");
NCM.Enqueue("Refresh");
NCM.Enqueue("Export");

// Process all commands in queue
NCM.ProcessQueue();
```

### Automatic queue processing in game loop

```csharp
// In your game loop
while (running)
{
    NCM.ProcessQueue();  // Process queued commands every frame
    Update();
    Render();
}
```

### UI integration

```csharp
// Process queue on UI idle
Application.Idle += (s, e) => NCM.ProcessQueue();

```

## 🧪 Example Commands

```csharp
// System commands
[COMMAND("help", "Shows all available commands", "System")]
private static void HelpCommand()
{
    var sb = new StringBuilder();
    sb.AppendLine("Available commands:");
    foreach (var category in NCM.GetCategories())
    {
        sb.AppendLine($"  [{category}]");
        foreach (var cmd in NCM.GetByCategory(category))
        {
            var aliases = NCM.GetAliases(cmd.Name);
            var aliasText = aliases.Length > 0 ? $" (aliases: {string.Join(", ", aliases)})" : "";
            sb.AppendLine($"    {cmd.Name,-20} - {cmd.Description}{aliasText}");
        }
    }
    Console.WriteLine(sb.ToString());
}

[COMMAND("clear", "Clears the console", "System")]
private static void ClearCommand()
{
    Console.Clear();
}

[COMMAND("exit", "Exits the application", "System")]
private static void ExitCommand()
{
    Application.Exit();
}

// UI commands
[COMMAND("change_text", "Changes text in textBox1", "UI Test", "ct")]
private static void ChangeTextCommand(EventArgs args)
{
    var form = UI.Form as Form1;
    if (form == null) return;

    form.textBox1.Text = "Hello from NCM!";
}

// TreeView command (receives TreeViewEventArgs directly)
[COMMAND("tree_select", "Selects a node in the tree", "UI")]
private static void SelectAssetCommand(TreeViewEventArgs args)
{
    var node = args.Node;
    Console.WriteLine($"Selected: {node.Text}");
}
```

---

## 🙏 Inspiration

NCM is heavily inspired by the **Source Engine's command system** created by Valve Software. The concepts of CVARs, console commands, and UI binding have been adapted and modernized for .NET applications.
