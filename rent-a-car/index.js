const express = require('express');
const cors = require('cors');
const { DefaultAzureCredential } = require('@azure/identity');
const { ServiceBusClient } = require('@azure/service-bus');
require('dotenv').config();
const app = express();
app.use(cors());
app.use(express.json());

app.post('/api/locacao', async (req, res) => {
  const { nome, email, modelo, ano, tempoAluguel } = req.body;
  const connectionString = "Endpoint=sb://sb-dev-eastus2-cereleo.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=nJSZ/i/RkN4PI0p7zKkGKWfDwPN2jRhx++ASbHzul6Y=";
 
  const mensagem = {
    nome,
    email,
    modelo,
    ano,
    tempoAluguel,
    data: new Date().toISOString(),
  };

  try {
    const credential = new DefaultAzureCredential();
    const serviceBusConnection = connectionString;
    const queueName = 'fila-locacao-auto';
    const sbClient = new ServiceBusClient(serviceBusConnection);
    const sender = sbClient.createSender(queueName);
    const message = {
      body: mensagem,
      contentType: 'application/json',
      label: 'locacao',
    };
    await sender.sendMessages(message);
    await sender.close();
    await sbClient.close();
    res.status(201).json({ message: 'Locação de veículo enviada para fila com sucesso!' });

  } catch (error) {
    console.error('Erro ao enviar mensagem:', error);
    res.status(500).json({ error: 'Erro ao enviar mensagem' });
    }

});

app.listen(3001, () => {
  console.log('Servidor rodando na porta 3001');
});
