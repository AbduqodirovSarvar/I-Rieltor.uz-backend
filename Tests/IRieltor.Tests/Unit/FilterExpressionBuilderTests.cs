using Application.Exceptions;
using Application.Extentions;
using Application.Models.Requests.Abstractions;
using Domain.Entities;

namespace IRieltor.Tests.Unit
{
    public class FilterExpressionBuilderTests
    {
        private static readonly List<User> Users =
        [
            new() { FullName = "Ali Valiyev", Login = "ali", Phone = "+998901111111" },
            new() { FullName = "Vali Aliyev", Login = "vali", Phone = "+998902222222" },
            new() { FullName = "Hasan", Login = "hasan" },
        ];

        private static List<string?> Logins(BaseGetListQuery query)
        {
            var predicate = query.BuildPredicate<User>();
            return Users.Where(predicate == null ? _ => true : predicate.Compile()).Select(u => u.Login).ToList();
        }

        [Fact]
        public void Filter_and_search_are_combined()
        {
            var query = new BaseGetListQuery { Filters = new() { ["login.startswith"] = "a" }, SearchText = "valiyev" };
            Assert.Equal(["ali"], Logins(query));
        }

        [Fact]
        public void Search_ignores_letter_case()
            => Assert.Equal(["ali", "vali"], Logins(new BaseGetListQuery { SearchText = "ALIYEV" }).Order().ToList());

        [Fact]
        public void No_filter_means_no_predicate()
            => Assert.Null(new BaseGetListQuery().BuildPredicate<User>());

        [Theory]
        [InlineData("passwordHash.equals")]   // [NotFilterable]
        [InlineData("noSuchField.equals")]
        [InlineData("login")]                 // no operation
        [InlineData("login.greaterthan")]     // not for strings
        public void Forbidden_or_malformed_filters_are_400(string key)
            => Assert.Throws<BadRequestException>(() => new BaseGetListQuery { Filters = new() { [key] = "x" } }.BuildPredicate<User>());

        [Fact]
        public void Wrong_value_type_is_400()
            => Assert.Throws<BadRequestException>(() => new BaseGetListQuery { Filters = new() { ["type.equals"] = "nonsense" } }.BuildPredicate<User>());
    }
}
