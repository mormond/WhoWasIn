using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
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
            IEnumerable<Shared.MovieDetails> movies = await GetMovies();

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

        private async Task<IEnumerable<Shared.MovieDetails>> GetMovies()
        {
            MovieService movieService = await MovieService.GetInstanceAsync();
            List<string> peopleList = new List<string>();
            foreach (Entities detected in this.response.entities)
            {
                if (!peopleList.Contains(detected.entity))
                {
                    peopleList.Add(detected.entity);
                }
            }

            var movies = await movieService.DiscoverMoviesWithPeopleAsync(peopleList.ToArray());
            return movies;
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument) {

            var message = await argument;

            switch (this.state) {
                case WhoWorkedState.UserPrompted:
                    bool carryOn = ParseReplyToPrompt(message.Text);
                    this.state = WhoWorkedState.UserRepliedToPrompt;
                    if (carryOn) {
                        List<string> movieOptions = new List<string>();
                        IEnumerable<Shared.MovieDetails> movies = await GetMovies();
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