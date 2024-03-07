using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;

namespace InDuckTor.Shared.Utils;

public static class EnumExtensions
{
    private record struct EnumValueCacheKey(Type EnumType, Enum Value);

    private static readonly ConcurrentDictionary<EnumValueCacheKey, string?> EnumMemberNameByValueCache = new();

    /// <summary>
    /// Достаёт значение из <see cref="EnumMemberAttribute"/>, если не атрибут найдет использует <see cref="Enum.GetName"/>
    /// </summary>
    public static string GetEnumMemberName<TEnum>(this TEnum value)
        where TEnum : struct, Enum
        => EnumMemberNameByValueCache.GetOrAdd(new EnumValueCacheKey(typeof(TEnum), value), GetEnumMemberValue)
           ?? Enum.GetName(value)!;

    private static string? GetEnumMemberValue(EnumValueCacheKey key)
    {
        var enumFieldName = Enum.GetName(key.EnumType, key.Value)!;
        var attribute = key.EnumType.GetField(enumFieldName)
            ?.GetCustomAttributes(typeof(EnumMemberAttribute), true)
            ?.FirstOrDefault() as EnumMemberAttribute;
        return attribute?.Value;
    }

    private record struct EnumNameCacheKey(Type EnumType, string Name);

    private static readonly ConcurrentDictionary<EnumNameCacheKey, Enum?> EnumValuesByNameCache = new();

    /// <summary>
    /// Парсит <paramref name="enumName"/> по значению из <see cref="EnumMemberAttribute"/> и <see cref="Enum.Parse(System.Type,System.ReadOnlySpan{char})"/>
    /// </summary>
    public static TEnum? TryParseWithEnumMember<TEnum>(string? enumName) where TEnum : struct, Enum
    {
        if (enumName is null) return null;
        return Enum.TryParse<TEnum>(enumName, out var result)
            ? result
            : EnumValuesByNameCache.GetOrAdd(new EnumNameCacheKey(typeof(TEnum), enumName), GetEnumValueWithEnumMember) as TEnum?;
    }

    private static Enum? GetEnumValueWithEnumMember(EnumNameCacheKey key)
    {
        foreach (var fieldInfo in key.EnumType.GetFields())
        {
            if (fieldInfo.GetCustomAttribute(typeof(EnumMemberAttribute)) is not EnumMemberAttribute attribute) continue;
            if (attribute.Value != key.Name) continue;

            var value = fieldInfo.GetRawConstantValue();
            if (value is null) return null;
            return Enum.ToObject(key.EnumType, value) as Enum;
        }

        return null;
    }
}