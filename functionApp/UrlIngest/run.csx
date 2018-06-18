#load ".\models.csx"
#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;
using System.Net;
using System;
using System.Linq;
using System.Web;

public static readonly string SHORTENER_URL = System.Environment.GetEnvironmentVariable("SHORTENER_URL");
public static readonly string Alphabet = "abcdefghijklmnopqrstuvwxyz0123456789";
public static readonly int Base = Alphabet.Length;

public static string Encode(int i)
{
            if (i == 0)
                            return Alphabet[0].ToString();
            var s = string.Empty;
            while (i > 0)
            {
                            s += Alphabet[i % Base];
                            i = i / Base;
            }
            return string.Join(string.Empty, s.Reverse());
}

public static string[] UTM_MEDIUMS=new [] {"Twitter" , "Linkedin" , "Flipboard" };

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, NextId keyTable, CloudTable tableOut, TraceWriter log)
{
    log.Info($"Function called with : {req}");

    if (req == null)
    {
        return req.CreateResponse(HttpStatusCode.NotFound);
    }

    Request input = await req.Content.ReadAsAsync<Request>();

    if (input == null)
    {
        return req.CreateResponse(HttpStatusCode.NotFound);
    }

    var result = new List<Result>();
    var LongUrl = input.Input;
    var medium = input.Medium ?? "None";
    
    log.Info($"URL: {LongUrl} Mediums? {medium}");
    
    if (String.IsNullOrWhiteSpace(LongUrl))
    {
        throw new Exception("Need a URL to shorten!");
    }

    if (keyTable == null)
    {
        keyTable = new NextId
        {
            PartitionKey = "1",
            RowKey = "KEY",
            Id = 1024
        };
        var keyAdd = TableOperation.Insert(keyTable);
        await tableOut.ExecuteAsync(keyAdd); 
    }
    
    log.Info($"Current key: {keyTable.Id}"); 
    if (medium == "All") 
    {
        foreach(var med in UTM_MEDIUMS)
        {
            var shortUrl = Encode(keyTable.Id++);
            log.Info($"Short URL for {med} is {shortUrl}");
            var newUrl = new ShortUrl 
            {
                PartitionKey = $"{shortUrl.First()}",
                RowKey = $"{shortUrl}",
                Medium = med,
                Url = LongUrl,
                Requests = 0
            };
            var multiAdd = TableOperation.Insert(newUrl);
            await tableOut.ExecuteAsync(multiAdd); 
            result.Add(new Result 
            { 
                ShortUrl = $"{SHORTENER_URL}/{newUrl.RowKey}",
                LongUrl = WebUtility.UrlDecode(newUrl.Url),
                Medium = med
            });
        }
    }
    else 
    {
        var shortUrl = Encode(keyTable.Id++);
        log.Info($"Short URL for {LongUrl} is {shortUrl}");
        var newUrl = new ShortUrl 
        {
            PartitionKey = $"{shortUrl.First()}",
            RowKey = $"{shortUrl}",
            Url = LongUrl,
            Medium = medium,
            Requests = 0
        };
        var singleAdd = TableOperation.Insert(newUrl);
        await tableOut.ExecuteAsync(singleAdd);
        result.Add(new Result 
        {
            ShortUrl = $"{SHORTENER_URL}/{newUrl.RowKey}",
            LongUrl = WebUtility.UrlDecode(newUrl.Url),
            Medium = medium
        }); 
    }

    var operation = TableOperation.Replace(keyTable);
    await tableOut.ExecuteAsync(operation);

    log.Info($"Done.");
    return req.CreateResponse(HttpStatusCode.OK, result);
    
}
