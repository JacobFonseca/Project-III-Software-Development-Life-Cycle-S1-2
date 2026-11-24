using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;


namespace HydroFlow.Core;

public static class AppSettings
{
    public const double DefaultPollingSeconds = 1;
    public const double MinPollingSeconds = 0.2;
    public const double MaxPollingSeconds = 2;     // NFR-03: UI updates within 2 s

    public static double ReadPollingSeconds(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                string? first = File.ReadLines(path).FirstOrDefault()?.Trim();
                if (double.TryParse(first, NumberStyles.Float, CultureInfo.InvariantCulture, out double s)
                    && s >= MinPollingSeconds && s <= MaxPollingSeconds)
                    return s;
            }
        }
        catch (IOException) { }
        return DefaultPollingSeconds;
    }
}