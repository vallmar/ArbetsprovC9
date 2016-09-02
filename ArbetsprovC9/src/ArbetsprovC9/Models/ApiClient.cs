using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text;
using System.Reflection;

namespace ArbetsprovC9.Models
{
    public class ApiClient
    {
        private const string ClientId = "996d0037680544c987287a9b0470fdbb";
        private const string ClientSecret = "5a3c92099a324b8f9e45d77e919fec13";
        private HttpClient GetDefaultClient()
        {
            var authHandler = new OauthToken(
                ClientId,
                ClientSecret,
                new HttpClientHandler());

            var client = new HttpClient(authHandler)
            {
                BaseAddress = new Uri(BaseUrl)
            };

            return client;
        }

        private const string OAuthToken = "BQBtNRYb8CPezAm8EBOWAqxLNr4LMoVAIGQSSEcoVFN5ZQDtl4LsFbQzAHGGIU6YpQku_2HoSy8mKXUORfH5z7PM6Gu1_nSRhXbOnDRCfls1i9J4pjixbR9Ta0GY7CL2NvJu68VrNA";

        protected const string BaseUrl = "https://api.spotify.com/";

        private async Task<string> GetSpotifyData(string urlaction)
        {
            var httpClient = GetDefaultClient();
            var url = "https://api.spotify.com" + urlaction;
            var Json = await httpClient.GetStringAsync(url);

            return Json;
        }

        public async Task<string> GetSearchedArtistId(string searchString)
        {
            var httpClient = GetDefaultClient();
            string url = "https://api.spotify.com/v1/search?q=" + searchString + "&type=artist";
            var Json = await httpClient.GetStringAsync(url);

            return Json;
        }

        public async Task<string> GetArtistById(string artistId)
        {
            var artistJson = await GetSpotifyData("/v1/artists/" + artistId);
            return artistJson;
        }

        public async Task<Track>GetTrackById(string trackId)
        {
            var trackJson = await GetSpotifyData("/v1/tracks/" + trackId);
            var deserResult=JsonConvert.DeserializeObject<Track>(trackJson);
            return deserResult;
        }

        public async Task<List<Artist>> GetRelatedArtistByArtistId(string artistID)
        {
            var relatedArtistsJson = await GetSpotifyData("/v1/artists/" + artistID + "/related-artists");
            var deserResult = JsonConvert.DeserializeObject<RootObjectRelatedArtist>(relatedArtistsJson);

            return deserResult.artists;
        }

        public async Task<List<Track>> GetTopTracksByArtistId(string artistId)
        {
            var artistsTopTracksJson = await GetSpotifyData("/v1/artists/" + artistId + "/top-tracks?country=SE");

            var deserResult = JsonConvert.DeserializeObject<RootObjectTrackList>(artistsTopTracksJson);

            return deserResult.tracks;
        }

        public async Task<List<string>> AnalyzeListOfSongs(List<string> trackIds)
        {
            var httpClient = GetDefaultClient();
            //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OAuthToken);
            httpClient.DefaultRequestHeaders.Add("Accept", "application / json");
            List<TrackAudioFeature> trackFeatures = new List<TrackAudioFeature>();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));//ACCEPT header

            foreach (var item in trackIds)
            {
                if (item.Length > 0)
                {
                    var url = "https://api.spotify.com/v1/audio-features/" + item;
                    var jsonAudioFeature = await httpClient.GetByteArrayAsync(url);
                    var responseString = Encoding.UTF8.GetString(jsonAudioFeature, 0, jsonAudioFeature.Length - 1);
                    var responsstring1 = responseString + "}";
                    var deserResult = JsonConvert.DeserializeObject<TrackAudioFeature>(responsstring1);
                    trackFeatures.Add(await GetTrackAudioFeature(item));
                }
            }

            var mostRelevantProps = GetSumOfProperties(trackFeatures).Select(o => o.PropertyName).ToList();
            var mostExplicitOpinionProps = GetMeanAnalyzationOfProperties(trackFeatures);

            var newTrackIds = await GetFinalTracks(trackIds, mostRelevantProps, mostExplicitOpinionProps);

