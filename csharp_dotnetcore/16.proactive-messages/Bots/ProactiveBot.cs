// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using RestSharp;

namespace Microsoft.BotBuilderSamples
{
    public class ProactiveBot : ActivityHandler
    {
        // Message to send to users when the bot receives a Conversation Update event
        private const string WelcomeMessage = "Welcome to the Proactive Bot sample.  Navigate to http://localhost:3978/api/notify to proactively message everyone who has previously messaged this bot.";

        // Dependency injected dictionary for storing ConversationReference objects used in NotifyController to proactively message users
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        public ProactiveBot(ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _conversationReferences = conversationReferences;
        }

        private void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        }

        protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);

            return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(WelcomeMessage), cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);

            var text = turnContext.Activity.Text.Trim().ToLower();

            
            var clientLogin = new RestClient("http://35.185.64.146:80/ingeneo/massive/login");
            clientLogin.Timeout = -1;
            var requestLogin = new RestRequest(Method.POST);
            requestLogin.AddHeader("Content-Type", "application/json");
            requestLogin.AddParameter("application/json", "{\r\n\"username\": \"daniel.espinosa@ingeneo.com.co\",\r\n\"password\": \"daniel.espinosa@ingeneo.com.co\"\r\n}\r\n", ParameterType.RequestBody);
            IRestResponse responseLogin = clientLogin.Execute(requestLogin);

            Login token = JsonConvert.DeserializeObject<Login>(responseLogin.Content);
            Message message = new Message();
            message.id = DateTime.Now.Ticks.ToString();
            message.did = "teamsDemo@ingeneo";
            message.msisdn = turnContext.Activity.Conversation.Id;
            message.name = turnContext.Activity.From.Name;
            message.type = "text";
            message.channel = "WEBCHAT";
            message.content = text;
            message.isAttachment = false;

            var clientMessage = new RestClient("http://35.185.64.146:80/ingeneo/massive/inbound");
            clientMessage.Timeout = -1;
            var requestMessage = new RestRequest(Method.POST);
            requestMessage.AddHeader("Authorization", "Bearer " + token.access_token);
            requestMessage.AddHeader("Content-Type", "application/json");
            requestMessage.AddParameter("application/json", JsonConvert.SerializeObject(message), ParameterType.RequestBody);
            IRestResponse responseMessage = clientMessage.Execute(requestMessage);

            // Echo back what the user said
            //await turnContext.SendActivityAsync(MessageFactory.Text($"Estamos en proceso de construcción," +
            //  $" muy pronto te podras comunicar con bots y agentes de CHATTIGO, Escribiste: {text}."),
            //  cancellationToken);
        }
    }
    public class Login
    {
        public string access_token { get; set; }
    }

    public class Attachment
    {
        public string mediaUrl { get; set; }
        public string mimeType { get; set; }
    }

    public class Message
    {
        public string id { get; set; }
        public string did { get; set; }
        public string msisdn { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public string channel { get; set; }
        public string content { get; set; }
        public bool isAttachment { get; set; }
        public Attachment attachment { get; set; }
    }
}
