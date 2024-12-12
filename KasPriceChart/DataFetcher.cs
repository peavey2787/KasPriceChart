using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace KasPriceChart
{
    public class DataFetcher
    {
        private readonly HttpClient _httpClient;

        public DataFetcher()
        {
            _httpClient = new HttpClient();
        }

        public async Task<double> FetchPriceData()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://api.kaspa.org/info/price");
                var json = JObject.Parse(response);
                return json["price"].ToObject<double>();
            }
            catch (Exception)
            {
                return 0; // Return 0 in case of any error
            }
        }

        public async Task<double> FetchHashrateData()
        {
            try
            {
                var response = await _httpClient.GetStringAsync("https://api.kaspa.org/info/hashrate?stringOnly=false");
                var json = JObject.Parse(response);
                return json["hashrate"].ToObject<double>();
            }
            catch (Exception)
            {
                return 0; // Return 0 in case of any error
            }
        }
    }
}


