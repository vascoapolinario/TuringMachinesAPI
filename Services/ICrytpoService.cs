namespace TuringMachinesAPI.Services
{
    public interface ICryptoService
    {
        string? Encrypt(string value);
        string? Decrypt(string value);
    }
}
