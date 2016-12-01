using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using whoWasIn.Services.LUISService;

namespace whoWasIn.Dialogs {

    [Serializable]
    public class WhoWorkedOnDialog : IDialog<object> {

        public WhoWorkedOnDialog(LUISResponse r) {
        }

        public async Task StartAsync(IDialogContext ctx) {
            await ctx.PostAsync("Who worked on dialog");
            ctx.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument) {
            ctx.Wait(MessageReceivedAsync);
        }
    }
}