using Microsoft.AspNetCore.Identity;

namespace EliteJobs.App.Services
{
    public class RussianIdentityErrorDescriber : IdentityErrorDescriber
    {
        // ВСЕ ОШИБКИ НА АНГЛИЙСКОМ
        public override IdentityError DefaultError() => new() { Description = "An error occurred." };
        public override IdentityError PasswordMismatch() => new() { Description = "Passwords do not match." };
        public override IdentityError PasswordTooShort(int length) => new() { Description = $"Password must be at least {length} characters." };
        public override IdentityError PasswordRequiresDigit() => new() { Description = "Password must include a digit." };
        public override IdentityError PasswordRequiresLower() => new() { Description = "Password must include a lowercase letter." };
        public override IdentityError PasswordRequiresUpper() => new() { Description = "Password must include an uppercase letter." };
        public override IdentityError PasswordRequiresNonAlphanumeric() => new() { Description = "Password must include a special character." };
        public override IdentityError PasswordRequiresUniqueChars(int u) => new() { Description = $"Password must include {u} unique characters." };
        public override IdentityError DuplicateEmail(string email) => new() { Description = $"Email '{email}' is already taken." };
        public override IdentityError DuplicateUserName(string name) => new() { Description = $"Username '{name}' is already taken." };
        public override IdentityError InvalidEmail(string? email) => new() { Description = $"Email '{email}' is invalid." };
        public override IdentityError InvalidUserName(string? name) => new() { Description = $"Username '{name}' is invalid." };
    }
}