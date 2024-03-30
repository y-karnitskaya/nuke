using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Serilog.Events;

namespace Nuke.Tooling;

public interface IToolWithCustomToolPath
{
    abstract string GetToolPath(ToolOptions options);
}

public interface IToolOptionsWithCustomToolPath
{
    string GetToolPath();
}

[PublicAPI]
public partial class ToolOptions : Options
{
    internal string GetToolPath()
    {
        if (ProcessToolPath != null)
            return ProcessToolPath;

        var optionsType = GetType();
        var toolType = optionsType.GetCustomAttribute<CommandAttribute>().NotNull().Type;

        var environmentVariable = toolType.Name.TrimEnd("Tasks").ToUpperInvariant() + "_EXE";
        if (ToolPathResolver.TryGetEnvironmentExecutable(environmentVariable) is { } environmentExecutable)
            return environmentExecutable;

        // TODO: refactor to (abstract) interfaces
        if (this is IToolOptionsWithCustomToolPath optionsCustomProvider)
            return optionsCustomProvider.GetToolPath();

        if (toolType.CreateInstance() is IToolWithCustomToolPath toolCustomProvider)
            return toolCustomProvider.GetToolPath(this);

        return toolType.GetCustomAttribute<ToolAttribute>().NotNull().GetToolPath(this);
    }

    internal IEnumerable<string> GetArguments()
    {
        var optionsType = GetType();
        var commandAttribute = optionsType.GetCustomAttribute<CommandAttribute>().NotNull();
        var toolAttribute = commandAttribute.Type.GetCustomAttribute<ToolAttribute>().NotNull();
        var escapeMethod = CreateEscape();

        if (toolAttribute.Arguments != null)
            yield return toolAttribute.Arguments;

        if (commandAttribute.Arguments != null)
            yield return commandAttribute.Arguments;

        var arguments = InternalOptions.Properties()
            .Select(x => (Token: x.Value, Property: GetType().GetProperty(x.Name).NotNull()))
            .Select(x => (x.Token, x.Property, Attribute: x.Property.GetCustomAttribute<ArgumentAttribute>()))
            .Where(x => x.Attribute != null)
            .OrderByDescending(x => x.Attribute.Position.CompareTo(0))
            .ThenBy(x => x.Attribute.Position)
            .SelectMany(x => GetArgument(x.Token, x.Property, x.Attribute, escapeMethod))
            .WhereNotNull();

        foreach (var argument in arguments)
            yield return argument;

        Func<string, PropertyInfo, string> CreateEscape()
        {
            if (toolAttribute.EscapeMethod == null)
                return null;

            var formatterType = toolAttribute.EscapeType ?? GetType();
            var formatterMethod = formatterType.GetMethod(toolAttribute.EscapeMethod, ReflectionUtility.All);
            return (value, property) => formatterMethod.GetValue<string>(obj: this, args: [value, property]);
        }
    }

