using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace whoWasIn.Services.LUISService 
{
    public class LUISResponse 
    { 
        public string query { get; set; } 
        public TopScoringIntent topScoringIntent { get; set; } 
        public Entities[] entities { get; set; } 
        public Dialog dialog { get; set; } 
    }

    public class TopScoringIntent 
    { 
        public string intent { get; set; } 
        public string score { get; set; } 
        public Actions[] actions { get; set; } 
    }
     
    public class Actions 
    { 
        public string triggered { get; set; } 
        public string name { get; set; } 
        public Parameters[] parameters { get; set; } 
    }
     
    public class Parameters 
    { 
        public string name { get; set; } 
        public string required { get; set; } 
        public Value[] value { get; set; } 
    }
     
    public class Value 
    { 
        public string entity { get; set; } 
        public string type { get; set; } 
    } 
 
    public class Entities 
    { 
        public string entity { get; set; } 
        public string type { get; set; } 
        public string startIndex { get; set; } 
        public string endIndex { get; set; } 
        public string score { get; set; } 
        public Resolution resolution { get; set; } 
    }
     
    public class Resolution 
    { 
        public string date { get; set; } 
    }
     
    public class Dialog 
    { 
        public string prompt { get; set; } 
        public string parameterName { get; set; } 
        public string contextId { get; set; } 
        public string status { get; set; } 
    } 

    public class LUISService 
    {
        private static string _baseUri = "https://api.projectoxford.ai/luis/v2.0/apps/";

        public static async Task<LUISResponse> askLUIS(string utterance)
        { 
            using (var client = new HttpClient()) 
            { 
                string _appId = "010074d6-697c-45e3-a0d4-3f090a951134"; 
                string _subscriptionKey = "f9750ad3a1b74196a316dd23ef69af4e"; 

                string uri = _baseUri + "/" + _appId + "?subscription-key=" + _subscriptionKey; 
                uri += "&q=" + utterance + "&verbose=true";

                HttpResponseMessage response = await client.GetAsync(uri); 
                return JsonConvert.DeserializeObject<LUISResponse>(await response.Content.ReadAsStringAsync()); 
            }
        }
    }
}