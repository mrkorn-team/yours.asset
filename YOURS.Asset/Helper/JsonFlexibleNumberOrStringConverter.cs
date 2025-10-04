using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Handles flexible conversion of numeric types (int, long, float, double, decimal)
/// and can convert number to string automatically if needed.
/// Use case: [JsonConverter(typeof(FlexibleNumberConverter<decimal>))] public decimal amount { get; set; }
/// </summary>
public class FlexibleNumberOrStringConverter<T> : JsonConverter<T>
{
  public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    try
    {
      // Handle numbers
      if (reader.TokenType == JsonTokenType.Number)
      {
        if (typeof(T) == typeof(int) && reader.TryGetInt32(out int i))
          return (T)(object)i;
        if (typeof(T) == typeof(long) && reader.TryGetInt64(out long l))
          return (T)(object)l;
        if (typeof(T) == typeof(float) && reader.TryGetSingle(out float f))
          return (T)(object)f;
        if (typeof(T) == typeof(double) && reader.TryGetDouble(out double db))
          return (T)(object)db;
        if (typeof(T) == typeof(decimal) && reader.TryGetDecimal(out decimal d))
          return (T)(object)d;
        if (typeof(T) == typeof(string))
          return (T)(object)reader.GetDouble().ToString();
      }

      // Handle strings
      if (reader.TokenType == JsonTokenType.String)
      {
        var str = reader.GetString();
        if (typeof(T) == typeof(int) && int.TryParse(str, out int i))
          return (T)(object)i;
        if (typeof(T) == typeof(long) && long.TryParse(str, out long l))
          return (T)(object)l;
        if (typeof(T) == typeof(float) && float.TryParse(str, out float f))
          return (T)(object)f;
        if (typeof(T) == typeof(double) && double.TryParse(str, out double db))
          return (T)(object)db;
        if (typeof(T) == typeof(decimal) && decimal.TryParse(str, out decimal d))
          return (T)(object)d;
        if (typeof(T) == typeof(string))
          return (T)(object)str!;
      }

      // Handle null
      if (reader.TokenType == JsonTokenType.Null)
      {
        if (Nullable.GetUnderlyingType(typeof(T)) != null || typeof(T) == typeof(string))
          return default!;
      }
    }
    catch (Exception ex)
    {
      throw new JsonException($"Error converting value to {typeof(T)}: {ex.Message}");
    }

    throw new JsonException($"Unsupported type or invalid value for {typeof(T)} at {reader.TokenType}");
  }

  public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
  {
    if (value == null)
    {
      writer.WriteNullValue();
      return;
    }

    switch (value)
    {
      case int i:
        writer.WriteNumberValue(i);
        break;
      case long l:
        writer.WriteNumberValue(l);
        break;
      case float f:
        writer.WriteNumberValue(f);
        break;
      case double db:
        writer.WriteNumberValue(db);
        break;
      case decimal d:
        writer.WriteNumberValue(d);
        break;
      default:
        writer.WriteStringValue(value.ToString());
        break;
    }
  }
}

public static class JsonSerializeHelper
{
  private static readonly JsonSerializerOptions _options;

  static JsonSerializeHelper()
  {
    _options = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true,
      WriteIndented = true
    };

    // Register all numeric types
    _options.Converters.Add(new FlexibleNumberOrStringConverter<int>());
    _options.Converters.Add(new FlexibleNumberOrStringConverter<long>());
    _options.Converters.Add(new FlexibleNumberOrStringConverter<float>());
    _options.Converters.Add(new FlexibleNumberOrStringConverter<double>());
    _options.Converters.Add(new FlexibleNumberOrStringConverter<decimal>());

    // Register string type
    _options.Converters.Add(new FlexibleNumberOrStringConverter<string>());
  }

  public static T? Deserialize<T>(string json)
  {
    return JsonSerializer.Deserialize<T>(json, _options);
  }

  public static string Serialize<T>(T obj)
  {
    return JsonSerializer.Serialize(obj, _options);
  }
}