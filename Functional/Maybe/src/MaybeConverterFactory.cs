using System.Text.Json;
using System.Text.Json.Serialization;

namespace Luger.Functional;

/// <summary>
/// Json converter factory for <see cref="Maybe{T}"/>.
/// </summary>
/// <remarks>
/// <b>Serialization</b>
/// <para>
/// Values will be serialized as their inner value if some, or as Null if none.
/// </para>
/// <para>
/// To avoid Null values on properties, use <see cref="JsonIgnoreAttribute"/> with <see cref="JsonIgnoreCondition"/> set
/// to <see cref="JsonIgnoreCondition.WhenWritingDefault"/> or <see cref="JsonSerializerOptions"/> with
/// <see cref="JsonSerializerOptions.DefaultIgnoreCondition"/> set to
/// <see cref="JsonIgnoreCondition.WhenWritingDefault"/>.
/// </para>
/// <b>Deserialization</b>
/// <para>
/// Objects, Arrays, Strings, Numbers and Booleans will be deserialized as Some inner value.
/// </para>
/// <para>
/// Null (or a missing property) will be deserialized as None.
/// </para>
/// </remarks>
public class MaybeConverterFactory : JsonConverterFactory
{
    /// <summary>Determines whether the converter instance can convert the specified object type.</summary>
    /// <returns>
    /// <see langword="true"/> if the specified type is a closed constructed type of <see cref="Maybe{T}"/>; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert is { IsGenericType: true, ContainsGenericParameters: false } &&
        typeToConvert.GetGenericTypeDefinition() == typeof(Maybe<>);

    /// <inheritdoc />
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(MaybeConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType, options)!;
    }

    sealed class MaybeConverter<T>(JsonSerializerOptions options) : JsonConverter<Maybe<T>> where T : notnull
    {
        readonly JsonConverter<T> _innerConverter = (JsonConverter<T>)options.GetConverter(typeof(T));
        readonly Type _innerType = typeof(T);

        public override Maybe<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Skip comments
            while (reader.TokenType == JsonTokenType.Comment)
            {
                reader.Skip();
            }

            return reader.TokenType switch
            {
                JsonTokenType.StartObject or
                    JsonTokenType.StartArray or
                    JsonTokenType.String or
                    JsonTokenType.Number or
                    JsonTokenType.True or
                    JsonTokenType.False =>
                    Maybe.Some(_innerConverter.Read(ref reader, _innerType, options) ?? throw new JsonException()),

                JsonTokenType.Null => Maybe.None<T>(),

                _ => throw new JsonException()
            };
        }

        public override void Write(Utf8JsonWriter writer, Maybe<T> value, JsonSerializerOptions options)
        {
            if (value is [var innerValue])
            {
                _innerConverter.Write(writer, innerValue, options);
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}
