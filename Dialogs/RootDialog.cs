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
            await ctx.PostAsync("Greetings...");
            ctx.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            await ctx.PostAsync("You said " + message.Text);

            LUISResponse response = await LUISService.askLUIS(message.Text);

            switch (response.topScoringIntent.intent)
            {
                case "Who worked on":
                    ctx.Call<object>(new WhoWorkedOnDialog(response), ResumeAfterWhoWorkedOnDialog);
                    break;
                case "GetYear":
                    ctx.Call<object>(new WhatYearDialog(response), ResumeAfterWhatYearDialog);
                    break;
                case "Tell me about":
                    ctx.Call<object>(new TellMeAboutDialog(response), ResumeAfterTellMeAboutDialog);
                    break;
                default:
                    ctx.Wait(MessageReceivedAsync);
                    break;
            }
        }

        private async Task ResumeAfterWhoWorkedOnDialog(IDialogContext ctx, IAwaitable<object> result)
        {
            ctx.Wait(MessageReceivedAsync);
        }

        private async Task ResumeAfterWhatYearDialog(IDialogContext ctx, IAwaitable<object> result)
        {
            ctx.Wait(MessageReceivedAsync);
        }

        private async Task ResumeAfterTellMeAboutDialog(IDialogContext ctx, IAwaitable<object> result)
        {
            ctx.Wait(MessageReceivedAsync);
        }
    }
}