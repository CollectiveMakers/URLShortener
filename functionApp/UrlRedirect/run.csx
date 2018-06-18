#load "../UrlIngest/models.csx"
#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.ApplicationInsights;
using System.Net;
using System;
using System.Linq;
using System.Web;

public static TelemetryClient telemetry = new TelemetryClient()
{
    InstrumentationKey = System.Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")
};

public static readonly string FALLBACK_URL = System.Environment.GetEnvironmentVariable("FALLBACK_URL");

public static HttpResponseMessage Run(HttpRequestMessage req, CloudTable inputTable, string shortUrl, TraceWriter log)
{
    log.Info($"ShortUrl {shortUrl}");

    var redirectUrl = FALLBACK_URL;

    if (!String.IsNullOrWhiteSpace(shortUrl))
    {
        shortUrl = shortUrl.Trim().ToLower();
        var partitionKey = $"{shortUrl.First()}";
        TableOperation operation = TableOperation.Retrieve<ShortUrl>(partitionKey, shortUrl);
        TableResult result = inputTable.Execute(operation);
        ShortUrl fullUrl = result.Result as ShortUrl;
        if (fullUrl != null)
        {
            log.Info($"URL Found : {fullUrl.Url} Medium: {fullUrl.Medium}");
            redirectUrl = WebUtility.UrlDecode(fullUrl.Url);
            fullUrl.Requests = fullUrl.Requests + 1;
            fullUrl.ETag = "*";
            var mergeoperation = TableOperation.Merge(fullUrl);
            TableResult resultmerge = inputTable.Execute(mergeoperation);
        }
    }
    else 
    {
        telemetry.TrackEvent("Bad short URL");
    }
    var res = req.CreateResponse(HttpStatusCode.Redirect);
    res.Headers.Add("Location", redirectUrl);
    return res;
}
