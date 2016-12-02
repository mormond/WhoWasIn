using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using whoWasIn.Services;
using whoWasIn.Services.LUISService;

namespace whoWasIn.Dialogs {

    public enum WhoWorkedState
    {
        MessageReceived,
        MessageProcessed,
        UserPrompted,
        UserRepliedToPrompt
    }

    [Serializable]
    public class WhoWorkedOnDialog : IDialog<object> {
        private LUISResponse response;
        //private IEnumerable<Shared.MovieDetails> movieList;
        private WhoWorkedState state;

        public WhoWorkedOnDialog(LUISResponse r) {
            this.response = r;
            this.state = WhoWorkedState.MessageReceived;
        }

        public async Task StartAsync(IDialogContext ctx)
        {
            List<string> peopleList = GetPeopleList();
            if (peopleList.Count > 0)
            {
                await ctx.PostAsync(GetPeopleListMessage(peopleList));

                IEnumerable<Shared.MovieDetails> movies = await GetMovies(peopleList);

                if (movies.Count() > 0)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var movie in movies)
                    {
                        builder.Append($"{movie.title}\n\n");
                    }

                    //this.movieList = movies;
                    string filmText = movies.Count() > 1 ? "any of those movies" : "that film";
                    await ctx.PostAsync($"They have been in {movies.Count()} films together. Here is a list of them:\n\n{builder.ToString()}\n\nWould you like to know more about {filmText}?");
                    this.state = WhoWorkedState.UserPrompted;
                }
                else
                {
                    await ctx.PostAsync("As far as I know, they haven't been in any films together");
                    this.state = WhoWorkedState.MessageProcessed;
                    ctx.Done("No results found");
                }

                ctx.Wait(MessageReceivedAsync);
            }
            else
            {
                await ctx.PostAsync("I didn't recognise the names of any actors, could you try some others?");
                ctx.Done("Done");
            }
        }

        private string GetPeopleListMessage(List<string> peopleList)
        {
            StringBuilder peopleListBuilder = new StringBuilder();
            peopleListBuilder.Append("I'm looking up films that have ");
            TextInfo textInfo = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo;
            int i = 1;
            foreach (string person in peopleList)
            {
                peopleListBuilder.Append($"{textInfo.ToTitleCase(person)}");
                if (i < peopleList.Count)
                {
                    if (peopleList.Count - i > 1)
                    {
                        peopleListBuilder.Append(", ");
                    }
                    else
                    {
                        peopleListBuilder.Append(" and ");
                    }
                    i++;
                }
            }
            peopleListBuilder.Append(" in.");
            return peopleListBuilder.ToString();
        }

        private async Task<IEnumerable<Shared.MovieDetails>> GetMovies(List<string> peopleList)
        {
            MovieService movieService = await MovieService.GetInstanceAsync();
            var movies = await movieService.DiscoverMoviesWithPeopleAsync(peopleList.ToArray());
            return movies;
        }

        private List<string> GetPeopleList()
        {
            List<string> peopleList = new List<string>();
            foreach (Entities detected in this.response.entities)
            {
                if (!peopleList.Contains(detected.entity))
                {
                    peopleList.Add(detected.entity);
                }
            }

            return peopleList;
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument) {

            var message = await argument;

            switch (this.state) {
                case WhoWorkedState.UserPrompted:
                    bool carryOn = ParseReplyToPrompt(message.Text);
                    this.state = WhoWorkedState.UserRepliedToPrompt;
                    if (carryOn) {
                        List<string> movieOptions = new List<string>();
                        IEnumerable<Shared.MovieDetails> movies = await GetMovies(GetPeopleList());
                        foreach (var movie in movies)
                        {
                            movieOptions.Add(movie.title);
                        }
                        PromptOptions<string> promptOptions = new PromptOptions<string>(prompt: "Which one?", options: movieOptions);
                        PromptDialog.Choice<string>(ctx, Resume, promptOptions);
                    }
                    else {
                        await ctx.PostAsync("OK, maybe another time.");
                        ctx.Done("Complete");
                    }
                    break;

                default:
                    ctx.Wait(MessageReceivedAsync);
                    break;
            }

            
        }

        private async Task Resume(IDialogContext context, IAwaitable<string> result)
        {
            string movieTitle = await result;
            await context.PostAsync($"You picked {movieTitle}");
            context.Call(new TellMeAboutDialog(movieTitle), ResumeAfterTellMe);
        }

        private async Task ResumeAfterTellMe(IDialogContext context, IAwaitable<object> result) {
            await result;
            context.Done(result);
        }

        private bool ParseReplyToPrompt(string reply) {

            bool result = false;

            string pattern = "^(yes|yup|y|yeah)";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            result = regex.IsMatch(reply);

            return result;
        }
    }
}