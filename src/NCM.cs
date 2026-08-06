/* 
 * NCM
 * ===============================================================
 * FileName: NCM.cs
 * Project: Nylon Command Manager
 * Location: ./
 * ---------------------------------------------------------------
 * Description: Nylon generic command manager
 * ---------------------------------------------------------------
 * This document is distributed under GNU General Public License.
 * Copyright © David Kutnar 2026 - All rights reserved.
 * ===============================================================
 */

using System.Reflection;
using System.Linq.Expressions;

// ICOMMAND
internal interface ICOMMAND
{
    string Name { get; }
    string[ ] Aliases { get; }
    string Description { get; }
    string Category { get; }

    void Execute( object args );
    bool CanExecute( object args );
}

// ArgumentTypeHelper
internal static class ArgumentTypeHelper
{
    internal static bool IsAnonymous( this Type type ) {
        return (
            type.IsGenericType &&
            type.Name.Contains( "AnonymousType" ) &&
            (type.Name.StartsWith( "<>" ) || type.Name.StartsWith( "VB$Anonymous" ))
        );
    }
}

// COMMAND
internal abstract class COMMAND<TARGS> : ICOMMAND
{
    public abstract string Name { get; }
    public virtual string[ ] Aliases => Array.Empty<string>( );
    public abstract string Description { get; }
    public virtual string Category => "General";

    public abstract void Execute( TARGS args );
    public virtual bool CanExecute( TARGS args ) => true;

    // ICOMMAND
    // Execute
    // CanExecute
    //
    void ICOMMAND.Execute( object arguments ) {
        var typedArguments = convertArguments( arguments );
        Execute( typedArguments );
    }
    bool ICOMMAND.CanExecute( object arguments ) {
        try {
            var typedArguments = convertArguments( arguments );
            return CanExecute( typedArguments );
        }
        catch {
            return false;
        }
    }

    // TARGS
    // ConvertValue
    // ConvertArguments
    // CreateFromDictionary
    // CreateFromAnonymous
    //
    static object? convertValue( object? value, Type targetType ) {
        if ( value == null ) {
            if ( !targetType.IsValueType ||
                Nullable.GetUnderlyingType( targetType ) != null ) {
                return null;
            }

            throw new ArgumentException( $"Cannot assign null to '{targetType.Name}'." );
        }

        var nullableType = Nullable.GetUnderlyingType( targetType );
        var actualType = nullableType ?? targetType;

        if ( actualType.IsInstanceOfType( value ) ) {
            return value;
        }

        if ( actualType.IsEnum ) {
            return (
                value is string text
                ? Enum.Parse( actualType, text, true )
                : Enum.ToObject( actualType, value )
            );
        }

        return Convert.ChangeType( value, actualType );
    }
    TARGS convertArguments( object arguments ) {
        if ( arguments is TARGS typed ) {
            return typed;
        }

        if ( arguments == null ) {
            return default( TARGS )!;
        }

        if ( arguments is IDictionary<string, object> dict ) {
            return createFromDictionary( dict );
        }

        if ( arguments.GetType( ).IsAnonymous( ) ) {
            return createFromAnonymous( arguments );
        }

        try {
            return ( TARGS )Convert.ChangeType( arguments, typeof( TARGS ) );
        }
        catch ( Exception exception ) {
            throw new ArgumentException( $"Cannot convert {arguments.GetType( ).Name} " + $"to {typeof( TARGS ).Name}.", exception );
        }
    }
    TARGS createFromDictionary( IDictionary<string, object> dictionary ) {
        var obj = Activator.CreateInstance<TARGS>( );
        var props = typeof( TARGS ).GetProperties( );

        foreach ( var prop in props ) {
            if ( !prop.CanWrite ) {
                continue;
            }

            var item = dictionary.FirstOrDefault(
                x => string.Equals(
                    x.Key,
                    prop.Name,
                    StringComparison.OrdinalIgnoreCase
                )
            );

            if ( item.Key == null ) {
                continue;
            }

            var converted = convertValue( item.Value, prop.PropertyType );
            prop.SetValue( obj, converted );
        }

        return obj;
    }
    TARGS createFromAnonymous( object anonymous ) {
        var dictionary = new Dictionary<string, object>( );

        foreach ( var property in anonymous.GetType( ).GetProperties( ) ) {
            dictionary[ property.Name ] = property.GetValue( anonymous )!;
        }

        return createFromDictionary( dictionary );
    }
}

