using Domain.Behaviours;
using Domain.Constants;
using System.ComponentModel.DataAnnotations;

namespace IRieltor.Tests.Unit
{
    public class ValidationAttributeTests
    {
        [Theory]
        [InlineData("+998901234567", true)]
        [InlineData("+99890123456789", true)]     // 15 characters: the column size
        [InlineData("+998901234567890", false)]   // 16 characters
        [InlineData("+9", false)]
        [InlineData("998901234567", false)]
        [InlineData("+098901234567", false)]
        [InlineData(null, true)]                  // optional
        public void Phone(string? value, bool valid)
            => Assert.Equal(valid, new PhoneNumberValidationAttribute().IsValid(value));

        [Theory]
        [InlineData("123456789", true)]           // INN
        [InlineData("12345678901234", true)]      // PINFL
        [InlineData("1234567890", false)]
        [InlineData("1234567890123", false)]
        [InlineData("12345678a", false)]
        [InlineData("", true)]
        public void InnPinfl(string value, bool valid)
        {
            var model = new { Value = value };
            var result = new INNPINFLValidationAttribute().GetValidationResult(value, new ValidationContext(model));
            Assert.Equal(valid, result == ValidationResult.Success);
        }

        [Fact]
        public void NotEmptyGuid()
        {
            var attribute = new NotEmptyGuidAttribute();
            Assert.False(attribute.IsValid(Guid.Empty));
            Assert.True(attribute.IsValid(Guid.CreateVersion7()));
        }

        [Theory]
        [InlineData("SUPER_ADMIN", true)]
        [InlineData("super_admin", true)]
        [InlineData(" Super_Admin_2 ", true)]
        [InlineData("superadmin", false)]
        [InlineData("admin", false)]
        [InlineData(null, false)]
        public void ReservedLogin(string? login, bool reserved)
            => Assert.Equal(reserved, SystemRoles.IsReservedLogin(login));
    }
}
