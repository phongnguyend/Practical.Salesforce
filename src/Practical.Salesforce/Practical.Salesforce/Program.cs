﻿using CryptographyHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Salesforce.Common;
using Salesforce.Force;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
                    //var described = await forceClient.DescribeAsync<object>("Lead");
                    var query = @"select Id, Name, Company, LeadSource, Email, Phone, Description, Address,
                    Owner.Id, Owner.Name, Owner.Email, Owner.UserName,
                    IsConverted, ConvertedDate,
                    ConvertedOpportunityId, ConvertedOpportunity.Id, ConvertedOpportunity.Name, ConvertedOpportunity.StageName,
                    ConvertedContactId, ConvertedContact.Id, ConvertedContact.Name,
                    ConvertedAccountId, ConvertedAccount.Id, ConvertedAccount.Name
                    from Lead";

                    var result = await forceClient.QueryAsync<Lead>(query);
                    var objects = result.Records;

                    await CreateLead(forceClient, values["access_token"]);
                    result = await forceClient.QueryAsync<Lead>(query);
                    objects = result.Records;

                    await UpdateLead(forceClient, objects.First(x => x.Email == "test@abc.com"));
                    result = await forceClient.QueryAsync<Lead>(query);
                    objects = result.Records;

                    await DeleteLead(forceClient, objects.First(x => x.Email == "test@abc.com"));
                    result = await forceClient.QueryAsync<Lead>(query);
                    objects = result.Records;
                }
            }
        }

        private static async Task CreateLead(ForceClient forceClient, string token)
        {
            var usersResult = await forceClient.QueryAsync<User>("select Id, Name, Email, UserName, FullPhotoUrl from User where Name like '%Phong%'");
            var users = usersResult.Records;
            var owner = users.FirstOrDefault();

            await DownloadPhoto(token, owner);

            var newObject = new Lead
            {
                FirstName = "Test First Name",
                LastName = "Test Last Name",
                Company = "Test Company",
                LeadSource = "LinkedIn",
                Email = "test@abc.com",
                Phone = "Test Phone",
                Description = "Test Description",
                Country = "VN",
                OwnerId = owner.Id,
            };

            var created = await forceClient.CreateAsync("Lead", newObject);
        }

        private static async Task DownloadPhoto(string token, User owner)
        {
            /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types
            var mineTypeToExtentionMappings = new Dictionary<string, string>()
            {
                { "image/png","png" }
            };

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.GetAsync(owner.FullPhotoUrl);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();

            var ext = mineTypeToExtentionMappings[response.Content.Headers.ContentType.MediaType];
            File.WriteAllBytes($"{owner.Id}.{ext}", imageBytes);
        }

        private static async Task UpdateLead(ForceClient forceClient, Lead lead)
        {
            var updateObject = new Dictionary<string, object>
            {
                { "Company", $"Test Company {DateTimeOffset.Now}" },
                { "Description", null }
            };

            var updated = await forceClient.UpdateAsync("Lead", lead.Id, updateObject);
        }

        private static async Task DeleteLead(ForceClient forceClient, Lead lead)
        {
            var deleted = await forceClient.DeleteAsync("Lead", lead.Id);
        }
    }
}
