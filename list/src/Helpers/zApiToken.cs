using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;


namespace list.Helpers
{
    public class zApiToken
    {
        public static async Task<string> Identify(Guid guid, string ns)
        {
            string claims = string.Empty;

            string URL = "http://auth."+ ns +".svc" + "/api/token?guid=" + guid;


            var httpWebRequest = (HttpWebRequest)WebRequest.Create(URL);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";
            //httpWebRequest.Headers.Add("XApiKey", Environment.GetEnvironmentVariable("APIKEY"));
            //httpWebRequest.Headers.Add("Authorization", "Bearer " + await Authenticate.GetOpenIdToken());

            Console.WriteLine("URL: " + URL);
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                claims = streamReader.ReadToEnd();
                Console.WriteLine(claims);

                /*
                JsonElement o = JsonSerializer.Deserialize<JsonElement>(tmp);
                email = o.GetProperty("email").GetString();
                */
            }

            return claims;
        }
    }
}
