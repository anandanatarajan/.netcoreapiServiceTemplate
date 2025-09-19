using FluentValidation;

namespace Intellimix_Template.Models
{
    public class LoginValidator: AbstractValidator<LoginRequst>
    {
        public LoginValidator()
        {
            RuleFor(x => x.username).NotEmpty().NotNull().Matches("test").WithMessage("user name should be valid one");
            RuleFor(x => x.password).NotNull().WithMessage("Password should not be null").NotEmpty().WithMessage("Password should not empty")
                .Matches("pass").WithMessage("invalid password");
        }
    }
    public record LoginRequst(string username, string password);
    public record RefreshRequest(string Refreshtoken);

    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; } // Hours
    }

    public class RefreshToken
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public string CreatedByIp { get; set; }
        public bool Revoked { get; set; }
    }


    public class JwtSettings
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string Key { get; set; }
        public int AccessTokenExpiryMinutes { get; set; }
        public int RefreshTokenExpiryDays { get; set; }
    }

}
