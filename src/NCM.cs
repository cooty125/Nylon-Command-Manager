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
    // ConvertArguments
    // CreateFromDictionary
    // CreateFromAnonymous
    //
    TARGS convertArguments( object arguments ) {
        if ( arguments is TARGS typed ) {
            return typed;
        }

        if ( arguments == null ) {
            return default( TARGS )!;
        }

        if ( arguments is Dictionary<string, object> dict ) {
            return createFromDictionary( dict );
        }

        if ( arguments.GetType( ).IsAnonymous( ) ) {
            return createFromAnonymous( arguments );
        }

        try {
            return ( TARGS )Convert.ChangeType( arguments, typeof( TARGS ) );
        }
        catch {
            throw new ArgumentException( $"Cannot convert {arguments.GetType( ).Name} to {typeof( TARGS ).Name}" );
        }
    }
    TARGS createFromDictionary( Dictionary<string, object> dictionary ) {
        var obj = Activator.CreateInstance<TARGS>( );
        var props = typeof( TARGS ).GetProperties( );

        foreach ( var prop in props ) {
            if ( dictionary.TryGetValue( prop.Name, out var value ) ) {

                if ( value != null && prop.PropertyType.IsInstanceOfType( value ) ) {
                    prop.SetValue( obj, value );
                }
                else if ( value != null ) {
                    var converted = Convert.ChangeType( value, prop.PropertyType );
                    prop.SetValue( obj, converted );
                }
            }
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
    static readonly Dictionary<string, ICOMMAND> commands = new Dictionary<string, ICOMMAND>( StringComparer.OrdinalIgnoreCase );
    static readonly Dictionary<string, List<string>> aliasMap = new( );

    //
    // Register
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
    }
    //
    // Exists
    // GetCagories
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
            foreach ( var kvp in commands ) {

                if ( kvp.Value == command && !aliasMap.ContainsKey( kvp.Key ) ) {
                    return kvp.Key;
                }
            }

            return command.Name;
        }

        return nameOrAlias;
    }
    internal static string[ ] GetAliases( string name ) {
        if ( commands.TryGetValue( name, out var command ) ) {
            foreach ( var kvp in commands ) {

                if ( kvp.Value == command && !aliasMap.ContainsKey( kvp.Key ) ) {
                    return (
                        aliasMap.TryGetValue( kvp.Key, out var aliases )
                        ? aliases.ToArray( )
                        : Array.Empty<string>( )
                    );
                }
            }
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
                    NCM.Register( new CMD_UIGeneric( cmdName, ( args ) => { } ) );
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
        NCM.Execute( commandName, new UIActionArgs {
            Sender = sender ?? control,
            EventArgs = eventArgs ?? EventArgs.Empty
        } );
    }

    //
    // Methods
    //
    internal static void Close( ) {
        uiForm?.Close( );
    }
}

// CMD_UIAction
internal class UIActionArgs {
    Dictionary<string, object> parameters = new( );

    public object Sender { get; set; } = null!;
    public EventArgs EventArgs { get; set; } = null!;

    public T Get<T>( string key, T defaultValue = default! ) {
        if ( parameters.TryGetValue( key, out var value ) && value is T typedValue ) {
            return typedValue;
        }

        return defaultValue;
    }
    public object Get( string key ) {
        return (parameters.TryGetValue( key, out var value ) ? value : null)!;
    }
    public void Set( string key, object value ) {
        parameters[ key ] = value;
    }
    public bool Has( string key ) {
        return parameters.ContainsKey( key );
    }
}
internal class CMD_UIGeneric : COMMAND<UIActionArgs>
{
    readonly Action<UIActionArgs> uiAction;

    public override string Name { get; }
    public override string Description => $"UI Action: {Name}";
    public override string Category => "UI";

    public CMD_UIGeneric( string name, Action<UIActionArgs> action = null! ) {
        Name = name;
        uiAction = ( action ?? (( args ) => { }) );
    }

    public override void Execute( UIActionArgs arguments ) {
        uiAction?.Invoke( arguments );
    }
    public override bool CanExecute( UIActionArgs args ) {
        return true;
    }
}
