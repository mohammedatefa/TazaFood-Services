using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TazaFood_Core.IdentityModels;
using TazaFood_Core.Services;

namespace TazaFood_Services.Token
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration configuration;

        public TokenService(IConfiguration _configartion)
        {
            this.configuration = _configartion;
        }
        public async Task<string> CreateTokenAsync(AppUser user, UserManager<AppUser> userManager)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            // Add validation for required configuration values
            var validIssuer = configuration["Token:ValidIssuer"];
            var validAudience = configuration["Token:ValidAudience"];
            var expirationTime = configuration["Token:ExperiationTime"];
            var tokenKey = configuration["Token:Key"];

            if (string.IsNullOrEmpty(validIssuer) || string.IsNullOrEmpty(validAudience) || string.IsNullOrEmpty(expirationTime) || string.IsNullOrEmpty(tokenKey))
            {
                throw new InvalidOperationException("Token configuration values are missing or invalid.");
            }

            // Add private claims
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName), // Use ClaimTypes.Name for username
                new Claim(ClaimTypes.GivenName, user.DisplayName),
                new Claim(ClaimTypes.Email, user.Email),
            };

            // Add roles
            var userRoles = await userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Define the auth key
            var authKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKey));

            // Create token
            var token = new JwtSecurityToken
            (
                issuer: validIssuer,
                audience: validAudience,
                expires: DateTime.Now.AddMinutes(double.Parse(expirationTime)),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authKey, SecurityAlgorithms.HmacSha256Signature)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

       
    }
}
