using System;
using System.Security.Cryptography;
using EmployeeManagement.Infrastructure.Services.Interfaces;

namespace EmployeeManagement.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 10000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            using (var algorithm = new Rfc2898DeriveBytes(password, SaltSize, Iterations, HashAlgorithm))
            {
                byte[] salt = algorithm.Salt;
                byte[] key = algorithm.GetBytes(KeySize);

                byte[] hashBytes = new byte[SaltSize + KeySize];
                Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                Array.Copy(key, 0, hashBytes, SaltSize, KeySize);

                return Convert.ToBase64String(hashBytes);
            }
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password))
                return false;
            if (string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                byte[] expectedKey = new byte[KeySize];
                Array.Copy(hashBytes, SaltSize, expectedKey, 0, KeySize);

                using (var algorithm = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithm))
                {
                    byte[] actualKey = algorithm.GetBytes(KeySize);

                    int result = 0;
                    for (int i = 0; i < KeySize; i++)
                    {
                        result |= actualKey[i] ^ expectedKey[i];
                    }
                    return result == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
