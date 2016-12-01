namespace whoWasIn.Services
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Shared;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Configuration;

    public class MovieService
    {
        const string BaseUrlString = "http://api.themoviedb.org/";
        const string VersionString = "3";
        const string ConfigMethodString = "configuration";
        const string GenresMethodString = "genre/movie/list";
        const string SearchMovieMethodString = "search/movie";
        const string SearchPeopleMethodString = "search/person";
        const string SearchMultiMethodString = "search/multi";
        const string DiscoverMethodString = "discover/movie";
        const string ApiKeySettingName = "TMD_APIIKey";

        private ConfigRoot config;
        private GenresRoot genres;
        private static MovieService instance;
        private bool initialized = false;
        private string ApiKey;

        private MovieService()
        {
            ApiKey = WebConfigurationManager.AppSettings[ApiKeySettingName];
        }

        private async Task<MovieService> InitializeAsync()
        {
            if (!initialized)
            {
                var queryParams = GetBaseParams();
                var configString = await MakeRequestAsync(ConfigMethodString, queryParams);
                var genresString = await MakeRequestAsync(GenresMethodString, queryParams);

                if (configString != null && genresString != null)
                {
                    this.config = JsonConvert.DeserializeObject<ConfigRoot>(configString);
                    this.genres = JsonConvert.DeserializeObject<GenresRoot>(genresString);
                    initialized = true;
                }
            }
            return this;
        }

        private Dictionary<string, string> GetBaseParams()
        {
            return new Dictionary<string, string>() { { "api_key", ApiKey } };
        }

        private async Task<string> MakeRequestAsync(string method, Dictionary<string, string> parameters)
        {
            if (method != ConfigMethodString && method != GenresMethodString && !initialized)
            {
                await this.InitializeAsync();
            }

            UriBuilder b = new UriBuilder(BaseUrlString);
            StringBuilder queryString = new StringBuilder();

            b.Path = VersionString + "/" + method;

            foreach (var parameter in parameters)
            {
                queryString.Append(string.Format("{0}={1}&", parameter.Key, parameter.Value));
            }

            b.Query = queryString.ToString().TrimEnd('&');
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("accept", "application/json");

                try
                {
                    using (var response = await httpClient.GetAsync(b.Uri))
                    {
                        return await response.Content.ReadAsStringAsync();
                    }

                }
                catch (HttpRequestException ex)
                {
                    throw ex;
                }
            }
        }

        private IEnumerable<MovieDetails> ParseMovieDetailsResponse(string jsonResultsString)
        {
            JObject searchResult = JObject.Parse(jsonResultsString);

            var query = from r in searchResult["results"]
                        select new MovieDetails()
                        {
                            overview = (string)r["overview"],
                            overview_short = ((string)r["overview"]).Length > 100 ? ((string)r["overview"]).Substring(0, 100) + "..." : (string)r["overview"],
                            release_date = (string)r["release_date"],
                            backdrop_path = config.images.base_url + config.images.backdrop_sizes[1] + (string)r["backdrop_path"],
                            poster_path = config.images.base_url + config.images.poster_sizes[1] + (string)r["poster_path"],
                            vote_count = (int)r["vote_count"],
                            vote_average = (float)r["vote_average"],
                            title = (string)r["title"]
                        };

            return query;
        }

        private IEnumerable<PersonDetails> ParsePersonDetailsResponse(string jsonResultsString)
        {
            JObject searchResult = JObject.Parse(jsonResultsString);

            var query = from r in searchResult["results"]
                        select new PersonDetails()
                        {
                            name = (string)r["name"],
                            id = (int)r["id"]
                        };

            return query;
        }

        private IEnumerable<TvShowDetails> ParseTvShowDetailsResponse(string jsonResultsString)
        {
            JObject searchResult = JObject.Parse(jsonResultsString);

            var query = from r in searchResult["results"]
                        select new TvShowDetails()
                        {
                            overview = (string)r["overview"],
                            backdrop_path = config.images.base_url + config.images.backdrop_sizes[1] + (string)r["backdrop_path"],
                            poster_path = config.images.base_url + config.images.poster_sizes[1] + (string)r["poster_path"],
                            vote_count = (int)r["vote_count"],
                            vote_average = (float)r["vote_average"],
                            name = (string)r["name"]
                        };

            return query;
        }

        public static async Task<MovieService> GetInstanceAsync()
        {
            if (instance == null)
            {
                instance = new MovieService();
                await instance.InitializeAsync();
            }
            return instance;
        }

        public async Task<IEnumerable<MovieDetails>> SearchMoviesAsync(string searchTerm)
        {
            var queryParams = GetBaseParams();
            queryParams.Add("query", searchTerm);
            string resultString = await MakeRequestAsync(SearchMovieMethodString, queryParams);

            return ParseMovieDetailsResponse(resultString);
        }

        public async Task<MultiResults> SearchMultiAsync(string searchTerm)
        {
            var queryParams = GetBaseParams();
            queryParams.Add("query", searchTerm);
            string resultString = await MakeRequestAsync(SearchMultiMethodString, queryParams);

            return ParseMultiResponse(resultString);
        }

        private MultiResults ParseMultiResponse(string jsonResultsString)
        {
            JObject searchResult = JObject.Parse(jsonResultsString);

            var movieQuery = from r in searchResult["results"]
                        where (string)r["media_type"] == "movie"
                        select new MovieDetails()
                        {
                            overview = (string)r["overview"],
                            overview_short = ((string)r["overview"]).Length > 100 ? ((string)r["overview"]).Substring(0, 100) + "..." : (string)r["overview"],
                            release_date = (string)r["release_date"],
                            backdrop_path = config.images.base_url + config.images.backdrop_sizes[1] + (string)r["backdrop_path"],
                            poster_path = config.images.base_url + config.images.poster_sizes[1] + (string)r["poster_path"],
                            vote_count = (int)r["vote_count"],
                            vote_average = (float)r["vote_average"],
                            title = (string)r["title"]
                        };

            var personQuery = from r in searchResult["results"]
                             where (string)r["media_type"] == "person"
                              select new PersonDetails()
                              {
                                  name = (string)r["name"],
                                  id = (int)r["id"]
                              };

            var tvShowQuery = from r in searchResult["results"]
                             where (string)r["media_type"] == "tv"
                             select new TvShowDetails()
                             {
                                 overview = (string)r["overview"],
                                 backdrop_path = config.images.base_url + config.images.backdrop_sizes[1] + (string)r["backdrop_path"],
                                 poster_path = config.images.base_url + config.images.poster_sizes[1] + (string)r["poster_path"],
                                 vote_count = (int)r["vote_count"],
                                 vote_average = (float)r["vote_average"],
                                 name = (string)r["name"]
                             };

            MultiResults results = new MultiResults();
            results.Movies = movieQuery;
            results.People = personQuery;
            results.TvShows = tvShowQuery;

            return results;
        }

        // URL: /discover/movie?primary_release_year=2010&sort_by=vote_average.desc
        public async Task<IEnumerable<MovieDetails>> DiscoverBestMoviesByYearAsync(string year)
        {
            var queryParams = GetBaseParams();
            queryParams.Add("primary_release_year", year);
            queryParams.Add("sort_by", "popularity.desc");
            string resultString = await MakeRequestAsync(DiscoverMethodString, queryParams);

            return ParseMovieDetailsResponse(resultString);
        }

        //URL: /discover/movie? with_genres = 18 & primary_release_year = 2014
        public async Task<IEnumerable<MovieDetails>> DiscoverBestMoviesByGenreByYearAsync(string genre, string year)
        {
            var queryParams = GetBaseParams();
            queryParams.Add("primary_release_year", year);
            queryParams.Add("with_genres", this.genres.genres.Where(g => g.name == genre).First().id.ToString());
            queryParams.Add("sort_by", "popularity.desc");
            string resultString = await MakeRequestAsync(DiscoverMethodString, queryParams);

            return ParseMovieDetailsResponse(resultString);
        }

        //URL: /search/person?query=brad%20pitt
        public async Task<IEnumerable<PersonDetails>> FindPeopleAsync(string name)
        {
            var queryParams = GetBaseParams();
            queryParams.Add("query", name);
            string resultString = await MakeRequestAsync(SearchPeopleMethodString, queryParams);

            return ParsePersonDetailsResponse(resultString);
        }

        // URL: /discover/movie? with_people = 108916,7467&sort_by=popularity.desc
        public async Task<IEnumerable<MovieDetails>> DiscoverMoviesWithPeopleAsync(params string[] people)
        {
            List<Task<IEnumerable<PersonDetails>>> taskList = new List<Task<IEnumerable<PersonDetails>>>();

            foreach (var item in people)
            {
                taskList.Add(FindPeopleAsync(item));
            }

            await Task.WhenAll(taskList);

            StringBuilder peopleList = new StringBuilder();

            foreach (var item in taskList)
            {
                if (item.Result != null)
                {
                    peopleList.Append(string.Format("{0},", item.Result.FirstOrDefault().id));
                }
                else
                {
                    return new List<MovieDetails>();
                }
            }

            peopleList.Remove(peopleList.Length-1, 1);            

            var queryParams = GetBaseParams();
            queryParams.Add("with_people", peopleList.ToString());
            queryParams.Add("sort_by", "popularity.desc");
            string resultString = await MakeRequestAsync(DiscoverMethodString, queryParams);

            return ParseMovieDetailsResponse(resultString);
        }
    }
}

