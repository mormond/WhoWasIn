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
    public class TellMeAboutDialog : IDialog<object> {
    
        //Entities[] _entities;

        public TellMeAboutDialog(LUISResponse r) {
            _entities = r.entities;
        }

        public async Task StartAsync(IDialogContext ctx) {
            await ctx.PostAsync("I've got " + _entities.Count() + " things to tell you about");

            if (_entities.Count() == 1) {
                await ctx.PostAsync("stuff");
            }
            else {
                await ctx.PostAsync("Thinking time");
            }

            ctx.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument) {
            ctx.Wait(MessageReceivedAsync);
        }
    }
}