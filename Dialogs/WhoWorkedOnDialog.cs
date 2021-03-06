﻿using Microsoft.Bot.Builder.Dialogs;
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

    public enum Trinary
    {
        Yes,
        No,
        Unsure
    }

    [Serializable]
    public class WhoWorkedOnDialog : IDialog<string> {
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
                    await ctx.PostAsync($"I have found {movies.Count()} films. Here is a list of them:\n\n{builder.ToString()}\n\nWould you like to know more about {filmText}?");
                    this.state = WhoWorkedState.UserPrompted;
                }
                else
                {
                    await ctx.PostAsync("There aren't any that I know.");
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
                    Trinary carryOn = ParseReplyToPrompt(message.Text);
                    this.state = WhoWorkedState.UserRepliedToPrompt;
                    switch (carryOn) {
                        case Trinary.Yes:
                            List<string> movieOptions = new List<string>();
                            IEnumerable<Shared.MovieDetails> movies = await GetMovies(GetPeopleList());
                            foreach (var movie in movies)
                            {
                                movieOptions.Add(movie.title);
                            }
                            PromptOptions<string> promptOptions = new PromptOptions<string>(prompt: "Which one?", options: movieOptions);
                            PromptDialog.Choice<string>(ctx, Resume, promptOptions);
                            break;

                        case Trinary.No:
                            await ctx.PostAsync("OK, maybe another time.");
                            ctx.Done("");
                            break;

                        default:
                            ctx.Done(message.Text);
                            break;
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

        private Trinary ParseReplyToPrompt(string reply) {

            Trinary result = Trinary.No;
            bool response = false;

            string pattern = "^(yes|yup|y|yeah)";
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            response = regex.IsMatch(reply);
            if (response)
            {
                result = Trinary.Yes;
            }

            if (!response)
            {
                pattern = "^(no|nope|n|nah)";
                regex = new Regex(pattern, RegexOptions.IgnoreCase);
                if (!regex.IsMatch(reply))
                {
                    result = Trinary.Unsure;
                }
            }

            return result;
        }
    }
}