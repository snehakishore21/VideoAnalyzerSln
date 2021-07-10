﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VideoAnalyzer.Server;
using VideoAnalyzer.Shared;
using VideoAnalyzer.Shared.Models.AzureVideoIndexer.ListVideos;
using VideoAnalyzer.Shared.Models.AzureVideoIndexer.SearchVideos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LV = VideoAnalyzer.Shared.Models.AzureVideoIndexer.ListVideos;

namespace VideoAnalyzer.AutomatedTests.Server
{
    [TestClass]
    public class VideoIndexerControllerTests
    {
        private AzureConfiguration AzureConfiguration { get; set; }
        private IHttpClientFactory HttpClientFactory { get; }
        private HttpClient ServerClient { get; }
        private IConfiguration Configuration { get; }

        private readonly TestServer Server;
        private readonly ServiceCollection Services;

        public VideoIndexerControllerTests()
        {
            ConfigurationBuilder configurationBuilder =
                new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json")
                .AddUserSecrets("5ee6af21-ee0f-4995-b5aa-ae4a9aafda1d");
            IConfiguration configuration = configurationBuilder.Build();
            this.Configuration = configuration;
            var azureConfiguration = configuration.GetSection("AzureConfiguration").Get<AzureConfiguration>();
            this.AzureConfiguration = azureConfiguration;
            Server = new TestServer(new WebHostBuilder()
                .UseConfiguration(configuration)
                .UseStartup<Startup>());
            this.Services = new ServiceCollection();
            this.Services.AddHttpClient("VideoIndexerAnonymousApiClient");
            this.Services.AddHttpClient("VideoIndexerAuthorizedApiClient", configuration =>
            {
                configuration.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key",
                    azureConfiguration.VideoIndexerConfiguration.SubscriptionKey);
            });
            this.HttpClientFactory = this.Services.BuildServiceProvider()
                .GetRequiredService<IHttpClientFactory>();
            this.ServerClient = this.Server.CreateClient();
        }

        [TestInitialize]
        public void InitializeTests()
        {
        }

        [TestMethod]
        public async Task Test_UploadVideo()
        {
            UploadVideoModel model =
                new UploadVideoModel()
                {
                    CallbackUrl = this.Configuration["TestUploadVideoCallbackUrl"],
                    Name = "Automated Test Video",
                    SendSuccessEmail = true,
                    VideoUrl = this.Configuration["TestUploadVideoUrl"]
                };
            var result = await this.ServerClient
                .PostAsJsonAsync<UploadVideoModel>("/VideoIndexer/UploadVideo", model);
            if (result.IsSuccessStatusCode)
            {
                Assert.IsTrue(result.IsSuccessStatusCode, result.ReasonPhrase);
            }
            else
            {
                string errorContent = string.Empty;
                if (result.Content.Headers.ContentLength > 0)
                {
                    errorContent = await result.Content.ReadAsStringAsync();
                    Assert.Fail($"Reason: {result.ReasonPhrase} - Details: {errorContent}");
                }
            }
        }

        [TestMethod]
        public async Task Test_ListVideosAsync()
        {
            var result = await this.ServerClient
                .GetFromJsonAsync<ListVideosResponse>("/VideoIndexer/ListVideos");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Test_GetAccountAccessTokenAsync()
        {
            var result = await this.ServerClient
                .GetStringAsync("/VideoIndexer/GetAccountAccessToken");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Test_GetVideoAccessTokenAsync()
        {
            var listVideosResult = await this.ServerClient
                .GetFromJsonAsync<ListVideosResponse>("/VideoIndexer/ListVideos");
            Assert.IsNotNull(listVideosResult);
            var firstVideo = listVideosResult.results.First();
            var result = await this.ServerClient
                .GetStringAsync($"/VideoIndexer/GetVideoAccessToken" +
                $"?videoId={firstVideo.id}&allowEdit={true}");
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Test_GetVideoThumbnailAsync()
        {
            var listVideosResult = await this.ServerClient
                .GetFromJsonAsync<ListVideosResponse>("/VideoIndexer/ListVideos");
            Assert.IsNotNull(listVideosResult);
            var firstVideo = listVideosResult.results.First();
            var result = await this.ServerClient.GetStringAsync($"/VideoIndexer/GetVideoThumbnail" +
                $"?videoId={firstVideo.id}&thumbnailId={firstVideo.thumbnailId}");
            Assert.IsNotNull(result, "Invalid result");
            Assert.IsTrue(result.Length > 0, "Invalid string");
        }

        [TestMethod]
        public async Task Test_GetLocation()
        {
            var result = await this.ServerClient.GetStringAsync("VideoIndexer/GetLocation");
            Assert.IsNotNull(result, "Invalid result");
            Assert.IsTrue(result.Length > 0, "Invalid string");
        }

        [TestMethod]
        public async Task Test_SearchVideosAsync()
        {
            var result = await this.ServerClient
                .GetFromJsonAsync<SearchVideosResponse>($"/VideoIndexer/SearchVideos?keyword=blazor");
            Assert.IsNotNull(result);
        }


    }
}
