﻿namespace whoWasIn.Dialogs
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
            //await ctx.PostAsync("Greetings...");
            ctx.Wait(MessageReceivedAsync);
        }

        private async Task askLuis(IDialogContext ctx, string message)
        {
            LUISResponse response = await LUISService.askLUIS(message);

            switch (response.topScoringIntent.intent)
            {
                case "Who worked on":
                    ctx.Call<object>(new WhoWorkedOnDialog(response), ResumeAfterWhoWorkedOnDialog);
                    break;
                case "GetYear":
                    ctx.Call<object>(new WhatYearDialog(response), ResumeAfterWhatYearDialog);
                    break;
                case "Tell me about":
                    ctx.Call<string>(new TellMeAboutDialog(response), ResumeAfterTellMeAboutDialog);
                    break;
                default:
                    await ctx.PostAsync("Sorry, didn't quite get that. Try something like \"What films has Cary Grant been in?\"");
                    ctx.Wait(MessageReceivedAsync);
                    break;
            }
        }

        public async Task MessageReceivedAsync(IDialogContext ctx, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            //await ctx.PostAsync("You said " + message.Text);
            await askLuis(ctx, message.Text);
        }

        private async Task ResumeAfterWhoWorkedOnDialog(IDialogContext ctx, IAwaitable<object> result)
        {
            var utterance = await result;
            ctx.Wait(MessageReceivedAsync);
        }

        private async Task ResumeAfterWhatYearDialog(IDialogContext ctx, IAwaitable<object> result)
        {
            var utterance = await result;
            ctx.Wait(MessageReceivedAsync);
        }

        private async Task ResumeAfterTellMeAboutDialog(IDialogContext ctx, IAwaitable<string> result)
        {
            var utterance = await result;
            if (utterance != "") {
                await askLuis(ctx, utterance);
            }
            else {
                ctx.Wait(MessageReceivedAsync);
            }
        }
    }
}