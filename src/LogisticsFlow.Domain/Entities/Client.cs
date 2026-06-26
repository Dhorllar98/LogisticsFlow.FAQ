namespace LogisticsFlow.Domain.Entities;

/// <summary>
/// Represents a client (company) with an active or historical commercial
/// relationship. Account-identifying fields here are Tier 2 - see
/// docs/data-classification.md.
/// </summary>
public class Client
{
    public Guid Id { get; private set; }

    /// <summary>Internal account identifier. Tier 2.</summary>
    public string AccountId { get; private set; } = string.Empty;

    /// <summary>Tier 2 - identifies a specific business relationship.</summary>
    public string CompanyName { get; private set; } = string.Empty;

    /// <summary>
    /// BCrypt hash of the client's API secret, used to authenticate
    /// token requests at POST /api/quotation/token. Never the raw
    /// secret - only ever the hash. Tier 3 by nature (a credential),
    /// even though the rest of this entity is Tier 2.
    /// </summary>
    public string SecretHash { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    private Client() { } // EF Core

    public Client(Guid id, string accountId, string companyName, string secretHash)
    {
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("AccountId is required.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("CompanyName is required.", nameof(companyName));
        if (string.IsNullOrWhiteSpace(secretHash))
            throw new ArgumentException("SecretHash is required.", nameof(secretHash));

        Id = id;
        AccountId = accountId;
        CompanyName = companyName;
        SecretHash = secretHash;
        CreatedAtUtc = DateTime.UtcNow;
    }
}