# NCM - Nylon Command Manager

> **Valve-inspired command system for .NET applications**

NCM is a lightweight, powerful command manager that brings Source Engine-style console commands and UI binding to your .NET applications. Built with simplicity and flexibility in mind.

---

## ✨ Features

- 🔌 **Command Registry** – Register commands with names, aliases, and categories
- 🎯 **Typed Arguments** – Commands with strongly-typed arguments, supporting both classes and anonymous objects
- 🔄 **Automatic UI Binding** – Bind any WinForms event to commands with a single line of code
- 🏷️ **Aliases** – Multiple names for the same command (e.g., `Build`, `b`, `make`)
- 📂 **Categories** – Organize commands into logical groups
- 🔍 **Case-Insensitive** – `Build` and `build` are the same command
- 🧩 **Service Registry** – Built-in DI container for services

---

## 🚀 Quick Start

### 1. Define a Command

```csharp
internal class CMD_Build : COMMAND<BuildArgs>
{
    public override string Name => "Build";
    public override string[] Aliases => new[] { "b", "make" };
    public override string Description => "Builds all assets";
    public override string Category => "Pipeline";

    public override void Execute(BuildArgs args)
    {
        // Build logic here
        Console.WriteLine($"Building {args.Target}...");
    }
}

internal class BuildArgs
{
    public string Target { get; set; } = "Release";
    public bool Verbose { get; set; } = false;
}
```

### 2. Register the Command

```csharp
NCM.Register(new CMD_Build());
```

### 3. Execute It

```csharp
// With typed arguments
NCM.Execute("Build", new BuildArgs { Target = "Debug", Verbose = true });

// With anonymous object (auto-mapped!)
NCM.Execute("Build", new { Target = "Debug", Verbose = true });

// With dictionary
NCM.Execute("Build", new Dictionary<string, object> { { "Target", "Debug" } });

// Without arguments
NCM.Execute("Build");
```

---

## 🖥️ UI Binding (WinForms)

### Automatic binding of all controls

```csharp
public partial class MainForm : Form
{
    public MainForm()
    {
        InitializeComponent();
        
        // Initialize UI
        UI.Initialize(this);
        
        // Register commands
        NCM.Register(new CMD_Build());
        NCM.Register(new CMD_Refresh());
        NCM.Register(new CMD_Save());
        
        // Bind all Click events to commands
        UI.BindEventAt("Click");
        
        // Bind all TextChanged events
        UI.BindEventAt("TextChanged");
        
        // Bind specific controls with filter
        UI.BindEventAt("Click", 
            c => c.Name.StartsWith("btn"));
    }
}
```

### Manual binding

```csharp
// Bind a single control
UI.Bind(btnBuild, "Click", "Build");
```

### Access UI from commands

```csharp
internal class CMD_Status : COMMAND<StatusArgs>
{
    public override string Name => "Status";
    public override string Description => "Sets status bar text";
    public override string Category => "UI";

    public override void Execute(StatusArgs args)
    {
        // Direct access to UI controls
        var form = UI.Form as MainForm;
        form.statusLabel.Text = args.Text;
        form.statusLabel.ForeColor = args.IsError ? Color.Red : Color.White;
    }
}
```

---

## 📦 Services (Dependency Injection)

```csharp
// Register services
SERVICES.Register(new AssetPipeline());
SERVICES.Register(new BuildService());

// Use them in commands
internal class CMD_Build : COMMAND<BuildArgs>
{
    public override void Execute(BuildArgs args)
    {
        var pipeline = SERVICES.Get<AssetPipeline>();
        var build = SERVICES.Get<BuildService>();
        
        // Use services...
    }
}
```

---

## 🔧 Advanced

### Event binding with context

```csharp
// When binding events, the command receives UIActionArgs
internal class CMD_SelectAsset : COMMAND<UIActionArgs>
{
    public override string Name => "SelectAsset";
    public override string Category => "Assets";

    public override void Execute(UIActionArgs args)
    {
        if (args.EventArgs is TreeViewEventArgs treeArgs)
        {
            var node = treeArgs.Node;
            // Handle selection...
        }
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

---

## 🧪 Example Commands

```csharp
// System commands
NCM.Register(new CMD_Help());      // Lists all commands
NCM.Register(new CMD_Clear());     // Clears console
NCM.Register(new CMD_Exit());      // Exits application

// Project commands
NCM.Register(new CMD_OpenProject());
NCM.Register(new CMD_SaveProject());
NCM.Register(new CMD_CloseProject());

// Build commands
NCM.Register(new CMD_Build());
NCM.Register(new CMD_Clean());
NCM.Register(new CMD_Rebuild());
```

---

## 🙏 Inspiration

NCM is heavily inspired by the **Source Engine's command system** created by Valve Software. The concepts of CVARs, console commands, and UI binding have been adapted and modernized for .NET applications.
