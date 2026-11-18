using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Orders;
using OrderManagement.Infrastructure.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
namespace OrderManagement.Worker;
public sealed class OrderProcessingConsumer(IServiceScopeFactory scopeFactory,IConnection connection,ILogger<OrderProcessingConsumer> logger):BackgroundService
{
    private const string Exchange="orders.events",RoutingKey="order.processing.requested",Queue="orders.processing",DeadQueue="orders.processing.dead",RetryQueue="orders.processing.retry";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel=connection.CreateModel();
        channel.ExchangeDeclare(Exchange,ExchangeType.Direct,durable:true,autoDelete:false);
        channel.QueueDeclare(Queue,durable:true,exclusive:false,autoDelete:false,arguments:null);
        channel.QueueBind(Queue,Exchange,RoutingKey);
        channel.QueueDeclare(DeadQueue,durable:true,exclusive:false,autoDelete:false,arguments:null);
        channel.QueueDeclare(RetryQueue,durable:true,exclusive:false,autoDelete:false,arguments:new Dictionary<string,object>
        {
            ["x-message-ttl"]=30000,
            ["x-dead-letter-exchange"]=Exchange,
            ["x-dead-letter-routing-key"]=RoutingKey
        });
        channel.BasicQos(0,1,false);
        var consumer=new EventingBasicConsumer(channel);
        consumer.Received+=(sender,args)=>HandleDelivery(channel,args,stoppingToken);
        channel.BasicConsume(Queue,autoAck:false,consumer);
        try{await Task.Delay(Timeout.Infinite,stoppingToken);}
        catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){}
    }
    private async void HandleDelivery(IModel channel,BasicDeliverEventArgs args,CancellationToken token)
    {
        try
        {
            var message=JsonSerializer.Deserialize<OrderProcessingRequested>(Encoding.UTF8.GetString(args.Body.ToArray()))
                ??throw new InvalidOperationException("Order processing event payload was empty.");
            await using var scope=scopeFactory.CreateAsyncScope();
            var handler=scope.ServiceProvider.GetRequiredService<FulfillOrderHandler>();
            var result=await handler.HandleAsync(message.OrderId,token);
            if(result.Outcome==FulfillOrderOutcome.Fulfilled || result.Outcome==FulfillOrderOutcome.Conflict&&result.Order?.Status==OrderStatus.Fulfilled.ToString())
            {channel.BasicAck(args.DeliveryTag,false);return;}
            throw new InvalidOperationException(result.Outcome==FulfillOrderOutcome.NotFound
                ?$"Order '{message.OrderId}' was not found."
                :$"Order '{message.OrderId}' is not ready for fulfilment (status '{result.Order?.Status}').");
        }
        catch(Exception exception)
        {
            if(token.IsCancellationRequested)
            {
                try{channel.BasicNack(args.DeliveryTag,false,requeue:true);}catch{}
                return;
            }
            logger.LogError(exception,"Order processing event failed");
            try
            {
                var retryCount=0;
                if(args.BasicProperties.Headers is not null&&args.BasicProperties.Headers.TryGetValue("x-retry-count",out var value))
                    retryCount=Convert.ToInt32(value);
                var properties=channel.CreateBasicProperties(); properties.Persistent=true; properties.ContentType="application/json";
                properties.Headers=new Dictionary<string,object>{{"x-retry-count",retryCount+1}};
                if(retryCount>=4)channel.BasicPublish(string.Empty,DeadQueue,false,properties,args.Body);
                else channel.BasicPublish(string.Empty,RetryQueue,false,properties,args.Body);
                channel.BasicAck(args.DeliveryTag,false);
            }
            catch(Exception publishException)
            {
                logger.LogError(publishException,"Could not schedule retry for order processing delivery");
                channel.BasicNack(args.DeliveryTag,false,requeue:true);
            }
        }
    }
}