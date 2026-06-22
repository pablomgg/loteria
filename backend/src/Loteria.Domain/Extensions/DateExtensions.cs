using System.Globalization;

namespace Loteria.Domain.Extensions;

public static class DateExtensions
{
    public static DateTime? ParseBrazilDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParseExact(
                value,
                "dd/MM/yyyy",
                CultureInfo.GetCultureInfo("pt-BR"),
                DateTimeStyles.None,
                out var dt))
            return dt;

        return null;
    }
}