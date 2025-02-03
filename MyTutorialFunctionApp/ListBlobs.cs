using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Identity;

namespace MyTutorialFunctionApp
{
    public static class ListBlobs
    {
        [FunctionName("ListBlobs")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("ListBlobs function processed a request.");

            // クエリ文字列から Blob コンテナー名を取得（指定がなければデフォルト値を使用）
            string containerName = req.Query["container"];
            if (string.IsNullOrEmpty(containerName))
            {
                containerName = "sample-container";
            }

            // 環境変数 "STORAGE_ACCOUNT_URL" からストレージアカウントの URL を取得
            // 例: https://mytutorialstorage.blob.core.windows.net
            string storageAccountUrl = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_URL");
            if (string.IsNullOrEmpty(storageAccountUrl))
            {
                return new BadRequestObjectResult("環境変数 'STORAGE_ACCOUNT_URL' が設定されていません。");
            }

            // Blob コンテナーのエンドポイント URL を作成
            string blobContainerUrl = $"{storageAccountUrl}/{containerName}";

            // DefaultAzureCredential を利用して認証（Azure にデプロイ済みの場合はマネージドIDが利用される）
            DefaultAzureCredential credential = new DefaultAzureCredential();

            // BlobContainerClient の生成
            BlobContainerClient containerClient;
            try
            {
                containerClient = new BlobContainerClient(new Uri(blobContainerUrl), credential);
            }
            catch (Exception ex)
            {
                log.LogError($"BlobContainerClient の作成に失敗しました: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            var blobNames = new List<string>();

            // Blob 一覧の取得
            try
            {
                await foreach (var blobItem in containerClient.GetBlobsAsync())
                {
                    blobNames.Add(blobItem.Name);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Blob の取得に失敗しました: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

            return new OkObjectResult(blobNames);
        }
    }
}
