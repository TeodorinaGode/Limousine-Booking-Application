namespace LimousineBooking.Application.Interfaces;

/// <summary>Encrypts/decrypts refresh tokens at rest. The Application layer never sees ciphertext details — only this interface.</summary>
public interface ITokenEncryptionService
{
    string Encrypt(string plaintext);

    string Decrypt(string ciphertext);
}
