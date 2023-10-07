namespace Zhp.SafeFromHarm.Func.Adapters.Tipi;

public class TipiOptions
{
    public Uri BaseUrl { get; init; } = new("https://tipi-api.zhp.pl");

    public string TokenId { get; init; } = string.Empty;

    public string TokenSecret { get; init; } = string.Empty;
}
