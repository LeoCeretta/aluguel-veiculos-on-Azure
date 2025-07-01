using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using fnPayment.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fnPayment
{
    public class Payment
    {
        private readonly ILogger<Payment> _logger;
        private readonly IConfiguration _configuration;
        private readonly string[] StatusList = { "Aprovado", "Reprovado", "Em análise" };
        private readonly Random random = new Random();

        public Payment(ILogger<Payment> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        [Function(nameof(Payment))]
        [CosmosDBOutput("%CosmosDb%", "%CosmosContainer%", Connection = "CosmosDBConnection", CreateIfNotExists = true)]
        public async Task<object> Run(
            [ServiceBusTrigger("payment-queue", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {

            _logger.LogInformation("Message ID: {id}", message.MessageId);
            _logger.LogInformation("Message Body: {body}", message.Body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            PaymentModel payment = null;
            try
            {
                // Deserialize the message body to PaymentModel
                payment = JsonSerializer.Deserialize<PaymentModel>(message.Body.ToString(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });
                if (payment == null)
                {
                    await messageActions.DeadLetterMessageAsync(message, null, "The message could not be deserialized.");
                    return null;
                }
                int index = random.Next(StatusList.Length);
                string status = StatusList[index];
                payment.Status = status;

                if (status == "Aprovado")
                {
                    payment.DataAprovacao = DateTime.UtcNow;
                    await SentToNotificationQueue(payment);
                }

                await messageActions.CompleteMessageAsync(message);
                return payment;
            }
            catch (Exception ex)
            {
                await messageActions.DeadLetterMessageAsync(message, null, $"Erro: {ex.Message}");
                return null;
            }
        }

        private async Task SentToNotificationQueue(PaymentModel payment)
        {
            var connectionString = _configuration.GetSection("ServiceBusConnection").Value.ToString();
            var queueName = _configuration.GetSection("NotificationQueue").Value.ToString();

            var serviceBusClient = new ServiceBusClient(connectionString);
            var sender = serviceBusClient.CreateSender(queueName);
            var message = new ServiceBusMessage(JsonSerializer.Serialize(payment))
            {
                ContentType = "application/json",
            };

            message.ApplicationProperties["IdPayment"] = payment.IdPayment;
            message.ApplicationProperties["type"] = "notification";
            message.ApplicationProperties["message"] = "Pagamento aprovado com sucesso.";

            try {                 
                await sender.SendMessageAsync(message);
                _logger.LogInformation("Message sent to notification queue: {id}", payment.IdPayment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to notification queue");
            }
            finally
            {
                await sender.DisposeAsync();
                await serviceBusClient.DisposeAsync();
            }
        }
    }
}