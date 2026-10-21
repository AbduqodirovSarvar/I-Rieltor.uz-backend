using System.Text.Json;
using WebApi.Converters;

namespace IRieltor.Tests.Unit
{
    public class UtcDateTimeTests
    {
        private static readonly JsonSerializerOptions Options = new() { Converters = { new UtcDateTimeJsonConverter() } };

        private sealed record Model(DateTime At, DateTime? Until);

        [Fact]
        public void Utc_and_offset_values_are_stored_as_utc()
        {
            var model = JsonSerializer.Deserialize<Model>("""{"At":"2026-10-21T09:00:00+05:00","Until":"2026-10-21T04:00:00Z"}""", Options)!;

            Assert.Equal(new DateTime(2026, 10, 21, 4, 0, 0, DateTimeKind.Utc), model.At);
            Assert.Equal(DateTimeKind.Utc, model.At.Kind);
            Assert.Equal(model.At, model.Until);
        }

        [Theory]
        [InlineData("""{"At":"2026-10-21T09:00:00"}""")]
        [InlineData("""{"At":"2026-10-21"}""")]
        [InlineData("""{"At":"not a date"}""")]
        public void Values_without_offset_are_refused(string json)
            => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Model>(json, Options));

        [Fact]
        public void Null_is_allowed_for_nullable()
            => Assert.Null(JsonSerializer.Deserialize<Model>("""{"At":"2026-10-21T04:00:00Z","Until":null}""", Options)!.Until);

        [Fact]
        public void Written_with_Z()
        {
            var json = JsonSerializer.Serialize(new Model(DateTime.SpecifyKind(new DateTime(2026, 10, 21, 4, 0, 0), DateTimeKind.Unspecified), null), Options);
            Assert.Contains("\"2026-10-21T04:00:00Z\"", json);
        }
    }
}
