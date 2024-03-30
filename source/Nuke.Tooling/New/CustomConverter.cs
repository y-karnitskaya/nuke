using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuke.Common;
using Nuke.Common.Utilities;

namespace Nuke.Tooling;

public class CustomConverter : JsonConverter
{
    private readonly Type _type;
    private readonly string _name;

    public CustomConverter(Type type, string name)
    {
        _type = type;
        _name = name;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var rootProperty = GetRootMember(value.GetType());
        var options = JToken.FromObject(rootProperty.GetValue(value).NotNull(), serializer);
        options.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        existingValue = Activator.CreateInstance(objectType);
        var rootProperty = GetRootMember(objectType);
        var jobject = JObject.Load(reader);
        rootProperty.SetValue(existingValue, jobject.ToObject(rootProperty.GetMemberType(), serializer));
        return existingValue;
    }

    public override bool CanRead => true;

    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType
            ? objectType.GetGenericTypeDefinition() == _type
            : objectType.IsAssignableTo(_type);
    }

    private MemberInfo GetRootMember(Type objectType)
    {
        return objectType.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic).First(x => x.Name == _name);
    }
}