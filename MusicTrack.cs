using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telhai.DotNet.PlayerProject
{
    public class MusicTrack
    {
        // Basic info (Local file)
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;

        // --- New properties for API data ---
        public string? Artist { get; set; }       // Artist name from iTunes
        public string? Album { get; set; }        // Album name
        public string? ImageUrl { get; set; }     // Cover image URL

        // Override ToString to display "Artist - Title" in the list if available
        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Artist))
            {
                return $"{Artist} - {Title}";
            }
            return Title;
        }
    }
}