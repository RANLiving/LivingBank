using System.Text.Json;
using System.Text.Json.Serialization;

namespace LivingBank.Api.Integrations.EnableBanking;

// DTOs que espelham (de forma simplificada) as respostas documentadas da API Enable Banking.
// Confirmar nomes de campos exatos na documentação oficial antes de ligar a produção:
// https://enablebanking.com/docs/api/reference/

public class EbAccountDto
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("account_id")]
    public EbAccountIdDto? AccountId { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "EUR";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("product")]
    public string? Product { get; set; }
}

public class EbAccountIdDto
{
    [JsonPropertyName("iban")]
    public string? Iban { get; set; }
}

public class EbBalanceDto
{
    [JsonPropertyName("balance_amount")]
    public EbAmountDto BalanceAmount { get; set; } = new();

    [JsonPropertyName("balance_type")]
    public string BalanceType { get; set; } = "CLBD";

    [JsonPropertyName("reference_date")]
    public DateOnly? ReferenceDate { get; set; }
}

public class EbAmountDto
{
    [JsonPropertyName("amount")]
    public string Amount { get; set; } = "0";

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "EUR";
}

public class EbTransactionDto
{
    [JsonPropertyName("entry_reference")]
    public string EntryReference { get; set; } = string.Empty;

    [JsonPropertyName("transaction_amount")]
    public EbAmountDto TransactionAmount { get; set; } = new();

    [JsonPropertyName("credit_debit_indicator")]
    public string CreditDebitIndicator { get; set; } = "DBIT";

    [JsonPropertyName("booking_date")]
    public DateOnly? BookingDate { get; set; }

    [JsonPropertyName("value_date")]
    public DateOnly? ValueDate { get; set; }

    [JsonPropertyName("remittance_information")]
    public List<string>? RemittanceInformation { get; set; }

    [JsonPropertyName("creditor")]
    public EbPartyDto? Creditor { get; set; }

    [JsonPropertyName("debtor")]
    public EbPartyDto? Debtor { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "BOOK";
}

public class EbPartyDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class EbListResponse<T>
{
    [JsonPropertyName("continuation_key")]
    public string? ContinuationKey { get; set; }
}

public class EbAccountsResponse : EbListResponse<EbAccountDto>
{
    [JsonPropertyName("accounts")]
    public List<EbAccountDto> Accounts { get; set; } = [];
}

public class EbBalancesResponse
{
    [JsonPropertyName("balances")]
    public List<EbBalanceDto> Balances { get; set; } = [];
}

public class EbTransactionsResponse : EbListResponse<EbTransactionDto>
{
    [JsonPropertyName("transactions")]
    public List<EbTransactionDto> Transactions { get; set; } = [];
}

public class EbAspspDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("maximum_consent_validity")]
    public int? MaximumConsentValidity { get; set; }
}

public class EbAspspsResponse
{
    [JsonPropertyName("aspsps")]
    public List<EbAspspDto> Aspsps { get; set; } = [];
}

public class EbAuthorizeResponse
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class EbSessionResponse
{
    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    // O Enable Banking devolve aqui tanto uma lista de UIDs (string) como, nalgumas
    // respostas, os objetos de conta completos — por isso lê-se como JsonElement bruto
    // e interpreta-se em EnableBankingClient consoante a forma real recebida.
    [JsonPropertyName("accounts")]
    public JsonElement AccountsRaw { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Preenchido pelo EnableBankingClient a partir de AccountsRaw — não vem do JSON.</summary>
    [JsonIgnore]
    public List<EbAccountDto> Accounts { get; set; } = [];
}
