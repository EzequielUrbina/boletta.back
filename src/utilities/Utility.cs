using System.Globalization;
using System.Text;
using wsmcbl.src.exception;

namespace wsmcbl.src.utilities;

public static class Utility
{
    public static string generalSecretary => 
        Environment.GetEnvironmentVariable("BOLETTA_SECRETARY") ?? "Secretaría General";    
    
    public static DateTime parseToDatetime(this string value)
    {
        return DateTime.ParseExact(value, "dd-MM-yyyy", CultureInfo.InvariantCulture);
    }
    
    public static DateTime toUTC6(this DateTime datetime)
    {
        var timeZoneUTC6 = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(datetime, timeZoneUTC6);
    }
    
    public static string toStringFull(this DateTime? datetime, bool withDayName = true)
     => datetime == null ? string.Empty : ((DateTime)datetime).toStringFull(withDayName);
    
    public static string toStringFull(this DateTime datetime, bool withDayName = true)
    {
        var dayFormat = withDayName ? "dddd" : "ddd.";
        return datetime.toString($"{dayFormat} dd/MMM/yyyy, h:mm tt");
    }
    
    public static string toString(this DateTime datetime, string format = "dd/MMMM/yyyy")
    {
        var culture = new CultureInfo("es-ES")
        {
            DateTimeFormat =
            {
                AMDesignator = "am",
                PMDesignator = "pm"
            }
        };
        
        return datetime.toUTC6().ToString(format, culture);
    }

    public static string toString(this DateOnly? date, string format = "dd/MM/yyyy")
        => date == null ? string.Empty : ((DateOnly)date).toString(format);
    
    public static string toString(this DateOnly date, string format = "dd/MM/yyyy")
        => date.ToString(format, new CultureInfo("es-ES"));
    
    public static void ReplaceInLatexFormat(this StringBuilder text, string oldValue, string? newValue)
    {
        text.Replace(oldValue, newValue.ReplaceLatexSpecialSymbols());
    }
    
    private static readonly string[] specialSymbols = ["$", "€", "£", "¥", "#", "%", "&", "_", "{", "}"];
    public static string ReplaceLatexSpecialSymbols(this string? text)
    {
        if (text == null)
            return string.Empty;
        
        var sb = new StringBuilder();
        foreach (var c in text)
        {
            if (c == '\\')
            {
                sb.Append("\\textbackslash ");
            }
            else if (Array.IndexOf(specialSymbols, c.ToString()) >= 0)
            {
                sb.Append('\\').Append(c);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
    
    public static string toNormalizeString(this string value)
    {
        var text = value.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (char c in text)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
    
    public static DateTime setHours(this DateTime value, int hour)
    {
        return new DateTime(value.Year, value.Month, value.Day, hour, 0, 0, DateTimeKind.Utc);
    }

    public static void AppendName(this StringBuilder builder, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.Append(' ').Append(value);
        }
    }
    
    public static bool isInProductionEnvironment()
    {
        var value = Environment.GetEnvironmentVariable("API_ENVIRONMENT_MODE");
        if (value == null)
        {
            throw new InternalException("API_ENVIRONMENT_MODE environment not found.");
        }

        return value == "Production";
    }

    public static string getOrDefault(this string? value) => string.IsNullOrWhiteSpace(value) ? "N/A" : value;
    
    public static decimal round(this decimal value) => Math.Round(value, 2);
    public static decimal? round(this decimal? value) => value == null ? null : Math.Round((decimal)value, 2);
}