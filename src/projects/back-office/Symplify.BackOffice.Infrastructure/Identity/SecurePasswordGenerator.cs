using System.Security.Cryptography;
using Symplify.BackOffice.Application.Services.UserAdministration;

namespace Symplify.BackOffice.Infrastructure.Identity;

public sealed class SecurePasswordGenerator : IPasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*?";
    private const string All = Upper + Lower + Digits + Symbols;

    public string Generate(int length = 14)
    {
        if (length < 8)
            length = 8;

        char[] password = new char[length];
        password[0] = Upper[RandomNumberGenerator.GetInt32(Upper.Length)];
        password[1] = Lower[RandomNumberGenerator.GetInt32(Lower.Length)];
        password[2] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        password[3] = Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)];

        for (int i = 4; i < password.Length; i++)
            password[i] = All[RandomNumberGenerator.GetInt32(All.Length)];

        Shuffle(password);
        return new string(password);
    }

    private static void Shuffle(char[] value)
    {
        for (int i = value.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (value[i], value[j]) = (value[j], value[i]);
        }
    }
}
