using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ArbetsprovC9.ViewModels;
using ArbetsprovC9.Models;
using Newtonsoft.Json;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace ArbetsprovC9.Controllers
{
    public class SuggestionController : Controller
    {
        // GET: /<controller>/

        ApiClient apiClient = new ApiClient();
        public async Task<IActionResult> Index(string artistId)
        {
            var artistJsonResult = await apiClient.GetArtistById(artistId);
            var deserResult = JsonConvert.DeserializeObject<Item>(artistJsonResult);
            return View(deserResult);
        }

        public async Task<IActionResult> SophisticatedIndex(string artistId)
        {
            var relatedArtists = await apiClient.GetRelatedArtistByArtistId(artistId);

            List<Track> allRelatedArtistsTopTracks = new List<Track>();

            foreach (var item in relatedArtists)
            {
                var thisArtistsTopTracks= await apiClient.GetTopTracksByArtistId(item.id);
                var sendTracks=thisArtistsTopTracks.Take(6);
                allRelatedArtistsTopTracks.AddRange(sendTracks);
            }


            return View(allRelatedArtistsTopTracks);
        }

        public async Task<IActionResult> FinalSuggestions(string ids)
        {
            if (ids==null)
            {
                return RedirectToAction("Home", "Error");
            }
            else
            {
                var trackIds = ids.Split(',');
                List<Track> trackList = new List<Track>();
                foreach (var item in trackIds)
                {
                    if (item.Length > 0)
                    {
                        trackList.Add(await apiClient.GetTrackById(item));
                    }
                }
                return View(trackList);
            }
        }

        public  IActionResult FindMusic()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FindMusic(FirstQuestionsVM modelAnswers)
        {
            if(!ModelState.IsValid)
            {
                return View(modelAnswers);
            }

            var searchResultJson=await apiClient.GetSearchedArtistId(modelAnswers.FavoriteArtist);
            var deserResult= JsonConvert.DeserializeObject<RootObjectSearchResult>(searchResultJson);

            string artistID = deserResult.artists.items.OrderByDescending(o => o.popularity).First().id;

            return RedirectToAction("SophisticatedIndex", new { artistID = artistID });
        }
        [HttpPost]
        public async Task<IActionResult> Refinedsuggestion (StrangeModel ids)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("SophisticatedIndex", new { artistId = ids });
            }

            var trackIds= ids.TrackIdString.Split(',').ToList();
            var newTrackIds= await apiClient.AnalyzeListOfSongs(trackIds);
            string sendstring = "";
            foreach (var item in newTrackIds)
            {
                sendstring += item + ",";
            }
            return RedirectToAction("FinalSuggestions", new { ids = sendstring });
        }
    }
}