    internal IEnumerable<string> GetArgument(
        JToken token,
        PropertyInfo property,
        ArgumentAttribute attribute,
        Func<string, PropertyInfo, string> escape)
    {
        var format = attribute.Format;
        var (first, second, third) = format.SplitSpace();

        if (!property.PropertyType.IsGenericType || property.PropertyType.IsNullableType())
        {
            if (property.PropertyType == typeof(bool?) && !format.ContainsOrdinalIgnoreCase("{value}"))
            {
                Assert.True(second == null);
                if (token.Value<bool>())
                    yield return first;
            }
            else
            {
                var argument = Escape(Format(token, property.PropertyType));
                yield return first?.Replace("{value}", argument);
                yield return second?.Replace("{value}", argument);
                yield return third?.Replace("{value}", argument);
            }
        }
        else
        {
            if (property.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
            {
                var values = token.ToObject<List<string>>();
            }
            else if (property.PropertyType.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
            {
                var values = token.ToObject<Dictionary<string, string>>();
            }
            else if (property.PropertyType.GetGenericTypeDefinition() == typeof(ILookup<,>))
            {
                var values = token.ToObject<Dictionary<string, List<string>>>();
            }
        }

        yield break;

        string Escape(string value) => escape?.Invoke(value, property) ?? value;

        string Format(JToken token, Type type)
        {
            if (attribute.FormatterMethod != null)
            {
                var formatterType = attribute.FormatterType ?? GetType();
                var formatterMethod = formatterType.GetMethod(attribute.FormatterMethod, ReflectionUtility.All);
                return formatterMethod.GetValue<string>(obj: this, args: [token.ToObject(type), property]);
            }

            var value = token.ToObject<string>();
            return !new[] { typeof(bool), typeof(bool?) }.Contains(type) ? value : value.ToLowerInvariant();
        }
    }
}

partial class ToolOptions
{
    public string ProcessToolPath => Get<string>(() => ProcessToolPath);
    public string ProcessWorkingDirectory => Get<string>(() => ProcessWorkingDirectory);

    public IReadOnlyDictionary<string, object> ProcessEnvironmentVariables =>
        Get<Dictionary<string, object>>(() => ProcessEnvironmentVariables);

    public int? ProcessExecutionTimeout => Get<int?>(() => ProcessExecutionTimeout);
    public LogEventLevel? ProcessOutputLogging => Get<LogEventLevel?>(() => ProcessOutputLogging);
    public LogEventLevel? ProcessInvocationLogging => Get<LogEventLevel?>(() => ProcessInvocationLogging);
}

[PublicAPI]
public static partial class ToolOptionsExtensions
{
    #region ToolOptions.ProcessToolPath

    /// <summary><p>Defines the path of the tool to be invoked. In most cases, the tool path is automatically resolved from the PATH environment variable or a NuGet package.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessToolPath))]
    public static T SetProcessToolPath<T>(this T o, string value) where T : ToolOptions => o.Modify(b => b.Set(() => o.ProcessToolPath, value));

    /// <summary><p>Defines the path of the tool to be invoked. In most cases, the tool path is automatically resolved from the PATH environment variable or a NuGet package.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessToolPath))]
    public static T ResetProcessToolPath<T>(this T o) where T : ToolOptions => o.Modify(b => b.Remove(() => o.ProcessToolPath));

    #endregion

    #region ToolOptions.ProcessWorkingDirectory

    /// <summary><p>Defines the working directory for the process.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessWorkingDirectory))]
    public static T SetProcessWorkingDirectory<T>(this T o, string value) where T : ToolOptions => o.Modify(b => b.Set(() => o.ProcessWorkingDirectory, value));

    /// <summary><p>Defines the working directory for the process.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessWorkingDirectory))]
    public static T ResetProcessWorkingDirectory<T>(this T o) where T : ToolOptions => o.Modify(b => b.Remove(() => o.ProcessWorkingDirectory));

    #endregion

    #region ToolOptions.ProcessEnvironmentVariables

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T SetProcessEnvironmentVariables<T>(this T o, IReadOnlyDictionary<string, object> values) where T : ToolOptions => o.Modify(b => b.Set(() => o.ProcessEnvironmentVariables, values));

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T SetProcessEnvironmentVariables<T>(this T o, IDictionary<string, object> values) where T : ToolOptions => o.Modify(b => b.Set(() => o.ProcessEnvironmentVariables, values));

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T AddProcessEnvironmentVariables<T>(this T o, IReadOnlyDictionary<string, object> values) where T : ToolOptions => o.Modify(b => b.AddDictionary(() => o.ProcessEnvironmentVariables, values));

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T AddProcessEnvironmentVariables<T>(this T o, IDictionary<string, object> values) where T : ToolOptions => o.Modify(b => b.AddDictionary(() => o.ProcessEnvironmentVariables, values));

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T AddProcessEnvironmentVariable<T>(this T o, string key, object value) where T : ToolOptions => o.Modify(b => b.AddDictionary(() => o.ProcessEnvironmentVariables, key, value));

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T SetProcessEnvironmentVariable<T>(this T o, string key, object value) where T : ToolOptions => o.Modify(b => b.SetDictionary(() => o.ProcessEnvironmentVariables, key, value));

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T RemoveProcessEnvironmentVariable<T>(this T o, string key) where T : ToolOptions => o.Modify(b => b.RemoveDictionary(() => o.ProcessEnvironmentVariables, key));

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T ClearProcessEnvironmentVariables<T>(this T o) where T : ToolOptions => o.Modify(b => b.ClearDictionary(() => o.ProcessEnvironmentVariables));

    /// <summary><p>Defines the environment variables to be passed to the process. By default, the environment variables of the current process are used.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessEnvironmentVariables))]
    public static T ResetProcessEnvironmentVariables<T>(this T o) where T : ToolOptions => o.Modify(b => b.Remove(() => o.ProcessEnvironmentVariables));

    #endregion

    #region ToolOptions.ProcessExecutionTimeout

    /// <summary><p>Defines the execution timeout of the invoked process.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessExecutionTimeout))]
    public static T SetProcessExecutionTimeout<T>(this T o, int? value) where T : ToolOptions => o.Modify(b => b.Set(() => o.ProcessExecutionTimeout, value));

    /// <summary><p>Defines the execution timeout of the invoked process.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessExecutionTimeout))]
    public static T ResetProcessExecutionTimeout<T>(this T o) where T : ToolOptions => o.Modify(b => b.Remove(() => o.ProcessExecutionTimeout));

    #endregion

    #region ToolOptions.ProcessOutputLogging

    /// <summary><p>Defines the log-level for standard output.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessOutputLogging))]
    public static T SetProcessOutputLogging<T>(this T o, LogEventLevel? value) where T : ToolOptions => o.Modify(b => b.Set(() => o.ProcessOutputLogging, value));

    /// <summary><p>Defines the log-level for standard output.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessOutputLogging))]
    public static T ResetProcessOutputLogging<T>(this T o) where T : ToolOptions => o.Modify(b => b.Remove(() => o.ProcessOutputLogging));

    #endregion

    #region ToolOptions.ProcessInvocationLogging

    /// <summary><p>Defines the log-level for the process invocation.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessInvocationLogging))]
    public static T SetProcessInvocationLogging<T>(this T o, LogEventLevel? value) where T : ToolOptions => o.Modify(b => b.Set(() => o.ProcessInvocationLogging, value));

    /// <summary><p>Defines the log-level for the process invocation.</p></summary>
    [Builder(Type = typeof(ToolOptions), Property = nameof(ToolOptions.ProcessInvocationLogging))]
    public static T ResetProcessInvocationLogging<T>(this T o) where T : ToolOptions => o.Modify(b => b.Remove(() => o.ProcessInvocationLogging));

    #endregion
}
