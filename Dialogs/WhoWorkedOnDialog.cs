using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using whoWasIn.Services;
using whoWasIn.Services.LUISService;

namespace whoWasIn.Dialogs {

    [Serializable]
    public class WhoWorkedOnDialog : IDialog<object> {
        private LUISResponse response;

        public WhoWorkedOnDialog(LUISResponse r) {
            this.response = r;
        }

        public async Task StartAsync(IDialogContext ctx) {

            MovieService movieService = await MovieService.GetInstanceAsync();
            List<string> peopleList = new List<string>();
            foreach (Entities detected in this.response.entities)
            {
                if (!peopleList.Contains(detected.entity)) {
                    peopleList.Add(detected.entity);
                }
            }

            var movies = await movieService.DiscoverMoviesWithPeopleAsync(peopleList.ToArray());

            if (movies.Count() > 0) {
                StringBuilder builder = new StringBuilder();
                foreach (var movie in movies) {
                    builder.Append($"{movie.title}\n\n");
                }

                await ctx.PostAsync($"They worked on {movies.Count()} films together. Here is a list of them:\n\n{builder.ToString()}");
            }
            else{
                await ctx.PostAsync("As far as I know, they haven't been in any films together");
            }

            ctx.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument) {

            ctx.Wait(MessageReceivedAsync);
        }
    }
}