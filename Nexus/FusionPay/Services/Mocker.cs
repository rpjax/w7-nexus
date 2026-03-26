namespace ServicesApi.FusionPay.Services;

public static class Mocker
{
    public static string GenerateFakeCpf()
    {
        var r = Random.Shared;
        return $"{r.Next(100, 999)}{r.Next(100, 999)}{r.Next(100, 999)}{r.Next(10, 99)}";
    }

    public static string GenerateFakePhone()
    {
        // Formato: +55 + DDD (2 dígitos) + 9 + 8 dígitos
        var r = Random.Shared;
        return $"+55{r.Next(11, 99)}9{r.Next(10000000, 99999999)}";
    }
}