// NCM
internal static class NCM
{
    static bool initialized = false;

    static readonly Dictionary<string, ICOMMAND> commands = new Dictionary<string, ICOMMAND>( StringComparer.OrdinalIgnoreCase );
    static readonly Dictionary<string, List<string>> aliasMap = new( StringComparer.OrdinalIgnoreCase );
    static Queue<Action> commandQueue = new( );

    public static int QueueCount => commandQueue.Count;

    //
    // Register
    // RegisterAttributes
    // RegisterAssembly
    //
    internal static void Register<TARGS>( COMMAND<TARGS> command ) {
        if ( string.IsNullOrWhiteSpace( command.Name ) ) {
            throw new ArgumentException( "Command name cannot be empty." );
        }

        if ( commands.ContainsKey( command.Name ) ) {
            throw new Exception( $"Command '{command.Name}' already registered!" );
        }

        var aliases = ( command.Aliases ?? Array.Empty<string>( ) );
        var checkedAliases = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

        foreach ( var alias in aliases ) {
            if ( string.IsNullOrWhiteSpace( alias ) ) {
                throw new ArgumentException( $"Alias for command '{command.Name}' cannot be empty." );
            }

            if ( string.Equals( alias, command.Name, StringComparison.OrdinalIgnoreCase ) ) {
                throw new ArgumentException( $"Alias '{alias}' cannot be the command name." );
            }

            if ( !checkedAliases.Add( alias ) ) {
                throw new Exception( $"Alias '{alias}' is registered more than once!" );
            }

            if ( commands.ContainsKey( alias ) ) {
                throw new Exception( $"Alias '{alias}' already registered as command or alias!" );
            }
        }

        commands[ command.Name ] = command;

        foreach ( var alias in aliases ) {
            commands[ alias ] = command;
        }

        if ( aliases.Length > 0 ) {
            aliasMap[ command.Name ] = aliases.ToList( );
        }
    }
    internal static void RegisterAttributes( ) {
        if ( initialized ) {
            return;
        }

        initialized = true;

        foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies( ) ) {
            RegisterAssembly( assembly );
        }
    }
    internal static void RegisterAssembly( Assembly assembly ) {
        foreach ( var type in assembly.GetTypes( ) ) {

            foreach ( var method in type.GetMethods( BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public ) ) {
                var attr = method.GetCustomAttribute<COMMANDAttribute>( );
                if ( attr == null ) {
                    continue;
                }

                var parameters = method.GetParameters( );

                if ( parameters.Length > 1 ) {
                    throw new InvalidOperationException(
                        $"Method '{method.DeclaringType?.FullName}.{method.Name}' " +
                        "can have zero or one parameter."
                    );
                }

                Action<object> action = args => {
                    if ( parameters.Length == 0 ) {
                        method.Invoke( null, null );
                        return;
                    }

                    var parameterType = parameters[ 0 ].ParameterType;
                    object? argument = args;

                    if ( argument != null && !parameterType.IsInstanceOfType( argument ) ) {
                        try {
                            argument = Convert.ChangeType( argument, parameterType );
                        }
                        catch {
                            if ( parameterType == typeof( string ) ) {
                                argument = argument.ToString( );
                            }
                        }
                    }

                    method.Invoke( null, new[ ] { argument } );
                };

                var command = new CMD( attr.Name, action, attr.Description, attr.Category );

                foreach ( var alias in attr.Aliases ) {
                    command.AddAlias( alias );
                }

                Register( command );
            }
        }
    }
    //
    // Execute
    //
    internal static void Execute<TARGS>( string name, TARGS arguments ) {
        if ( !commands.TryGetValue( name, out var command ) ) {
            throw new Exception( $"Command '{name}' not found!" );
        }

        if ( command is COMMAND<TARGS> typedCmd ) {
            typedCmd.Execute( arguments );
        }
        else {
            // Dynamic call
            command.Execute( arguments! );
        }
    }
    internal static void Execute( string name, object arguments = null! ) {
        if ( !commands.TryGetValue( name, out var command ) ) {
            throw new Exception( $"Command '{name}' not found!" );
        }

        command.Execute( arguments );
    }
    //
    // Can Execute
    //
    internal static bool CanExecute<TARGS>( string name, TARGS arguments ) {
        if ( !commands.TryGetValue( name, out var command ) ) {
            return false;
        }

        if ( command is COMMAND<TARGS> typedCommand ) {
            return typedCommand.CanExecute( arguments );
        }

        return command.CanExecute( arguments! );
    }
    //
    // Clear
    //
    internal static void Clear( ) {
        commands.Clear( );
        aliasMap.Clear( );

        initialized = false;
    }
    //
    // Enqueue
    //
    internal static void Enqueue( string name, object args = null! ) {
        commandQueue.Enqueue( ( ) => Execute( name, args ) );
    }
    //
    // ProcessQueue
    //
    internal static void ProcessQueue( ) {
        while ( commandQueue.Count > 0 ) {
            var action = commandQueue.Dequeue( );
            action( );
        }
    }
    //
    // ClearQueue
    //
    internal static void ClearQueue( ) {
        commandQueue.Clear( );
    }
    //
    // Exists
    // GetCategories
    // GetByCategory
    // GetPrimaryName
    // GetAliases
    //
    internal static bool Exists( string name ) {
        return commands.ContainsKey( name );
    }
    internal static IEnumerable<string> GetCategories( ) {
        return commands.Values
            .Distinct( )
            .Select( c => c.Category )
            .Distinct( )
            .OrderBy( c => c );
    }
    internal static IEnumerable<ICOMMAND> GetByCategory( string category ) {
        return commands.Values
            .Distinct( )
            .Where( c => string.Equals( c.Category, category, StringComparison.OrdinalIgnoreCase ) )
            .OrderBy( c => c.Name );
    }
    internal static string GetPrimaryName( string nameOrAlias ) {
        if ( commands.TryGetValue( nameOrAlias, out var command ) ) {
            return command.Name;
        }

        return nameOrAlias;
    }
    internal static string[ ] GetAliases( string name ) {
        if ( commands.TryGetValue( name, out var command ) && aliasMap.TryGetValue( command.Name, out var aliases ) ) {
            return aliases.ToArray( );
        }

        return Array.Empty<string>( );
    }
}

