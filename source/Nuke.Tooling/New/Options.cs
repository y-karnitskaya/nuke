// Copyright 2022 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;

namespace Nuke.Tooling;

[JsonObject(MemberSerialization.OptIn)]
public class Options
{
    private static JsonConverter LookupTableConverter = new CustomConverter(typeof(LookupTable<,>), "_dictionary");
    private static JsonConverter OptionsBuilderConverter = new CustomConverter(typeof(Options), "InternalOptions");

    internal static JsonSerializer JsonSerializer = new() { Converters = { LookupTableConverter, OptionsBuilderConverter } };
    internal static JsonSerializerSettings JsonSerializerSettings = new() { Converters = new[] { LookupTableConverter, OptionsBuilderConverter } };

    protected internal JObject InternalOptions = new();

    private static string GetOptionName(LambdaExpression lambdaExpression)
    {
        var member = lambdaExpression.GetMemberInfo();
        return member.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? member.Name;
    }

    internal Options Set<T>(Expression<Func<T>> propertyProvider, object value)
    {
        if (value != null)
        {
            var internalOption = JValue.FromObject(value, JsonSerializer);
            InternalOptions[GetOptionName(propertyProvider)] = internalOption;
        }
        else
        {
            Remove(propertyProvider);
        }

        return this;
    }

    internal Options Remove<T>(Expression<Func<T>> propertyProvider)
    {
        InternalOptions.Property(GetOptionName(propertyProvider))?.Remove();
        return this;
    }

    internal T Get<T>(Expression<Func<object>> optionProvider)
    {
        return Get<T>((LambdaExpression)optionProvider);
    }

    private T Get<T>(LambdaExpression optionProvider)
    {
        return InternalOptions[GetOptionName(optionProvider)] is { } token ? token.ToObject<T>(JsonSerializer) : default;
    }

    #region Dictionary

    private Options UsingDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, Action<Dictionary<TKey, TValue>> action)
    {
        var dictionary = Get<Dictionary<TKey, TValue>>(optionProvider) ?? new Dictionary<TKey, TValue>();
        action.Invoke(dictionary);
        Set(optionProvider, dictionary);
        return this;
    }

    internal Options SetDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, TKey key, TValue value)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary[key] = value);
    }

    internal Options AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, TKey key, TValue value)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary.Add(key, value));
    }

    internal Options AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, IDictionary<TKey, TValue> value)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary.AddDictionary(value));
    }

    internal Options AddDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, IReadOnlyDictionary<TKey, TValue> value)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary.AddReadOnlyDictionary(value));
    }

    internal Options RemoveDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider, TKey key)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary.Remove(key));
    }

    internal Options ClearDictionary<TKey, TValue>(Expression<Func<IReadOnlyDictionary<TKey, TValue>>> optionProvider)
    {
        return UsingDictionary(optionProvider, dictionary => dictionary.Clear());
    }

    #endregion

    #region Lookup

    private Options UsingLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, Action<LookupTable<TKey, TValue>> action)
    {
        var lookup = Get<LookupTable<TKey, TValue>>(optionProvider) ?? new LookupTable<TKey, TValue>();
        action.Invoke(lookup);
        Set(optionProvider, lookup);
        return this;
    }

    internal Options SetLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key, params TValue[] values)
    {
        return UsingLookup(optionProvider, lookup => lookup[key] = values);
    }

    internal Options SetLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key, IEnumerable<TValue> values)
    {
        return UsingLookup(optionProvider, lookup => lookup[key] = values);
    }

    internal Options AddLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key, params TValue[] values)
    {
        return UsingLookup(optionProvider, lookup => lookup.AddRange(key, values));
    }

    internal Options AddLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key, IEnumerable<TValue> values)
    {
        return UsingLookup(optionProvider, lookup => lookup.AddRange(key, values));
    }

    internal Options RemoveLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key)
    {
        return UsingLookup(optionProvider, lookup => lookup.Remove(key));
    }

    internal Options RemoveLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider, TKey key, TValue value)
    {
        return UsingLookup(optionProvider, lookup => lookup.Remove(key, value));
    }

    internal Options ClearLookup<TKey, TValue>(Expression<Func<ILookup<TKey, TValue>>> optionProvider)
    {
        return UsingLookup(optionProvider, lookup => lookup.Clear());
    }

    #endregion

    #region List

    private Options UsingCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider, Action<List<T>> action)
    {
        var collection = Get<List<T>>(optionProvider) ?? new List<T>();
        action.Invoke(collection);
        Set(optionProvider, collection);
        return this;
    }

    internal Options AddCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider, params T[] value)
    {
        return UsingCollection(optionProvider, collection => collection.AddRange(value));
    }

    internal Options AddCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider, IEnumerable<T> value)
    {
        return UsingCollection(optionProvider, collection => collection.AddRange(value));
    }

    internal Options RemoveCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider, params T[] value)
    {
        return UsingCollection(optionProvider, collection => collection.RemoveAll(value.Contains));
    }

    internal Options RemoveCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider, IEnumerable<T> value)
    {
        return UsingCollection(optionProvider, collection => collection.RemoveAll(value.ToList().Contains));
    }

    internal Options ClearCollection<T>(Expression<Func<IReadOnlyCollection<T>>> optionProvider)
    {
        return UsingCollection(optionProvider, collection => collection.Clear());
    }

    #endregion
}

public static class OptionsExtensions
{
    internal static T Modify<T>(this T builder, Action<Options> modification = null)
        where T : Options
    {
        var serializedObject = JsonConvert.SerializeObject(builder, Options.JsonSerializerSettings);
        var copiedObject = JsonConvert.DeserializeObject<T>(serializedObject, Options.JsonSerializerSettings);
        modification?.Invoke(copiedObject);
        return copiedObject;
    }
}
