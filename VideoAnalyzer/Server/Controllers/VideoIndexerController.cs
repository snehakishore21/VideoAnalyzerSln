using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using VideoAnalyzer.Shared;
using System.Text;
using OpenTextSummarizer.Interfaces;
using OpenTextSummarizer;
using VideoAnalyzer.Shared.Helpers;
using VideoAnalyzer.Shared.Models;
using VideoAnalyzer.Shared.Models.AzureVideoIndexer.GetPersonModels;
using VideoAnalyzer.Shared.Models.AzureVideoIndexer.GetPersons;
using VideoAnalyzer.Shared.Models.AzureVideoIndexer.GetVideoIndex;
using VideoAnalyzer.Shared.Models.AzureVideoIndexer.ListVideos;
using VideoAnalyzer.Shared.Models.AzureVideoIndexer.SearchVideos;

namespace VideoAnalyzer.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoIndexerController : ControllerBase
    {
        private AzureConfiguration AzureConfiguration { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        private IMemoryCache MemoryCache { get; }

        public VideoIndexerController(AzureConfiguration azureConfiguration,
            IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            this.AzureConfiguration = azureConfiguration;
            this.HttpClientFactory = httpClientFactory;
            this.MemoryCache = memoryCache;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetPersonsModels()
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            PersonModel[] result = await helper.GetAllPersonsModels();
            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetPersonPicture(string personModelId,
            string personId, string faceId)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            string base64String = await helper.GetPersonPictureInBase64(personModelId, personId, faceId);
            return Ok(base64String);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllPersons()
        {
            GetAllPersonsModel model = null;
            this.MemoryCache.TryGetValue<GetAllPersonsModel>(Constants.ALLPERSONS_INFO, out model);
            if (model == null)
            {
                AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                    this.CreateAuthorizedHttpClient());
                model = await helper.GetAllPersonsData();
            }
            return Ok(model);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetPersons(string personModelId)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            GetPersonsResponse result = await helper.GetAllPersonsInPersonModel(personModelId);
            return Ok(result);
        }

        [HttpPost("[action]")]
        public async Task<IActionResult> UploadVideo(UploadVideoModel model)
        {
            HttpResponseMessage result = await AnalyzeVideo(model);
            if (result.IsSuccessStatusCode)
            {
                return Ok();
            }
            else
            {
                string content = string.Empty;
                if (result.Content.Headers.ContentLength > 0)
                {
                    content = await result.Content.ReadAsStringAsync();
                }
                return Problem(detail: content, title: result.ReasonPhrase);
            }
        }

        private async Task<HttpResponseMessage> AnalyzeVideo(UploadVideoModel model)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            string accountAccesstoken = await helper.GetAccountAccessTokenString(true);
            string requestUrl = $"https://api.videoindexer.ai/" +
                $"{this.AzureConfiguration.VideoIndexerConfiguration.Location}" +
                $"/Accounts/{this.AzureConfiguration.VideoIndexerConfiguration.AccountId}" +
                $"/Videos" +
                $"?name={EncodeUrl(model.Name)}" +
                //$"[&privacy]" +
                //$"[&priority]" +
                //$"[&description]" +
                //$"[&partition]" +
                //$"[&externalId]" +
                //$"[&externalUrl]" +
                //$"[&metadata]" +
                //$"[&language]" +
                $"&videoUrl={EncodeUrl(model.VideoUrl)}" +
                //$"[&fileName]" +
                //$"[&indexingPreset]" +
                //$"[&streamingPreset]" +
                //$"[&linguisticModelId]" +
                //$"[&personModelId]" +
                //$"[&animationModelId]" +
                $"&sendSuccessEmail={model.SendSuccessEmail}" +
                //$"[&assetId]" +
                //$"[&brandsCategories]" +
                $"&accessToken={accountAccesstoken}";
            if (!string.IsNullOrWhiteSpace(model.CallbackUrl))
                requestUrl +=
                    $"&callbackUrl={EncodeUrl(model.CallbackUrl)}";
            var client = this.CreateAuthorizedHttpClient();
            var result = await client.PostAsync(requestUrl, null);
            return result;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllKeywords()
        {
            List<KeywordInfoModel> lstKeywords = null;
            this.MemoryCache.TryGetValue<List<KeywordInfoModel>>
                (Constants.ALLVIDEOS_KEYWORDS, out lstKeywords);
            if (lstKeywords == null)
            {
                AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                    this.CreateAuthorizedHttpClient());
                lstKeywords = await helper.GetAllKeywords();
            }
            return Ok(lstKeywords.OrderByDescending(p=>p.Appeareances).ToList());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllTopics()
        {
            List<KeywordInfoModel> lstKeywords = null;
            this.MemoryCache.TryGetValue<List<KeywordInfoModel>>
               (Constants.ALLVIDEOS_TOPICS, out lstKeywords);
            if (lstKeywords == null)
            {
                AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                    this.CreateAuthorizedHttpClient());
                lstKeywords = await helper.GetAllTopics();
            }
            return Ok(lstKeywords.OrderByDescending(p => p.Appeareances).ToList());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllLabels()
        {
            List<KeywordInfoModel> lstKeywords = null;
            this.MemoryCache.TryGetValue<List<KeywordInfoModel>>
               (Constants.ALLVIDEOS_LABELS, out lstKeywords);
            if (lstKeywords == null)
            {
                AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                    this.CreateAuthorizedHttpClient());
                lstKeywords = await helper.GetAllLabels();
            }
            return Ok(lstKeywords.OrderByDescending(p => p.Appeareances).ToList());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllBrands()
        {
            List<KeywordInfoModel> lstKeywords = null;
            this.MemoryCache.TryGetValue<List<KeywordInfoModel>>
               (Constants.ALLVIDEOS_BRANDS, out lstKeywords);
            if (lstKeywords == null)
            {
                AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                    this.CreateAuthorizedHttpClient());
                lstKeywords = await helper.GetAllBrands();
            }
            return Ok(lstKeywords.OrderByDescending(p => p.Appeareances).ToList());
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAllNamedLocations()
        {
            List<KeywordInfoModel> lstKeywords = null;
            this.MemoryCache.TryGetValue<List<KeywordInfoModel>>
               (Constants.ALLVIDEOS_LOCATIONS, out lstKeywords);
            if (lstKeywords == null)
            {
                AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                    this.CreateAuthorizedHttpClient());
                lstKeywords = await helper.GetAllNamedLocations();
            }
            return Ok(lstKeywords.OrderByDescending(p => p.Appeareances).ToList());
        }


        [HttpGet("[action]")]
        public async Task<IActionResult> GetInstancesOfKeywordPerVideo(string query, string videoid)
        {
            List<SearchQueryDetail> lstKeywords = null;
            if (lstKeywords == null)
            {
                AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                    this.CreateAuthorizedHttpClient());
                lstKeywords = await helper.GetInstancesOfKeywordPerVideo(query, videoid);
            }
            return Ok(lstKeywords.OrderByDescending(p => p.Appeareances).ToList());
        }

        private string EncodeUrl(string url)
        {
            return System.Web.HttpUtility.UrlPathEncode(url);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ListVideos()
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            ListVideosResponse result = await helper.GetAllVideos();
            return Ok(result);
        }

        private HttpClient CreateAuthorizedHttpClient()
        {
            return this.HttpClientFactory.CreateClient("VideoIndexerAuthorizedApiClient");
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetAccountAccessToken(bool allowEdit = false)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            string result = await helper.GetAccountAccessTokenString(allowEdit);
            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVideoAccessToken(string videoId, bool allowEdit = false)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            var videoAccesstoken = await helper.GetVideoAccessTokenStringAsync(videoId, allowEdit);
            return Ok(videoAccesstoken);
        }

        [HttpGet("[action]")]
        public IActionResult GetLocation()
        {
            return Ok(this.AzureConfiguration.VideoIndexerConfiguration.Location);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVideoThumbnail(string videoId, string thumbnailId)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            string videoAccessToken = await helper.GetVideoAccessTokenStringAsync(videoId, true);
            string format = "Base64";
            string requestUrl = $"{this.AzureConfiguration.VideoIndexerConfiguration.BaseAPIUrl}" +
                $"/{this.AzureConfiguration.VideoIndexerConfiguration.Location}" +
                $"/Accounts/{this.AzureConfiguration.VideoIndexerConfiguration.AccountId}" +
                $"/Videos/{videoId}" +
                $"/Thumbnails/{thumbnailId}" +
                $"?format={format}" +
                $"&accessToken={videoAccessToken}";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                this.AzureConfiguration.VideoIndexerConfiguration.SubscriptionKey);
            var result = await client.GetStringAsync(requestUrl);
            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVideoCaption(string videoId)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            string videoAccessToken = await helper.GetVideoAccessTokenStringAsync(videoId, true);
            string format = "Txt";
            string requestUrl = $"{this.AzureConfiguration.VideoIndexerConfiguration.BaseAPIUrl}" +
                $"/{this.AzureConfiguration.VideoIndexerConfiguration.Location}" +
                $"/Accounts/{this.AzureConfiguration.VideoIndexerConfiguration.AccountId}" +
                $"/Videos/{videoId}/Captions" +
                 $"?indexId={videoId}" +
                 $"&format={format}" +
                $"&accessToken={videoAccessToken}";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                this.AzureConfiguration.VideoIndexerConfiguration.SubscriptionKey);
            var result = await client.GetStringAsync(requestUrl);
            //GetSummary(result.ToString());
            //GetTranscript(result);
            return Ok(result);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> GetVideoSummary(string videoId)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            string videoAccessToken = await helper.GetVideoAccessTokenStringAsync(videoId, true);
            string format = "Txt";
            string requestUrl = $"{this.AzureConfiguration.VideoIndexerConfiguration.BaseAPIUrl}" +
                $"/{this.AzureConfiguration.VideoIndexerConfiguration.Location}" +
                $"/Accounts/{this.AzureConfiguration.VideoIndexerConfiguration.AccountId}" +
                $"/Videos/{videoId}/Captions" +
                 $"?indexId={videoId}" +
                 $"&format={format}" +
                $"&accessToken={videoAccessToken}";
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                this.AzureConfiguration.VideoIndexerConfiguration.SubscriptionKey);
            var result = await client.GetStringAsync(requestUrl);
            var summary = GetSummary(result.ToString());
            //
            //GetTranscript(result);
            return Ok(summary);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> SearchVideos(string keyword)
        {
            AzureVideoIndexerHelper helper = new AzureVideoIndexerHelper(this.AzureConfiguration,
                this.CreateAuthorizedHttpClient());
            var accountAccesstoken = await helper.GetAccountAccessTokenString(false);
            string requestUrl = $"https://api.videoindexer.ai/" +
                $"{this.AzureConfiguration.VideoIndexerConfiguration.Location}" +
                $"/Accounts/{this.AzureConfiguration.VideoIndexerConfiguration.AccountId}" +
                $"/Videos/Search?" +
                //$"[?sourceLanguage]" +
                //$"[&isBase]" +
                //$"[&hasSourceVideoFile]" +
                //$"[&sourceVideoId]" +
                //$"[&state]" +
                //$"[&privacy]" +
                //$"[&id]" +
                //$"[&partition]" +
                //$"[&externalId]" +
                //$"[&owner]" +
                //$"[&face]" +
                //$"[&animatedcharacter]" +
                $"query={keyword}" +
            //$"[&textScope]" +
            //$"[&language]" +
            //$"[&createdAfter]" +
            //$"[&createdBefore]" +
            //$"[&pageSize]" +
            //$"[&skip]" +
            $"&accessToken={accountAccesstoken}";
            HttpClient client = new HttpClient();
            var result = await client.GetFromJsonAsync<SearchVideosResponse>(requestUrl);
            return Ok(result);
        }

        public void GetTranscript(string transcript)
        {
            string fullPath = "C:\\invoice\\abc.txt";
            using (StreamWriter writer = new StreamWriter(fullPath))
            {
                writer.WriteLine(transcript);
            }
        }
        public string GetSummary(string transcript)
        {
            string fullPath = "C:\\invoice\\abc.txt";
            using (StreamWriter writer = new StreamWriter(fullPath))
            {
                writer.WriteLine(transcript);
            }

            var summarizedDocument = OpenTextSummarizer.Summarizer.Summarize(
                new OpenTextSummarizer.FileContentProvider("C:\\invoice\\abc.txt"),
                new SummarizerArguments()
                {
                    Language = "en",
                    MaxSummarySentences = 5
                });

            return summarizedDocument.Sentences.ToString();
            //SummarizedDocument doc = Summarizer.Summarize(content, sumargs);
            // string summary = string.Join("\r\n\r\n", doc.Sentences.ToArray());
            //Console.WriteLine(summary);
        }


    }
}