           return newTrackIds.Select(o => o.id).ToList();

        }

        private async Task<List<TrackAudioFeature>> GetFinalTracks(List<string> trackIds, List<string> mostRelevantProps, List<TrackPropAndValue> mostExplicitOpinionProps)
        {
            var artistIds = new List<string>();
            var relatedArtists = new List<Artist>();
            foreach (var item in trackIds)
            {
                if (item.Length > 0)
                {
                    var jsonResult = await GetSpotifyData("/v1/tracks/" + item);
                    var desrResult = JsonConvert.DeserializeObject<Track>(jsonResult);
                    var first5Related = await GetRelatedArtistByArtistId(desrResult.artists[0].id);
                    relatedArtists.AddRange(first5Related.Take(4));
                }
            }

            var toptracks = new List<Track>();
            foreach (var item in relatedArtists)
            {
                var hey = await GetTopTracksByArtistId(item.id);
                toptracks.AddRange(hey.Take(3));
            }
            List<TrackAudioFeature> trackFeatures = new List<TrackAudioFeature>();
            foreach (var item in toptracks.Select(o => o.id))
            {
                trackFeatures.Add(await GetTrackAudioFeature(item));
            }
            var newlist = new List<TrackAudioFeature>();
            switch (mostRelevantProps[0])
            {
                case "danceability":
                    {
                        newlist=trackFeatures.OrderByDescending(o => o.danceability).ToList();
                    }
                    break;
                case "valence":
                    {
                        newlist=trackFeatures.OrderByDescending(o => o.valence).ToList();
                    }
                    break;
                case "speechiness":
                    {
                        newlist=trackFeatures.OrderByDescending(o => o.speechiness).ToList();
                    }
                    break;
                case "instrumentalness":
                    {
                        newlist= trackFeatures.OrderByDescending(o => o.instrumentalness).ToList();
                    }
                    break;
                case "energy":
                    {
                       newlist= trackFeatures.OrderByDescending(o => o.energy).ToList();
                    }
                    break;
                case "acousticness":
                    {
                        newlist=trackFeatures.OrderByDescending(o => o.acousticness).ToList();
                    }
                    break;
                default:
                    {

                    }
                    break;
            }

            switch (mostExplicitOpinionProps[0].PropertyName)
            {
                case "danceability":
                    {
                       return newlist.Where(o => o.danceability > (mostExplicitOpinionProps[0].PropertyValue - 0.05) && o.danceability < (mostExplicitOpinionProps[0].PropertyValue + 0.05)).Take(10).ToList();
                    }
                case "valence":
                    {
                        return newlist.Where(o => o.valence > (mostExplicitOpinionProps[0].PropertyValue - 0.05) && o.valence < (mostExplicitOpinionProps[0].PropertyValue + 0.05)).Take(10).ToList();
                    }
                case "speechiness":
                    {
                        return newlist.Where(o => o.speechiness > (mostExplicitOpinionProps[0].PropertyValue - 0.05) && o.speechiness < (mostExplicitOpinionProps[0].PropertyValue + 0.05)).Take(10).ToList();
                    }
                case "instrumentalness":
                    {
                        return newlist.Where(o => o.instrumentalness > (mostExplicitOpinionProps[0].PropertyValue - 0.05) && o.instrumentalness < (mostExplicitOpinionProps[0].PropertyValue + 0.05)).Take(10).ToList();
                    }
                case "energy":
                    {
                        return newlist.Where(o => o.energy > (mostExplicitOpinionProps[0].PropertyValue - 0.05) && o.energy < (mostExplicitOpinionProps[0].PropertyValue + 0.05)).Take(10).ToList();
                    }
                case "acousticness":
                    {
                        return newlist.Where(o => o.acousticness > (mostExplicitOpinionProps[0].PropertyValue - 0.05) && o.acousticness < (mostExplicitOpinionProps[0].PropertyValue + 0.05)).Take(10).ToList();
                    }
                default:
                    {
                        return newlist.ToList();
                    }
            }
        }

        private async Task<TrackAudioFeature> GetTrackAudioFeature(string item)
        {
            var httpClient = GetDefaultClient();
            var url = "https://api.spotify.com/v1/audio-features/" + item;
            var jsonAudioFeature = await httpClient.GetByteArrayAsync(url);
            var responseString = Encoding.UTF8.GetString(jsonAudioFeature, 0, jsonAudioFeature.Length - 1);
            var responsstring1 = responseString + "}";
            var deserResult = JsonConvert.DeserializeObject<TrackAudioFeature>(responsstring1);
            return deserResult;
        }

        private List<TrackPropAndValue> GetMeanAnalyzationOfProperties(List<TrackAudioFeature> trackFeatures)
        {
            List<TrackPropAndValue> listOfItems = new List<TrackPropAndValue>();

            TrackPropAndValue danceability = new TrackPropAndValue();
            TrackPropAndValue valence = new TrackPropAndValue();
            TrackPropAndValue speechiness = new TrackPropAndValue();
            TrackPropAndValue instrumentalness = new TrackPropAndValue();
            TrackPropAndValue energy = new TrackPropAndValue();
            TrackPropAndValue acousticness = new TrackPropAndValue();

            danceability.PropertyName = "danceability";
            valence.PropertyName = "valence";
            speechiness.PropertyName = "speechiness";
            instrumentalness.PropertyName = "instrumentalness";
            energy.PropertyName = "energy";
            acousticness.PropertyName = "acousticness";

            danceability.PropertyValue = (trackFeatures.Select(o => o.danceability).Max() - trackFeatures.Select(o => o.danceability).Average()) * (trackFeatures.Select(o => o.danceability).Average() - trackFeatures.Select(o => o.danceability).Min());
            valence.PropertyValue = (trackFeatures.Select(o => o.valence).Max() - trackFeatures.Select(o => o.valence).Average()) * (trackFeatures.Select(o => o.valence).Average() - trackFeatures.Select(o => o.valence).Min());
            speechiness.PropertyValue = (trackFeatures.Select(o => o.speechiness).Max() - trackFeatures.Select(o => o.speechiness).Average()) * (trackFeatures.Select(o => o.speechiness).Average() - trackFeatures.Select(o => o.speechiness).Min());
            instrumentalness.PropertyValue = (trackFeatures.Select(o => o.instrumentalness).Max() - trackFeatures.Select(o => o.instrumentalness).Average()) * (trackFeatures.Select(o => o.instrumentalness).Average() - trackFeatures.Select(o => o.instrumentalness).Min());
            energy.PropertyValue = (trackFeatures.Select(o => o.energy).Max() - trackFeatures.Select(o => o.energy).Average()) * (trackFeatures.Select(o => o.energy).Average() - trackFeatures.Select(o => o.energy).Min());
            acousticness.PropertyValue = (trackFeatures.Select(o => o.acousticness).Max() - trackFeatures.Select(o => o.acousticness).Average()) * (trackFeatures.Select(o => o.acousticness).Average() - trackFeatures.Select(o => o.acousticness).Min());

            listOfItems.Add(danceability);
            listOfItems.Add(valence);
            listOfItems.Add(speechiness);
            listOfItems.Add(instrumentalness);
            listOfItems.Add(energy);
            listOfItems.Add(acousticness);

            var returnList= listOfItems.OrderByDescending(o => o.PropertyValue).Take(2).ToList();

            switch (returnList[0].PropertyName)
            {
                case "danceability":
                    {
                        returnList[0].PropertyValue = trackFeatures.Select(o => o.danceability).Average();
                    }
                    break;
                case "valence":
                    {
                        returnList[0].PropertyValue = trackFeatures.Select(o => o.valence).Average();
                    }
                    break;
                case "speechiness":
                    {
                        returnList[0].PropertyValue = trackFeatures.Select(o => o.speechiness).Average();
                    }
                    break;
                case "instrumentalness":
                    {
                        returnList[0].PropertyValue = trackFeatures.Select(o => o.instrumentalness).Average();
                    }
                    break;
                case "energy":
                    {
                        returnList[0].PropertyValue = trackFeatures.Select(o => o.energy).Average();
                    }
                    break;
                case "acousticness":
                    {
                        returnList[0].PropertyValue = trackFeatures.Select(o => o.acousticness).Average();
                    }
                    break;
                default:
                    break;
            }

            return listOfItems.OrderByDescending(o => o.PropertyValue).Take(2).ToList();
        }

        private List<TrackPropAndValue> GetSumOfProperties(List<TrackAudioFeature> listOfAudioFeatures)
        {
            List<TrackPropAndValue> listOfItems = new List<TrackPropAndValue>();

            TrackPropAndValue danceability = new TrackPropAndValue();
            TrackPropAndValue valence = new TrackPropAndValue();
            TrackPropAndValue speechiness = new TrackPropAndValue();
            TrackPropAndValue instrumentalness = new TrackPropAndValue();
            TrackPropAndValue energy = new TrackPropAndValue();
            TrackPropAndValue acousticness = new TrackPropAndValue();

            danceability.PropertyName = "danceability";
            valence.PropertyName = "valence";
            speechiness.PropertyName = "speechiness";
            instrumentalness.PropertyName = "instrumentalness";
            energy.PropertyName = "energy";
            acousticness.PropertyName = "acousticness";

            danceability.PropertyValue = listOfAudioFeatures.Select(o => o.danceability).Sum();
            valence.PropertyValue = listOfAudioFeatures.Select(o => o.valence).Sum();
            speechiness.PropertyValue = listOfAudioFeatures.Select(o => o.speechiness).Sum();
            instrumentalness.PropertyValue = listOfAudioFeatures.Select(o => o.instrumentalness).Sum();
            energy.PropertyValue = listOfAudioFeatures.Select(o => o.energy).Sum();
            acousticness.PropertyValue = listOfAudioFeatures.Select(o => o.acousticness).Sum();

            listOfItems.Add(danceability);
            listOfItems.Add(valence);
            listOfItems.Add(speechiness);
            listOfItems.Add(instrumentalness);
            listOfItems.Add(energy);
            listOfItems.Add(acousticness);

            return listOfItems.OrderByDescending(o => o.PropertyValue).Take(1).ToList();

        }

    }
}
