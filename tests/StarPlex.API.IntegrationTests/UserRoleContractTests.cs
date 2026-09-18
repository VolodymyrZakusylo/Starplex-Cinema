using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentAssertions;
using StarPlex.API.Controllers;
using StarPlex.Domain.Enums;

namespace StarPlex.API.IntegrationTests;

public class UserRoleContractTests
{
    [Theory]
    [InlineData(0, UserRole.SuperAdmin)]
    [InlineData(1, UserRole.CinemaManager)]
    [InlineData(2, UserRole.Cashier)]
    [InlineData(3, UserRole.Customer)]
    public void NumericRolePayload_BindsAndValidates(int number, UserRole role)
    {
        var request = JsonSerializer.Deserialize<UpdateRoleRequest>($"{{\"newRole\":{number},\"cinemaId\":null}}", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        request.NewRole.Should().Be(role);
        Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true).Should().BeTrue();
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"newRole\":null}")]
    [InlineData("{\"newRole\":99}")]
    public void MissingOrUndefinedRole_IsRejected(string json)
    {
        var request = JsonSerializer.Deserialize<UpdateRoleRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Validator.TryValidateObject(request, new ValidationContext(request), new List<ValidationResult>(), true).Should().BeFalse();
    }
}
