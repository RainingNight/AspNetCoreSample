using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Newtonsoft.Json.Linq;

namespace ConsoleClient
{
    class Program
    {
        public static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            var client = new HttpClient();

            // discover endpoints from metadata     

            // Obsolete
            //var disco = await DiscoveryClient.GetAsync("https://oidc.faasx.com/");

            // New: HttpClient extension methods
            var disco = await client.GetDiscoveryDocumentAsync("https://oidc.faasx.com/");

            // request token

            // Obsolete
            //var tokenClient = new TokenClient(disco.TokenEndpoint, "client.cc", "secret");
            //var tokenResponse = await tokenClient.RequestClientCredentialsAsync("api");

            // New: HttpClient extension methods
            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client.cc",
                ClientSecret = "secret",
                Scope = "api"
            });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return;
            }

            Console.WriteLine(tokenResponse.Json);
            Console.WriteLine("\n\n");

            // call api
            client.SetBearerToken(tokenResponse.AccessToken);
            var response = await client.GetAsync("http://localhost:5200/api/SampleData/WeatherForecasts");
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine(response.StatusCode);
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine(JArray.Parse(content));
            }
        }
    }
}
