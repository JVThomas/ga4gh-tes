﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Core;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Tes.ApiClients.Tests
{
    [TestClass, TestCategory("Unit")]
    public class TerraLandingZoneApiClientTest
    {
        private TerraLandingZoneApiClient terraLandingZoneApiClient = null!;
        private Mock<TokenCredential> tokenCredential = null!;
        private Mock<CacheAndRetryHandler> cacheAndRetryHandler = null!;
        private TerraApiStubData terraApiStubData = null!;

        [TestInitialize]
        public void SetUp()
        {
            terraApiStubData = new TerraApiStubData();
            tokenCredential = new Mock<TokenCredential>();
            cacheAndRetryHandler = new Mock<CacheAndRetryHandler>();
            terraLandingZoneApiClient = new TerraLandingZoneApiClient(TerraApiStubData.LandingZoneApiHost, tokenCredential.Object, cacheAndRetryHandler.Object, NullLogger<TerraLandingZoneApiClient>.Instance);
        }

        [TestMethod]
        public async Task GetResourceQuotaAsync_ValidResourceIdReturnsQuotaInformationAndGetsAuthToken()
        {
            var body = terraApiStubData.GetResourceQuotaApiResponseInJson();
            cacheAndRetryHandler.Setup(c => c.ExecuteWithRetryAndCachingAsync(It.IsAny<string>(),
                    It.IsAny<Func<CancellationToken, Task<string>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(body);

            var quota = await terraLandingZoneApiClient.GetResourceQuotaAsync(terraApiStubData.LandingZoneId, terraApiStubData.BatchAccountId, cacheResults: true, cancellationToken: CancellationToken.None);

            Assert.IsNotNull(quota);
            Assert.AreEqual(terraApiStubData.LandingZoneId, quota.LandingZoneId);
            Assert.AreEqual(terraApiStubData.BatchAccountId, quota.AzureResourceId);
            Assert.AreEqual("Microsoft.Batch/batchAccounts", quota.ResourceType);
            Assert.AreEqual(100, quota.QuotaValues.PoolQuota);
            Assert.IsTrue(quota.QuotaValues.DedicatedCoreQuotaPerVmFamilyEnforced);
            Assert.AreEqual(300, quota.QuotaValues.ActiveJobAndJobScheduleQuota);
            Assert.AreEqual(350, quota.QuotaValues.DedicatedCoreQuota);
            Assert.AreEqual(59, quota.QuotaValues.DedicatedCoreQuotaPerVmFamily.Count);
            Assert.AreEqual("standardLSv2Family", quota.QuotaValues.DedicatedCoreQuotaPerVmFamily.Keys.First());
            Assert.AreEqual(0, quota.QuotaValues.DedicatedCoreQuotaPerVmFamily.Values.First());
            tokenCredential.Verify(t => t.GetTokenAsync(It.IsAny<TokenRequestContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task GetLandingZoneResourcesAsync_ListOfLandingZoneResourcesAndGetsAuthToken()
        {
            var body = terraApiStubData.GetResourceApiResponseInJson();

            cacheAndRetryHandler.Setup(c => c.ExecuteWithRetryAndCachingAsync(It.IsAny<string>(),
                    It.IsAny<Func<CancellationToken, Task<string>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(body);

            var resources = await terraLandingZoneApiClient.GetLandingZoneResourcesAsync(terraApiStubData.LandingZoneId, CancellationToken.None);

            Assert.IsNotNull(resources);
            Assert.AreEqual(terraApiStubData.LandingZoneId, resources.Id);
            Assert.AreEqual(5, resources.Resources.Length);
            tokenCredential.Verify(t => t.GetTokenAsync(It.IsAny<TokenRequestContext>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

        }

        [TestMethod]
        public void GetLandingZoneResourcesApiUrl_CorrectUrlIsParsed()
        {
            var url = terraLandingZoneApiClient.GetLandingZoneResourcesApiUrl(terraApiStubData.LandingZoneId);

            var expectedUrl = $"{TerraApiStubData.LandingZoneApiHost}/api/landingzones/v1/azure/{terraApiStubData.LandingZoneId}/resources";

            Assert.AreEqual(expectedUrl, url.ToString());

        }

        [TestMethod]
        public void GetQuotaApiUrl_CorrectUrlIsParsed()
        {
            var url = terraLandingZoneApiClient.GetQuotaApiUrl(terraApiStubData.LandingZoneId, terraApiStubData.BatchAccountId);

            var expectedUrl = $"{TerraApiStubData.LandingZoneApiHost}/api/landingzones/v1/azure/{terraApiStubData.LandingZoneId}/resource-quota?azureResourceId={Uri.EscapeDataString(terraApiStubData.BatchAccountId)}";

            Assert.AreEqual(expectedUrl, url.ToString());

        }
    }
}