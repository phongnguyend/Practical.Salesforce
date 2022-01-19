using CryptographyHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Salesforce.Common;
using Salesforce.Force;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Practical.Salesforce
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            using (var authenticationClient = new AuthenticationClient())
            {
                var config = new ConfigurationBuilder()
                //.AddJsonFile("appsettings.json")
                .AddUserSecrets("871c4384-4147-4331-96cd-71e635956419")
                .Build();

                var rsa = RSA.Create();
                rsa.ImportFromPem(File.ReadAllText(config["KeyPath"]).ToCharArray());

                string jwtHeader = "{\"alg\":\"RS256\"}";
                string iss = config["Iss"];
                string aud = "https://test.salesforce.com";
                string sub = config["Sub"];
                string exp = DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds().ToString();
                string jwtPayload = "{" + string.Format("\"iss\": \"{0}\", \"sub\": \"{1}\", \"aud\": \"{2}\", \"exp\": \"{3}\"", iss, sub, aud, exp) + "}";

                var jwt = Base64UrlEncoder.Encode(jwtHeader.GetBytes()) + "." + Base64UrlEncoder.Encode(jwtPayload.GetBytes());

                var signature = Base64UrlEncoder.Encode(rsa.SignData(jwt.GetBytes(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

                jwt += "." + signature;

                var httpClient = new HttpClient();
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                    new KeyValuePair<string, string>("assertion", jwt)
                });

                var response = await httpClient.PostAsync("https://test.salesforce.com/services/oauth2/token", formContent);
                var text = await response.Content.ReadAsStringAsync();
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);

                using (var forceClient = new ForceClient(values["instance_url"], values["access_token"], "v51.0"))
                {
                    var described = await forceClient.DescribeAsync<object>("ObjectName");
                    var query = "select Field1, Field1, Field1, Field1, Field1 from ObjectName where Field1 = 'ABC'";
                    var result = await forceClient.QueryAsync<SalesforceObject>(query);
                    var objects = result.Records;

                    var newObject = new SalesforceObject
                    {
                        Field1 = "Test",
                        Field2 = "Test",
                        Field3 = "Test@abc.com",
                        Field4 = "Test"
                    };

                    var created = await forceClient.CreateAsync("ObjectName", newObject);

                    result = await forceClient.QueryAsync<SalesforceObject>(query);
                    objects = result.Records;
                }
            }
        }
    }
}
