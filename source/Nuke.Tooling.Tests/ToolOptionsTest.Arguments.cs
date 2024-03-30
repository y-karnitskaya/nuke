// Copyright 2024 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Nuke.Tooling;
using Xunit;

namespace Nuke.Common.Tests;

public partial class ToolOptionsTest
{
    [Fact]
    public void TestBool()
    {
        new SimpleToolOptions().SetBoolValue(true).GetArguments().Should().Equal(["/bool:true"]);
    }

    [Fact]
    public void TestString()
    {
        var memberInfo = (PropertyInfo) ReflectionUtility.GetMemberInfo(() => new SimpleToolOptions().BoolValue);
        var token = JObject.Parse($$"""
                                    {
                                      {{nameof(SimpleToolOptions.BoolValue)}}: true
                                    }
                                    """).Properties().Single();
        var foo = new SimpleToolOptions().GetArgument(token, memberInfo, new ArgumentAttribute
                                                                         {
                                                                             Format = "--string {value}"
                                                                         }, (x, y) => x);

        new SimpleToolOptions()
            .SetStringValue("value")
            .GetArguments().Should().Equal(["--string", "value"]);
    }

    [Fact]
    public void TestBoolFlag()
    {
        new SimpleToolOptions()
            .SetFlagValue(true)
            .GetArguments().Should().Equal(["/flag"]);

        new SimpleToolOptions()
            .SetFlagValue(false)
            .GetArguments().Should().BeEmpty();

        new SimpleToolOptions()
            .SetFlagValue(null)
            .GetArguments().Should().BeEmpty();
    }

    [Fact]
    public void TestList()
    {
        new SimpleToolOptions()
            .SetSimpleListValue(["a", "b", "c"])
            .GetArguments().Should().Equal(["--param", "a", "b", "c"]);
    }

    [Fact]
    public void TestDictionary()
    {
        new SimpleToolOptions()
            .SetSimpleDictionaryValue(new Dictionary<string, object> { ["key1"] = 1, ["key2"] = "foobar" })
            .GetArguments().Should().Equal(["--param", "key1=1", "key2=foobar"]);
    }

    [Fact]
    public void TestLookup()
    {
        new SimpleToolOptions()
            .SetLookupValue(new LookupTable<string, object> { ["key"] = [1, 2] })
            .GetArguments().Should().Equal(["--param", "key1=1", "key2=foobar"]);
    }

    [Fact]
    public void TestPosition()
    {
        new SimpleToolOptions()
            .SetStringValue("middle")
            .SetLastValue("last")
            .SetSecondToLastValue("second-last")
            .SetSecondValue("second")
            .SetFirstValue("first")
            .GetArguments().Should().Equal(["first", "second", "--string", "middle", "second-last", "last"]);
    }

    [Fact]
    public void TestOrder()
    {
        new SimpleToolOptions()
            .SetStringValue("value")
            .SetBoolValue(true)
            .GetArguments().Should().Equal(["--string", "value", "/bool:true"]);

        new SimpleToolOptions()
            .SetBoolValue(true)
            .SetStringValue("value")
            .GetArguments().Should().Equal(["/bool:true", "--string", "value"]);
    }

    [Fact]
    public void TestFormatter()
    {
        new SimpleToolOptions()
            .SetTimeValue(DateTime.UnixEpoch.AddHours(1).AddMinutes(15))
            .GetArguments().Should().Equal(["01:15"]);

        new SimpleToolOptions()
            .SetDateValue(DateTime.UnixEpoch)
            .GetArguments().Should().Equal(["01/01/1970"]);

        new SimpleToolOptions()
            .SetMinutesValue(TimeSpan.FromMinutes(10))
            .GetArguments().Should().Equal(["10"]);
    }

    [Fact]
    public void TestImplicitArguments()
    {
        new ImplicitArgumentsToolOptions()
            .SetStringValue("value")
            .GetArguments().Should().Equal(["first", "second", "--string", "value"]);
    }
}

[NuGetTool(PackageId = ["xunit.runner.console"], Executable = ["xunit.console.exe"])]
file class SimpleTool;

