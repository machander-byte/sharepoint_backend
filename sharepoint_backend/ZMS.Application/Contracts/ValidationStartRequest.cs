namespace ZMS.Application.Contracts;

public sealed class ValidationStartRequest
{
    public Guid MigrationJobId { get; set; }
}
