using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;

namespace whoWasIn.Dialogs {
    
    public class TheMovieDB {
        private static string apiKey = "c5bfd90362222164104c5317ee8a93cb";

        private static async Task<dynamic> request(string uri) 
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri + "&api_key=" + apiKey);
            using (StreamReader reader = new StreamReader(request.GetResponse().GetResponseStream()))
            {
                var responseString = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<dynamic>(responseString);
            }
        }
                
        public static async Task<List<int>> lookupPeople(List<string> people) 
        {
            string baseUri = "https://api.themoviedb.org/3/search/person?language=en-US&include_adult=false";
            List<Task<dynamic>> reqs = new List<Task<dynamic>>();
            people.ForEach(p => reqs.Add(request(baseUri + "&query=" + p)));
            await Task.WhenAll(reqs);

            List<int> results = new List<int>();
            reqs.ForEach(req => {
                var json = req.Result;
                results.Add((int)json.results[0].id);  
            });
            return results;
        }

        public static Task<dynamic> discoverMovies(Dictionary<string, List<string>> queries) 
        {
            string baseUri = "https://api.themoviedb.org/3/discover/movie?&language=en-US&sort_by=popularity.desc&include_adult=false&include_video=false";

            if (queries.ContainsKey("cast")) {
                List<int> ids = lookupPeople(queries["cast"]).Result;
                baseUri += "&with_cast=" + string.Join(",", ids);
            }

            return request(baseUri);
        }
    }

    [Serializable]
    public class RootDialog : IDialog<object> {

        public async Task StartAsync(IDialogContext ctx) 
        {
            await ctx.PostAsync("You wanted to know about Bob and Al, right?");
            ctx.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            await ctx.PostAsync("You said " + message.Text + " but you meant to ask something else");
            await ctx.PostAsync("They both acted in");
            Dictionary<string, List<string>> query = new Dictionary<string, List<string>>()
            {
                {  "cast", new List<string>() { "al pacino", "robert de niro" } }
            };

            JObject movies = await TheMovieDB.discoverMovies(query);
            // this stuff just drives me mad !!!
            JToken results = movies.First.Next.First;
            foreach (var movie in results.Children()) {
                await ctx.PostAsync(movie["title"].ToString());
            }

            ctx.Wait(MessageReceivedAsync);
        }
    }
}