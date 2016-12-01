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
    public class TellMeAboutDialog : IDialog<object> {
    
        Entities[] _entities;

        public TellMeAboutDialog(LUISResponse r) {
            _entities = r.entities;
        }

        private async Task ShowDetails(IDialogContext ctx)
        {
            if (_entities.Count() > 0) {
                var entity = _entities[0];
                MovieService movies = await MovieService.GetInstanceAsync();
                IEnumerable<MovieDetails> details = await movies.SearchMoviesAsync(entity.entity);
                if (details.Count() > 0) {
                    var detail = details.First();
                    var message = detail.title + " is a film released in " + detail.release_date;
                    await ctx.PostAsync(message);
                }
                else {
                    await ctx.PostAsync("Can't deal with less than one match right now");
                }
            }
            else {
                await ctx.PostAsync("Can't deal with more than one entity right now");
            }
        }

        public async Task StartAsync(IDialogContext ctx) {
            await ShowDetails(ctx);
            ctx.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> activity) {

            var message = await activity;

            if (message.Text == "who was in it") {
               ctx.PostAsync("Some guy"); 
            }
            else if (message.Text == "who directed it") {
               ctx.PostAsync("Some girl"); 
            }

            ctx.Wait(MessageReceivedAsync);
        }
    }
}