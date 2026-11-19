using FluentValidation.TestHelper;
using LinkedInContentManager.Application.Features.Auth;
using LinkedInContentManager.Application.Features.Campaigns;
using LinkedInContentManager.Application.Features.Content;
using LinkedInContentManager.Contracts.Auth;
using LinkedInContentManager.Contracts.Campaigns;
using LinkedInContentManager.Contracts.Content;
using LinkedInContentManager.Domain.Enums;

namespace LinkedInContentManager.UnitTests.Application;

public class CreateCampaignCommandValidatorTests
{
    private readonly CreateCampaignCommandValidator _validator = new();

    [Fact]
    public void EmptyName_IsInvalid()
    {
        var command = new CreateCampaignCommand(new CreateCampaignRequest("", Guid.NewGuid(), WriterPersona.Practitioner));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Request.Name);
    }

    [Fact]
    public void EmptyNicheId_IsInvalid()
    {
        var command = new CreateCampaignCommand(new CreateCampaignRequest("Name", Guid.Empty, WriterPersona.Practitioner));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Request.NicheId);
    }

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var command = new CreateCampaignCommand(new CreateCampaignRequest("Name", Guid.NewGuid(), WriterPersona.Practitioner));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class GenerateContentCommandValidatorTests
{
    private readonly GenerateContentCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("ab")] // below MinimumLength(3)
    public void InvalidTopic_IsRejected(string topic)
    {
        var command = new GenerateContentCommand(new GenerateContentRequest(Guid.NewGuid(), topic));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Request.Topic);
    }

    [Fact]
    public void EmptyCampaignId_IsInvalid()
    {
        var command = new GenerateContentCommand(new GenerateContentRequest(Guid.Empty, "A real topic about fintech security"));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Request.CampaignId);
    }
}

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void InvalidEmail_IsRejected()
    {
        var command = new RegisterCommand(new RegisterRequest("not-an-email", "SuperSecret123!", "Jane"));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Request.Email);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public void ShortPassword_IsRejected(string password)
    {
        var command = new RegisterCommand(new RegisterRequest("jane@example.com", password, "Jane"));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Request.Password);
    }

    [Fact]
    public void ValidRegistration_HasNoErrors()
    {
        var command = new RegisterCommand(new RegisterRequest("jane@example.com", "SuperSecret123!", "Jane Doe"));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class RejectContentCommandValidatorTests
{
    private readonly RejectContentCommandValidator _validator = new();

    [Fact]
    public void EmptyReason_IsInvalid()
    {
        var command = new RejectContentCommand(Guid.NewGuid(), new RejectContentRequest(""));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Request.Reason);
    }
}
