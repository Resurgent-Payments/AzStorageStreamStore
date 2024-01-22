namespace LvStreamStore.Authentication;

using System;
using System.Security.Cryptography;
using System.Text;

using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Options;

public interface IPasswordHasher {
    public string HashPassword(string password);
    public bool VerifyPassword(string providedPassword, string hashedPassword);
}

internal class PasswordHasher : IPasswordHasher {

    private int _iterations = 100_000;
    private readonly RandomNumberGenerator _rng;

    public PasswordHasher(IOptions<PasswordHasherOptions> optionsAccessor) {
        var options = optionsAccessor.Value ?? new();

        _iterations = options.IterationCount;
        _rng = options.Rng;
    }

    public string HashPassword(string password) {
        var saltSize = 128 / 8;
        byte[] salt = new byte[saltSize];
        _rng.GetBytes(salt);
        byte[] subkey = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA512, _iterations, 256 / 8);

        var outputBytes = new byte[13 + salt.Length + subkey.Length];
        outputBytes[0] = 0x01; // format marker
        WriteNetworkByteOrder(outputBytes, 1, (uint)KeyDerivationPrf.HMACSHA512);
        WriteNetworkByteOrder(outputBytes, 5, (uint)_iterations);
        WriteNetworkByteOrder(outputBytes, 9, (uint)saltSize);
        Buffer.BlockCopy(salt, 0, outputBytes, 13, salt.Length);
        Buffer.BlockCopy(subkey, 0, outputBytes, 13 + saltSize, subkey.Length);
        return Convert.ToBase64String(outputBytes);
    }

    public bool VerifyPassword(string providedPassword, string hashedPassword) {
        throw new NotImplementedException();
    }


    private static uint ReadNetworkByteOrder(byte[] buffer, int offset) {
        return ((uint)(buffer[offset + 0]) << 24)
            | ((uint)(buffer[offset + 1]) << 16)
            | ((uint)(buffer[offset + 2]) << 8)
            | ((uint)(buffer[offset + 3]));
    }

    private static void WriteNetworkByteOrder(byte[] buffer, int offset, uint value) {
        buffer[offset + 0] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)(value >> 0);
    }
}

internal class PasswordHasherOptions {
    private static readonly RandomNumberGenerator _defaultRng = RandomNumberGenerator.Create(); // secure PRNG
    public int IterationCount { get; set; } = 250_000;
    internal RandomNumberGenerator Rng { get; set; } = _defaultRng;
}
