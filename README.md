# Sistema de Aluguel de Carros - Lab07

Este projeto demonstra uma arquitetura moderna baseada em serviços na nuvem Azure para um sistema de aluguel de carros, integrando API, Azure Functions, Service Bus, Cosmos DB, front-end e Docker.

## Visão Geral da Arquitetura

- **API de Locação (rent-a-cart):** Responsável por receber as solicitações de aluguel de carros dos usuários via front-end ou requisições HTTP.
- **Azure Service Bus:** Utilizado como barramento de mensagens para desacoplar os serviços e garantir resiliência e escalabilidade no processamento das locações.
- **Azure Functions:** Funções serverless que processam as mensagens da fila, atualizam o status da locação, interagem com o banco de dados NoSQL (Cosmos DB) e disparam notificações.
- **Cosmos DB:** Banco de dados NoSQL utilizado para armazenar as informações das locações de forma escalável e flexível.
- **Notificação por E-mail:** Ao final do processamento, um e-mail é enviado ao locatário confirmando a locação.
- **Front-end (rent-a-cart):** Interface web para interação do usuário, empacotada via Docker para facilitar o deploy.

## Fluxo do Processo

1. **Solicitação de Locação:**  
   O usuário realiza uma solicitação de aluguel de carro via API (ou front-end).

2. **Envio para o Service Bus:**  
   A API publica uma mensagem na fila do Azure Service Bus (`fila-locacao-auto`), contendo os dados da locação.

3. **Processamento pela Azure Function:**  
   A Azure Function (`fnSBRentProcess`) é disparada automaticamente ao receber uma nova mensagem na fila. Ela:
   - Deserializa a mensagem.
   - Armazena os dados da locação em um banco de dados relacional (SQL) e/ou envia para a próxima fila de processamento.
   - Encaminha a mensagem para a fila de pagamento.

4. **Processamento de Pagamento:**  
   Outra Azure Function (`fnPayment`) consome a fila de pagamento, processa o pagamento, atualiza o status da locação e armazena/atualiza os dados no Cosmos DB.

5. **Notificação:**  
   Uma função adicional pode consumir a fila de notificações e enviar um e-mail ao locatário, informando sobre o status da locação.

## Destaques Técnicos

- **Arquitetura Orientada a Serviços:**  
  Utiliza componentes desacoplados, facilitando manutenção, escalabilidade e resiliência.

- **Azure Service Bus:**  
  Garante entrega confiável das mensagens entre API, funções e serviços de notificação.

- **Azure Functions:**  
  Permite processamento serverless, com escalabilidade automática e baixo custo operacional.

- **Cosmos DB:**  
  Banco de dados NoSQL globalmente distribuído, ideal para aplicações modernas e escaláveis.

- **Notificações Automatizadas:**  
  O fluxo permite que o locatário seja informado automaticamente por e-mail ao final do processo.

- **Front-end em Docker:**  
  O front-end pode ser facilmente implantado em qualquer ambiente compatível com Docker, facilitando testes e deploys.

## Estrutura de Pastas

```
Lab07/
│
├── functions/
│   ├── fnSBRentProcess/      # Azure Function para processamento inicial da locação
│   └── fnPayment/            # Azure Function para processamento de pagamento e integração com Cosmos DB
│
├── rent-a-cart/              # Front-end do sistema
│   ├── Dockerfile
│   ├── index.js
│   └── (demais arquivos do front-end)
│
└── (demais componentes do Lab07)
```

## Como Executar

1. **Configurar recursos Azure:**  
   - Service Bus (com as filas necessárias)
   - Cosmos DB
   - Azure Functions (deploy dos projetos fnSBRentProcess e fnPayment)
   - Serviço de envio de e-mails (ex: SendGrid)

2. **Executar o front-end:**  
   ```sh
   cd rent-a-cart
   docker build -t rent-a-cart .
   docker run -p 3001:3001 rent-a-cart
   ```

3. **Testar o fluxo:**  
   - Realize uma locação via front-end ou API.
   - Acompanhe o processamento nas filas do Service Bus e no Cosmos DB.
   - Verifique o recebimento do e-mail de confirmação.

---

**Observação:**  
Este projeto é um exemplo didático, mas segue boas práticas de arquitetura cloud-native, podendo ser expandido para cenários reais de produção.