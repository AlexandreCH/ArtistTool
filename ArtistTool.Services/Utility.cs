namespace ArtistTool.Services
{
    public static class Utility
    {
        public const long Kilobyte = 1024;
        public const long Megabyte = Kilobyte * Kilobyte;
        public const long Gigabyte = Megabyte * Kilobyte;
       
        public static string AsByteSize(this long value)
        {
            if (value < Kilobyte)
            {
                return $"{value} bytes";
            }
            
            if (value < Megabyte)
            {
                return $"{value / Kilobyte} kilobytes";
            }

            if (value < Gigabyte)
            {
                return $"{value / Megabyte} megabytes";
            }

            return $"{value / Gigabyte} gigabytes";
        }
    }
}
