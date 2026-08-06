# NCM - Nylon Command Manager

> **Valve-inspired command system for .NET applications**

NCM is a lightweight, powerful command manager that brings Source Engine-style console commands and UI binding to your .NET applications. Built with simplicity and flexibility in mind.

---

## ✨ Features

- 🔌 **Command Registry** – Register commands with names, aliases, and categories
- 🎯 **Typed Arguments** – Commands with strongly-typed arguments, supporting both classes and anonymous objects
- 🏷️ **Attributes** – Define commands with `[COMMAND]` attribute for automatic registration
- 🔄 **Automatic UI Binding** – Bind any WinForms event to commands with a single line of code
- 🏷️ **Aliases** – Multiple names for the same command (e.g., `Build`, `b`, `make`)
- 📂 **Categories** – Organize commands into logical groups
- 🔍 **Case-Insensitive** – `Build` and `build` are the same command
- 🧩 **Service Registry** – Built-in DI container for services
- 📦 **Command Queue** – Optional async command queue for game loops and batching

---

## 🚀 Quick Start

### 1. Define a Command with Attribute

```csharp
[COMMAND("change_text", "Changes text in textBox1", "UI Test", "ct")]
private static void ChangeTextCommand(object args)
{
    var form = UI.Form as Form1;
    if (form == null) return;

    if (args is UIActionArgs uiArgs && uiArgs.Sender is Button btn)
    {
        form.textBox1.Text = $"Hello from {btn.Text}!";
    }
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
    
    // Automatic binding of all buttons
    UI.BindAllButtons();
}
```

### 3. Execute It

```csharp
// From anywhere
NCM.Execute("change_text", new UIActionArgs { Sender = btn, EventArgs = e });

// With alias
NCM.Execute("ct", new UIActionArgs { Sender = btn, EventArgs = e });

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
        NCM.Execute("Build", new UIActionArgs { Sender = s, EventArgs = e });
    };
}
```

### Manual binding

```csharp
// Bind a single control
UI.Bind(btnBuild, "Click", "Build");
```

### Access UI from commands

```csharp
[COMMAND("set_status", "Sets status bar text", "UI")]
private static void StatusCommand(object args)
{
    var form = UI.Form as MainForm;
    if (form == null) return;

    if (args is UIActionArgs uiArgs)
    {
        form.statusLabel.Text = "Hello!";
    }
}
```

### Custom UI command with action

```csharp
// Register a command with custom action
NCM.Register(new CMD_UIGeneric("MyAction", (args) =>
{
    var sender = args.Sender;
    var eventArgs = args.EventArgs;
    // Do something...
}));
```

## 📦 Services (Dependency Injection)

```csharp
// Register services
SERVICES.Register(new AssetPipeline());
SERVICES.Register(new BuildService());

// Use them in commands
[COMMAND("build", "Builds all assets", "Pipeline", "b")]
private static void BuildCommand(object args)
{
    var pipeline = SERVICES.Get<AssetPipeline>();
    var build = SERVICES.Get<BuildService>();
    
    // Use services...
}
```

---

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
private static void HelpCommand(object args)
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
private static void ClearCommand(object args)
{
    Console.Clear();
}

[COMMAND("exit", "Exits the application", "System")]
private static void ExitCommand(object args)
{
    Application.Exit();
}
```

---

## 🙏 Inspiration

NCM is heavily inspired by the **Source Engine's command system** created by Valve Software. The concepts of CVARs, console commands, and UI binding have been adapted and modernized for .NET applications.
