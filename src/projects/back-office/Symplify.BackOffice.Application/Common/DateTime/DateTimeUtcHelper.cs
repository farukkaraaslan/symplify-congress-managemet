namespace Symplify.BackOffice.Application.Common.DateTime;

public static class DateTimeUtcHelper
{
    public static System.DateTime ToUtc(System.DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => System.DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime(),
            _ => value
        };
    }

    public static System.DateTime? ToUtc(System.DateTime? value)
    {
        return value.HasValue ? ToUtc(value.Value) : null;
    }

    public static System.DateTime ToLocal(System.DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Local => value,
            DateTimeKind.Utc => value.ToLocalTime(),
            DateTimeKind.Unspecified => System.DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime(),
            _ => value
        };
    }

    public static System.DateTime? ToLocal(System.DateTime? value)
    {
        return value.HasValue ? ToLocal(value.Value) : null;
    }
}