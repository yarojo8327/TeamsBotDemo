using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ProactiveBot.Controllers
{
    [Route("api/webhook/message")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;

        public Message _agentMessage { get; set; }

        public WebhookController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ConcurrentDictionary<string, ConversationReference> conversationReferences)
        {
            _adapter = adapter;
            _conversationReferences = conversationReferences;
            _appId = configuration["MicrosoftAppId"];

            // If the channel is the Emulator, and authentication is not in use,
            // the AppId will be null.  We generate a random AppId for this case only.
            // This is not required for production, since the AppId will have a value.
            if (string.IsNullOrEmpty(_appId))
            {
                _appId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post(Message agentMessage)
        {
            _agentMessage = agentMessage;
            foreach (var conversationReference in _conversationReferences.Values)
            {
                if(conversationReference.Conversation.Id == agentMessage.msisdn)
                    await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, BotCallback, default(CancellationToken));
            }

            // Let the caller know proactive messages have been sent
            return new ContentResult()
            {
                Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }

        private async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // If you encounter permission-related errors when sending this message, see
            // https://aka.ms/BotTrustServiceUrl
            await turnContext.SendActivityAsync(_agentMessage.content);
        }
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
