using System;

namespace StarPlex.Application.Common.Helpers;

public static class TimeZoneHelpers
{
    public static readonly TimeZoneInfo KyivTimeZone = GetKyivTimeZoneInfo();

    private static TimeZoneInfo GetKyivTimeZoneInfo()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Kyiv");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
        }
    }
}
