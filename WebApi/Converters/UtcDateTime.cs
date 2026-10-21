using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApi.Converters
{
    /// <summary>
    /// Time policy: the API takes and returns instants in UTC. Input must carry "Z" or an offset
    /// ("2026-10-21T09:00:00+05:00" for Tashkent); it is stored as UTC. A value without one is refused (400),
    /// because nobody could tell whether it meant Tashkent or UTC. Converting to Asia/Tashkent is the frontend's job.
    /// </summary>
    public static class UtcDateTime
    {
        public const string Error = "Date and time must be in UTC ('Z') or have an offset, e.g. 2026-10-21T04:00:00Z or 2026-10-21T09:00:00+05:00.";

        public static bool TryParse(string? text, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(text)
                || !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                || parsed.Kind == DateTimeKind.Unspecified)
                return false;

            utc = DateTimeOffset.Parse(text, CultureInfo.InvariantCulture).UtcDateTime;
            return true;
        }

        /// <summary>Values from PostgreSQL timestamptz are already UTC; anything else is written as UTC too.</summary>
        public static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    /// <summary>JSON bodies; System.Text.Json also applies it to DateTime?.</summary>
    public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => UtcDateTime.TryParse(reader.GetString(), out var utc) ? utc : throw new JsonException(UtcDateTime.Error);

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteStringValue(UtcDateTime.ToUtc(value));
    }

    /// <summary>Form and query values (the CRUD endpoints take multipart forms).</summary>
    public sealed class UtcDateTimeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (value == ValueProviderResult.None || string.IsNullOrEmpty(value.FirstValue))
                return Task.CompletedTask;

            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
            if (UtcDateTime.TryParse(value.FirstValue, out var utc))
                bindingContext.Result = ModelBindingResult.Success(utc);
            else
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, UtcDateTime.Error);
            return Task.CompletedTask;
        }
    }

    public sealed class UtcDateTimeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
            => context.Metadata.UnderlyingOrModelType == typeof(DateTime) ? new UtcDateTimeModelBinder() : null;
    }
}
