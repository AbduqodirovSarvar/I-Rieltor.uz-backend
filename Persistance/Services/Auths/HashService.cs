using Application.Interfaces.Auths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Persistance.Services.Auths
{
    public class HashService : IHashService
    {
        private readonly PasswordHasher<object> _passwordHasher;

        public HashService()
        {
            _passwordHasher = new PasswordHasher<object>();
        }
        public string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            return _passwordHasher.HashPassword(null!, password);
        }

        public bool Verify(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;

            var result = _passwordHasher.VerifyHashedPassword(null!, hashedPassword, password);

            return result == PasswordVerificationResult.Success ||
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
