namespace ZMS.Core.Interfaces;

public interface ISecretProtector
{
    string? Protect(string? secret);
    string? Unprotect(string? protectedSecret);
}
