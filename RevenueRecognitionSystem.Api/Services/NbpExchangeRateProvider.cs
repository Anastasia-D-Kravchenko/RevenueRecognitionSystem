namespace RevenueRecognitionSystem.Api.Services;

public class NbpExchangeRateProvider
{
    private readonly HttpClient _http;

    public NbpExchangeRateProvider(HttpClient http)
    {
        _http = http;
    }

    public async Task<decimal> GetRateAsync(string toCurrency, CancellationToken ct = default)
    {
        if (string.Equals(toCurrency, "PLN", StringComparison.OrdinalIgnoreCase))
            return 1m;

        var code = toCurrency.ToUpperInvariant();
        var url = $"https://api.nbp.pl/api/exchangerates/rates/A/{code}/?format=json";
        var response = await _http.GetFromJsonAsync<NbpResponse>(url, ct);

        if (response is null || response.Rates is null || response.Rates.Count == 0)
            throw new InvalidOperationException($"Exchange rate for {code} not found.");

        var mid = response.Rates[0].Mid;
        if (mid <= 0)
            throw new InvalidOperationException($"Invalid mid rate for {code}.");

        return 1m / mid;
    }

    private record NbpResponse(string Code, List<NbpRate> Rates);
    private record NbpRate(string No, DateTime EffectiveDate, decimal Mid);
}
