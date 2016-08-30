using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ArbetsprovC9.Models
{
    public class ApiClient
    {
        private const string ClientId = "996d0037680544c987287a9b0470fdbb";
        private const string ClientSecret = "5a3c92099a324b8f9e45d77e919fec13";
        private const string OAuthToken = "BQD3gLpyUA3Kpr8y9og48PDtq5H16qvccKjtBWGhM2MJ2GtEav_H5G8wu5FkadlZ3n8iggLgKL1dFyE6SRxTRSYk3u59GJx7XCei9wScwbX3nj7cQ2wJMU3olB3 - pkuwnXMVuLTL5w";

        protected const string BaseUrl = "https://api.spotify.com/";

        private async Task<string> getSpotifyData(string urlaction)
        {
            var httpClient = new HttpClient();
            var url = "https://api.spotify.com" + urlaction;
            var Json = await httpClient.GetStringAsync(url);

            return Json;
        }

        public async Task<string> getSearchedArtistId(string searchString)
        {
            var httpClient = new HttpClient();
            string url = "https://api.spotify.com/v1/search?q=" + searchString + "&type=artist";
            var Json = await httpClient.GetStringAsync(url);

            return Json;
        }

        public async Task<string> getArtistById(string artistId)
        {
            var artistJson = await getSpotifyData("/v1/artists/" + artistId);
            return artistJson;        
        }

        public async Task<List<Artist>> getRelatedArtistByArtistId(string artistID)
        {
            var relatedArtistsJson = await getSpotifyData("/v1/artists/" + artistID + "/related-artists");
            var deserResult = JsonConvert.DeserializeObject<RootObjectRelatedArtist>(relatedArtistsJson);

            return deserResult.artists;
        }

        public async Task<List<Track>> getTopTracksByArtistId(string artistId)
        {
            var artistsTopTracksJson = await getSpotifyData("/v1/artists/" + artistId + "/top-tracks?country=SE");

            var deserResult = JsonConvert.DeserializeObject<RootObjectTrackList>(artistsTopTracksJson);

            return deserResult.tracks;
        }

    }
}