[Command(Type = typeof(SimpleTool))]
file partial class SimpleToolOptions : ToolOptions
{
    [Argument(Format = "/bool:{value}")] public bool BoolValue => Get<bool>(() => BoolValue);
    [Argument(Format = "/flag")] public bool? FlagValue => Get<bool>(() => FlagValue);
    [Argument(Format = "--string {value}")] public string StringValue => Get<string>(() => StringValue);

    [Argument(Format = "--param {value}")] public IReadOnlyList<string> SimpleListValue => Get<List<string>>(() => SimpleListValue);
    [Argument(Format = "--param {value}", ListSeparator = " ")] public IReadOnlyList<string> AdvancedListValue => Get<List<string>>(() => AdvancedListValue);

    [Argument(Format = "--param {key}={value}")] public IReadOnlyDictionary<string, object> SimpleDictionaryValue => Get<Dictionary<string, object>>(() => SimpleDictionaryValue);
    [Argument(Format = "--param {key}={value}", ItemSeparator = ";")] public IReadOnlyDictionary<string, object> AdvancedDictionaryValue => Get<Dictionary<string, object>>(() => AdvancedDictionaryValue);

    [Argument(Format = "--param {key}={value}")] public ILookup<string, object> LookupValue => Get<LookupTable<string, object>>(() => LookupValue);

    [Argument(Format = "{value}", Position = 1)] public string FirstValue => Get<string>(() => FirstValue);
    [Argument(Format = "{value}", Position = 2)] public string SecondValue => Get<string>(() => SecondValue);
    [Argument(Format = "{value}", Position = -1)] public string LastValue => Get<string>(() => LastValue);
    [Argument(Format = "{value}", Position = -2)] public string SecondToLastValue => Get<string>(() => SecondToLastValue);

    [Argument(Format = "{value}", FormatterMethod = nameof(FormatTime))] public DateTime TimeValue => Get<DateTime>(() => TimeValue);
    [Argument(Format = "{value}", FormatterMethod = nameof(FormatDate))] public DateTime DateValue => Get<DateTime>(() => DateValue);
    [Argument(Format = "{value}", FormatterType = typeof(Formatter), FormatterMethod = nameof(Formatter.FormatMinutes))] public TimeSpan MinutesValue => Get<TimeSpan>(() => MinutesValue);
    private string FormatTime(DateTime datetime, PropertyInfo property) => datetime.ToString("t", CultureInfo.InvariantCulture);
    private string FormatDate(DateTime datetime, PropertyInfo property) => datetime.ToString("d", CultureInfo.InvariantCulture);
}

file static class Formatter
{
    public static string FormatMinutes(TimeSpan timespan, PropertyInfo property) => timespan.TotalMinutes.ToString(CultureInfo.InvariantCulture);
}

file static class SimpleToolOptionsExtensions
{
    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.BoolValue))]
    public static T SetBoolValue<T>(this T o, bool value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.BoolValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.FlagValue))]
    public static T SetFlagValue<T>(this T o, bool? value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.FlagValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.StringValue))]
    public static T SetStringValue<T>(this T o, string value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.StringValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.SimpleListValue))]
    public static T SetSimpleListValue<T>(this T o, string[] value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.SimpleListValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.AdvancedListValue))]
    public static T SetAdvancedListValue<T>(this T o, string[] value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.AdvancedListValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.SimpleDictionaryValue))]
    public static T SetSimpleDictionaryValue<T>(this T o, Dictionary<string, object> value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.SimpleDictionaryValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.AdvancedDictionaryValue))]
    public static T SetAdvancedDictionaryValue<T>(this T o, Dictionary<string, object> value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.AdvancedDictionaryValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.LookupValue))]
    public static T SetLookupValue<T>(this T o, ILookup<string, object> value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.LookupValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.FirstValue))]
    public static T SetFirstValue<T>(this T o, string value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.FirstValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.SecondValue))]
    public static T SetSecondValue<T>(this T o, string value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.SecondValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.SecondToLastValue))]
    public static T SetSecondToLastValue<T>(this T o, string value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.SecondToLastValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.LastValue))]
    public static T SetLastValue<T>(this T o, string value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.LastValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.TimeValue))]
    public static T SetTimeValue<T>(this T o, DateTime value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.TimeValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.DateValue))]
    public static T SetDateValue<T>(this T o, DateTime value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.DateValue, value));

    [Builder(Type = typeof(SimpleToolOptions), Property = nameof(SimpleToolOptions.MinutesValue))]
    public static T SetMinutesValue<T>(this T o, TimeSpan value) where T : SimpleToolOptions => o.Modify(b => b.Set(() => o.MinutesValue, value));
}

[NuGetTool(Arguments = "first")]
file class ImplicitArgumentsTool;

[Command(Type = typeof(ImplicitArgumentsTool), Arguments = "second")]
file class ImplicitArgumentsToolOptions : ToolOptions
{
    [Argument(Format = "--string {value}")] public string StringValue => Get<string>(() => StringValue);
}

file static class ImplicitArgumentsToolOptionsExtensions
{
    [Builder(Type = typeof(ImplicitArgumentsToolOptions), Property = nameof(ImplicitArgumentsToolOptions.StringValue))]
    public static T SetStringValue<T>(this T o, string value) where T : ImplicitArgumentsToolOptions => o.Modify(b => b.Set(() => o.StringValue, value));
}
