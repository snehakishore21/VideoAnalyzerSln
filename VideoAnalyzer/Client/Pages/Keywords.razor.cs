using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using VideoAnalyzer.Shared.Models;

namespace VideoAnalyzer.Client.Pages
{
    public partial class Keywords
    {
        [Inject]
        private HttpClient httpClient { get; set; }
        List<KeywordInfoModel> KeywordsInfoResult { get; set; }

        List<KeywordInfoModel> TopicsInfoResult { get; set; }

        List<KeywordInfoModel> LabelsInfoResult { get; set; }

        List<KeywordInfoModel> BrandsInfoResult { get; set; }

        List<KeywordInfoModel> LocationsInfoResult { get; set; }


        protected override async Task OnInitializedAsync()
        {
            this.KeywordsInfoResult =
                await this.httpClient.
                GetFromJsonAsync<List<KeywordInfoModel>>("VideoIndexer/GetAllKeywords");

            this.TopicsInfoResult =
                await this.httpClient.
                GetFromJsonAsync<List<KeywordInfoModel>>("VideoIndexer/GetAllTopics");

            this.LabelsInfoResult =
           await this.httpClient.
               GetFromJsonAsync<List<KeywordInfoModel>>("VideoIndexer/GetAllLabels");
            this.BrandsInfoResult =
           await this.httpClient.
              GetFromJsonAsync<List<KeywordInfoModel>>("VideoIndexer/GetAllBrands");
            this.LocationsInfoResult =
           await this.httpClient.
             GetFromJsonAsync<List<KeywordInfoModel>>("VideoIndexer/GetAllNamedLocations");
        }
    }
}
