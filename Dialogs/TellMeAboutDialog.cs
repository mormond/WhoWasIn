using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using whoWasIn.Services;
using whoWasIn.Services.LUISService;
using whoWasIn.Shared;

namespace whoWasIn.Dialogs {
    [Serializable]
    public class TellMeAboutDialog : IDialog<string> {

        string _entity = null;
                
        public TellMeAboutDialog(LUISResponse r) {
            if (r.entities.Count() > 0) {
                _entity = r.entities.First().entity;
            }
        }

        public TellMeAboutDialog(string movieName) {
            _entity = movieName;
        }

        private async Task<MovieDetails> GetMovieDetails(string movieName) {
            MovieService movies = await MovieService.GetInstanceAsync();
            IEnumerable<MovieDetails> details = await movies.SearchMoviesAsync(movieName);
            if (details.Count() > 0) {
                return details.First();
            }
            return null;
        }

        private async Task<IEnumerable<CastCredit>> GetMovieCastCredits(int movieId) {
            MovieService movies = await MovieService.GetInstanceAsync();
            return await movies.GetMovieCastCreditsAsync(movieId);
        }

        private async Task<IEnumerable<CrewCredit>> GetMovieCrewCredits(int movieId) {
            MovieService movies = await MovieService.GetInstanceAsync();
            return await movies.GetMovieCrewCreditsAsync(movieId);
        }

        private async Task ShowDetails(IDialogContext ctx)
        {
            MovieDetails details = await GetMovieDetails(_entity);
            if (details != null) {

                IEnumerable<CastCredit> credits = await GetMovieCastCredits(details.id);
                credits = credits.OrderBy(x => x.order).Take(3);

                var starring = " starring ";
                foreach (var c in credits) {
                   starring += c.name;
                   starring += ", "; 
                }

                starring = starring.Remove(starring.Length - 2);
                var idx = starring.LastIndexOf(",");
                if (idx >= 0) {
                    starring = starring.Substring(0, starring.Length - (starring.Length - idx)) + " and " + starring.Substring(idx + 2, starring.Length - idx - 2);
                }

                var message = details.title + " is a film released in " + details.release_date.Substring(0, 4) + starring;
                await ctx.PostAsync(message);
            }
        }

        public async Task StartAsync(IDialogContext ctx) {
            await ShowDetails(ctx);
            ctx.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> activity) {

            var message = await activity;


            if (message.Text == "who was in it") {
                ctx.Wait(MessageReceivedAsync);
            }
            else if (message.Text == "who directed it") {
                MovieDetails details = await GetMovieDetails(_entity);
                if (details != null) {
                    IEnumerable<CrewCredit> credits = await GetMovieCrewCredits(details.id);
                    foreach (var c in credits) {
                        if (c.job == "Director") {
                            await ctx.PostAsync(c.name);
                        }
                        System.Diagnostics.Debug.WriteLine(c.job);
                    }
                }
                ctx.Wait(MessageReceivedAsync);
            }
            else {
                Regex regex = new Regex(@"^who did the (?<job>.+)|^who wrote the (?<job>.+)|^who did (?<job>.+)|^who was the (?<job>.+)|");
                var match = regex.Match(message.Text);
                if (match.Groups["job"] != null && match.Groups["job"].Value.Length > 0) {
                    var job = match.Groups["job"];

                    bool matched = false;
                    MovieDetails details = await GetMovieDetails(_entity);
                    if (details != null) {
                        IEnumerable<CrewCredit> credits = await GetMovieCrewCredits(details.id);
                        foreach (var c in credits) {
                            if (String.Equals(c.job, job.Value, StringComparison.OrdinalIgnoreCase)) {
                                matched = true;
                                await ctx.PostAsync(c.name);
                            }
                            System.Diagnostics.Debug.WriteLine(c.job);
                        }
                    }
                    if (!matched) {
                        await ctx.PostAsync("Sorry.. couldn't find anyone who did that on this title");
                    }
                    ctx.Wait(MessageReceivedAsync);
                }
                else {
                    ctx.Done(message.Text);
                }
            }
        }

        public async Task Resume(IDialogContext ctx, IAwaitable<string> result) {
                ctx.Wait(MessageReceivedAsync);
        }
    }
}