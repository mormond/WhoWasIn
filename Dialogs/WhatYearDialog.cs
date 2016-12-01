using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using whoWasIn.Services.LUISService;

namespace whoWasIn.Dialogs
{
    [Serializable]
    public class WhatYearDialog : IDialog<object>
    {
        LUISResponse response;

        public WhatYearDialog(LUISResponse r)
        {
            response = r;
        }

        public async Task StartAsync(IDialogContext ctx)
        {
            await ctx.PostAsync("What year dialog");
            ctx.Wait(MessageReceivedAsync);

            ctx.Done<object>(null);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument)
        {
            ctx.Wait(MessageReceivedAsync);
        }
    }
}