// SERVICES
internal static class SERVICES
{
    static Dictionary<Type, object> services = new( );

    //
    // Register
    //
    internal static void Register<T>( T service ) where T : class {
        services[ typeof( T ) ] = service;
    }
    //
    // Get
    // Has
    // Clear
    //
    internal static T Get<T>( ) where T : class {
        if ( services.TryGetValue( typeof( T ), out var service ) ) {
            return (service as T)!;
        }

        throw new Exception( $"Service '{typeof( T ).Name}' not registered!" );
    }
    internal static bool Has<T>( ) where T : class {
        return services.ContainsKey( typeof( T ) );
    }
    internal static void Clear( ) {
        services.Clear( );
    }
}

// UI
internal static class UI
{
    static readonly HashSet<(Control Control, string EventName)> boundControls = new( );
    static Form uiForm = null!;
    internal static Form Form => uiForm;

    //
    // Initialize
    //
    internal static void Initialize( Form form ) {
        if ( form == null ) {
            throw new ArgumentNullException( nameof( form ) );
        }

        uiForm = form;
    }
    //
    // Bind
    // BindEventAt
    //
    internal static void Bind( Control control, string eventName, string commandName ) {
        if ( control == null ) {
            return;
        }

        var key = (control, eventName);
        if ( boundControls.Contains( key ) ) {
            return;
        }

        var eventInfo = control.GetType( ).GetEvent( eventName );
        if ( eventInfo == null ) {
            throw new Exception( $"Event '{eventName}' not found on {control.Name}" );
        }

        if ( !NCM.Exists( commandName ) ) {
            throw new InvalidOperationException( $"Cannot bind event '{eventName}' on control '{control.Name}'. " + $"Command '{commandName}' is not registered." );
            //NCM.Register( new CMD_UIAction( commandName, ( args ) => { } ) );
        }

        var handler = createEventHandler( eventInfo, control, commandName );
        eventInfo.AddEventHandler( control, handler );

        boundControls.Add( key );
    }
    internal static void BindEventAt( string eventName, Func<Control, bool> filter = null!, string prefix = "" ) {
        if ( uiForm == null ) {
            return;
        }

        foreach ( Control control in GetAllControls( uiForm ) ) {
            if ( filter != null && !filter( control ) ) {
                continue;
            }

            if ( control.GetType( ).GetEvent( eventName ) != null ) {
                var cmdName = $"{prefix}{(string.IsNullOrEmpty( prefix ) ? "" : "_")}{control.Name}_{eventName}";

                if ( !NCM.Exists( cmdName ) ) {
                    NCM.Register( new CMD( cmdName, ( args ) => { } ) );
                }

                Bind( control, eventName, cmdName );
            }
        }
    }
    //
    // GetAllControls
    // GetCommandsForControl
    //
    internal static IEnumerable<Control> GetAllControls( Control parent ) {
        if ( parent == null ) { 
            return Enumerable.Empty<Control>( );
        }

        var controls = new List<Control>( );

        foreach ( Control child in parent.Controls ) {
            controls.Add( child );
            controls.AddRange( GetAllControls( child ) );
        }

        return controls;
    }
    internal static IEnumerable<string> GetCommandsForControl( Control control, string prefix = "" ) {
        var events = control.GetType( ).GetEvents( );

        foreach ( var ev in events ) {
            var cmdName = $"{prefix}{(string.IsNullOrEmpty( prefix ) ? "" : "_")}{control.Name}_{ev.Name}";

            if ( NCM.Exists( cmdName ) ) {
                yield return cmdName;
            }
        }
    }

