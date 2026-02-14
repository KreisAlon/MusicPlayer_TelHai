using System.Collections.Generic;

namespace Telhai.DotNet.PlayerProject
{
    // Model class representing a single music track
    public class MusicTrack
    {
        public string Title { get; set; }
        public string FilePath { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string ImageUrl { get; set; } // URL fetched from the iTunes API

        // List of additional images managed by the user
        public List<string> UserImages { get; set; }

        // Flag to indicate if metadata was already retrieved
        public bool IsMetadataLoaded { get; set; }

        public MusicTrack()
        {
            // Initialize the list to avoid null reference errors
            UserImages = new List<string>();
            IsMetadataLoaded = false;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}