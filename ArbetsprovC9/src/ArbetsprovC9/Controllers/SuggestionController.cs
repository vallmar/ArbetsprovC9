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
            var artistJsonResult = await apiClient.getArtistById(artistId);
            var deserResult = JsonConvert.DeserializeObject<Item>(artistJsonResult);
            return View(deserResult);
        }

        public async Task<IActionResult> SophisticatedIndex(string artistId)
        {
            var relatedArtists = await apiClient.getRelatedArtistByArtistId(artistId);

            List<Track> allRelatedArtistsTopTracks = new List<Track>();

            foreach (var item in relatedArtists)
            {
                var thisArtistsTopTracks= await apiClient.getTopTracksByArtistId(item.id);
                allRelatedArtistsTopTracks.AddRange(thisArtistsTopTracks);
            }

            return View(allRelatedArtistsTopTracks);
        }

        public  IActionResult FindMusic()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> FindMusic(FirstQuestionsVM modelAnswers)
        {
            var searchResultJson=await apiClient.getSearchedArtistId(modelAnswers.FavoriteArtist);
            var deserResult= JsonConvert.DeserializeObject<RootObjectSearchResult>(searchResultJson);

            string artistID = deserResult.artists.items.OrderByDescending(o => o.popularity).First().id;

            return RedirectToAction("SophisticatedIndex", new { artistID = artistID });
        }
    }
}