    //
    // CreateEventHandler
    // ExecuteBoundCommand
    //
    static Delegate createEventHandler( EventInfo eventInfo, Control control, string commandName ) {
        var handlerType = ( eventInfo.EventHandlerType
            ?? throw new Exception( $"Event '{eventInfo.Name}' has no handler type." ) );
        var invoke = ( handlerType.GetMethod( "Invoke" )
            ?? throw new Exception( $"Event '{eventInfo.Name}' has no invoke method." ) );
        var parameters = invoke.GetParameters( );

        if ( parameters.Length != 2 || !typeof( EventArgs ).IsAssignableFrom( parameters[ 1 ].ParameterType ) ) {
            throw new NotSupportedException( $"Event '{eventInfo.Name}' does not use a standard WinForms event signature." );
        }

        var sender = Expression.Parameter( parameters[ 0 ].ParameterType, "sender" );
        var eventArgs = Expression.Parameter( parameters[ 1 ].ParameterType, "eventArgs" );
        var execute = typeof( UI ).GetMethod( nameof( executeBoundCommand ), BindingFlags.NonPublic | BindingFlags.Static )!;
        var body = Expression.Call(
            execute,
            Expression.Constant( control ),
            Expression.Constant( commandName ),
            Expression.Convert( sender, typeof( object ) ),
            Expression.Convert( eventArgs, typeof( EventArgs ) )
        );

        return Expression.Lambda( handlerType, body, sender, eventArgs ).Compile( );
    }
    static void executeBoundCommand( Control control, string commandName, object sender, EventArgs eventArgs ) {
        NCM.Execute( commandName, eventArgs ?? EventArgs.Empty );
    }

    //
    // Methods
    //
    internal static void Close( ) {
        uiForm?.Close( );
    }
}

// CMD
internal class CMD : COMMAND<object>
{
    private readonly Action<object> genericAction;
    private readonly List<string> aliases = new( );

    public override string Name { get; }
    public override string[ ] Aliases => aliases.ToArray( );
    public override string Description { get; }
    public override string Category { get; }

    public CMD( string name, Action<object> action, string description = "", string category = "General" ) {
        Name = name;
        genericAction = ( action ?? (( args ) => { } ) );
        Description = string.IsNullOrEmpty( description ) ? $"Command: {name}" : description;
        Category = category;
    }

    public override void Execute( object args ) {
        genericAction?.Invoke( args );
    }

    public void AddAlias( string alias ) {
        if ( !string.IsNullOrEmpty( alias ) && !aliases.Contains( alias ) ) {
            aliases.Add( alias );
        }
    }
}

[AttributeUsage( AttributeTargets.Method, AllowMultiple = false )]
internal class COMMANDAttribute : Attribute
{
    public string Name { get; }
    public string Description { get; }
    public string[ ] Aliases { get; }
    public string Category { get; }

    public COMMANDAttribute( string name, string description = "", string category = "General", params string[ ] aliases ) {
        Name = name;
        Description = description;
        Category = category;
        Aliases = ( aliases ?? Array.Empty<string>( ) );
    }
}
