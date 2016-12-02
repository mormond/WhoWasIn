using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using whoWasIn.Services;
using whoWasIn.Services.LUISService;
using whoWasIn.Shared;

namespace whoWasIn.Dialogs {

    [Serializable]
    public class WhatYearDialog : IDialog<object>
    {
        LUISResponse response;
        List<MovieDetails> movieList;
        string entityName;

        public WhatYearDialog(LUISResponse r)
        {
            response = r;
        }

        public async Task StartAsync(IDialogContext ctx)
        {
            //await ctx.PostAsync("What year dialog");

            if (response.entities.Count() == 0)
            {
                await ctx.PostAsync(string.Format("I'm sorry, I couldn't find any matches."));
                ctx.Done<object>(null);
            }

            foreach (var item in response.entities)
            {
                if (item.@type == "builtin.encyclopedia.film.film")
                {
                    entityName = item.entity;
                    movieList = new List<MovieDetails>(await LookupMovieAsync(entityName));
                }
            }

            if (movieList.Count() > 1)
            {
                await ctx.PostAsync(string.Format("I found {0} results containing the term '{1}'. I'm guessing you probably meant this one...", movieList.Count(), entityName));
            }
            else
            {
                await ctx.PostAsync("I found the following movie.");
            }

            List<Attachment> attachmentList = new List<Attachment>();

            foreach (var item in movieList)
            {
                CardImage image = new CardImage(item.poster_path);
                List<CardImage> images = new List<CardImage>() { image };

                var x = await LookupImdbAsync(item.id);

                ThumbnailCard h = new ThumbnailCard()
                {
                    Title = item.title,
                    Subtitle = string.IsNullOrEmpty(item.release_date) ? "" : item.release_date.Substring(0, 4),
                    //Tap = new CardAction(ActionTypes.OpenUrl, "Visit in IMDB", value: string.Format("http://www.imdb.com/title/{0}", x)),
                    Text = item.overview_short,
                    Images = images,
                    Buttons = new List<CardAction>() { new CardAction(ActionTypes.OpenUrl, "Learn more", value: string.Format("http://www.imdb.com/title/{0}", x)) }
                };

                attachmentList.Add(h.ToAttachment());
            }

            var reply = ctx.MakeMessage();
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            reply.Attachments = attachmentList;
            await ctx.PostAsync(reply);

            //ctx.Wait(MessageReceivedAsync);

            ctx.Done<object>(null);
        }

        async private Task<IEnumerable<MovieDetails>> LookupMovieAsync(string searchString)
        {
            MovieService movieService = await MovieService.GetInstanceAsync();
            return await movieService.SearchMoviesAsync(searchString);
        }

        async private Task<string> LookupImdbAsync(int id)
        {
            MovieService movieService = await MovieService.GetInstanceAsync();
            return await movieService.GetImdbId(id);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument)
        {
            ctx.Wait(MessageReceivedAsync);
        }
    }
}