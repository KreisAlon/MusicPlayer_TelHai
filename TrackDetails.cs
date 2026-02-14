using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Telhai.DotNet.PlayerProject
{
    // Class representing a single song from iTunes
    public class SongItem
    {
        // Note: These names match the iTunes API fields (case-insensitive)
        public string? TrackName { get; set; }
        public string? ArtistName { get; set; }      // Must be ArtistName to match data
        public string? CollectionName { get; set; }  // Must be CollectionName (album)
        public string? ArtworkUrl100 { get; set; }   // The image link
    }

    // Class for the API response structure
    public class SearchResult
    {
        public int ResultCount { get; set; }
        public List<SongItem>? Results { get; set; }
    }

    public class ItunesService
    {
        // 'readonly' ensures this client is not changed by mistake
        private readonly HttpClient client;

        public ItunesService()
        {
            client = new HttpClient();
        }

        // Fetch song details from the web asynchronously
        public async Task<SongItem?> GetSongDetails(string songName, System.Threading.CancellationToken token)
        {
            try
            {
                string url = "https://itunes.apple.com/search?term=" + songName + "&media=music&limit=1";

                // Pass the token here so we can cancel the download if needed
                var response = await client.GetAsync(url, token);

                // Read the text
                string jsonContent = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<SearchResult>(jsonContent);

                if (data != null && data.Results != null && data.Results.Count > 0)
                {
                    return data.Results[0];
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}