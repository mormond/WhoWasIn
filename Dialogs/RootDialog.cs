namespace whoWasIn.Dialogs
{
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Connector;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using whoWasIn.Services.LUISService;

    [Serializable]
    public class RootDialog : IDialog<object>
    {

        public async Task StartAsync(IDialogContext ctx)
        {
            await ctx.PostAsync("You wanted to know about Bob and Al, right?");
            ctx.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            await ctx.PostAsync("You said " + message.Text + " but you meant to ask something else");

            LUISResponse response = await LUISService.askLUIS(message.Text);

            switch (response.topScoringIntent.intent)
            {
                case "Who worked on":
                    ctx.Call<object>(new WhoWorkedOnDialog(response), null);
                    break;
                case "GetYear":
                    ctx.Call<object>(new WhatYearDialog(response), null);
                    break;
                case "Tell me about":
                    ctx.Call<object>(new TellMeAboutDialog(response), null);
                    break;
                default:
                    ctx.Wait(MessageReceivedAsync);
                    break;
            }
        }

        private async Task ResumeAfterWhoWorkedOnDialog(IDialogContext context, IAwaitable<int> result)
        {
        }
        private async Task ResumeAfterWhatYearDialog(IDialogContext context, IAwaitable<int> result)
        {
        }
        private async Task ResumeAfterTellMeAbout(IDialogContext context, IAwaitable<int> result)
        {
        }
    }
}