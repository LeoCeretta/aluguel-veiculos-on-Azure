using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace fnSBRentProcess
{
    public class ProcessoLocacao
    {
        private readonly ILogger<ProcessoLocacao> _logger;
        private readonly IConfiguration _configuration;

        public ProcessoLocacao(ILogger<ProcessoLocacao> logger, IConfiguration configuration)
        {
            _logger = logger;
            this._configuration = configuration;
        }

        [Function(nameof(ProcessoLocacao))]
        public async Task Run(
            [ServiceBusTrigger("fila-locacao-auto", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Message ID: {id}", message.MessageId);
            var body = message.Body.ToString();
            _logger.LogInformation("Message Body: {body}", body);
            _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);


            RentModel rentModel = null;
            try
            {
                // Deserialize the message body to RentModel
                rentModel = JsonSerializer.Deserialize<RentModel>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

                if (rentModel is null)
                {
                    _logger.LogError("Mensagem mal formatada.");
                    await messageActions.DeadLetterMessageAsync(message, null, "Mensagem mal formatada.");
                    return;
                }

                var connectionString = _configuration.GetConnectionString("SqlConnectionString");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var command = new SqlCommand(@"INSERT INTO Locacao (Nome, Email, Modelo, Ano, TempoAluguel, Data) VALUES (@Nome, @Email, @Modelo, @Ano, @TempoAluguel, @Data)", connection);
                    command.Parameters.AddWithValue("@Nome", rentModel.nome);
                    command.Parameters.AddWithValue("@Email", rentModel.email);
                    command.Parameters.AddWithValue("@Modelo", rentModel.modelo);
                    command.Parameters.AddWithValue("@Ano", rentModel.ano);
                    command.Parameters.AddWithValue("@TempoAluguel", rentModel.tempoAluguel);
                    command.Parameters.AddWithValue("@Data", rentModel.data);

                    var serviceBusConnection = _configuration["ServiceBusConnection"];
                    var serviceBusQueue = _configuration["ServiceBusQueue"];
                    sendMessageToPay(serviceBusConnection, serviceBusQueue, rentModel);

                    var rowsAffected = await command.ExecuteNonQueryAsync();

                    // Complete the message
                    await messageActions.CompleteMessageAsync(message);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ao processar a mensagem: {messageId}", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, null, $"Erro ao processar a mensagem: {ex.Message} ");
                return;

            }
        }

        private void sendMessageToPay(string serviceBusConnection, string serviceBusQueue, RentModel rentModel)
        {
            var serviceBusClient = new ServiceBusClient(serviceBusConnection);
            var serviceBusSender = serviceBusClient.CreateSender(serviceBusQueue);

            var message = new ServiceBusMessage(JsonSerializer.Serialize(rentModel))
            {
                ContentType = "application/json"
            };

            message.ApplicationProperties.Add("Tipo", "Pagamento");
            message.ApplicationProperties.Add("Nome", rentModel.nome);
            message.ApplicationProperties.Add("Email", rentModel.email);
            message.ApplicationProperties.Add("Modelo", rentModel.modelo);
            message.ApplicationProperties.Add("Ano", rentModel.ano);
            message.ApplicationProperties.Add("TempoAluguel", rentModel.tempoAluguel);
            message.ApplicationProperties.Add("Data", rentModel.data.ToString("o")); // ISO 8601

            serviceBusSender.SendMessageAsync(message).Wait();
            serviceBusSender.DisposeAsync();
        }
    }
}