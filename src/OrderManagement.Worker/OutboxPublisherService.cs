using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderManagement.Infrastructure.Messaging;
using OrderManagement.Infrastructure.Persistence;
using RabbitMQ.Client;
namespace OrderManagement.Worker;
public sealed class OutboxPublisherService(IServiceScopeFactory scopeFactory,IConnection connection,ILogger<OutboxPublisherService> logger):BackgroundService
{
    private const string Exchange="orders.events",RoutingKey="order.processing.requested",Queue="orders.processing";
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var channel=connection.CreateModel();
        channel.ExchangeDeclare(Exchange,ExchangeType.Direct,durable:true,autoDelete:false);
        channel.QueueDeclare(Queue,durable:true,exclusive:false,autoDelete:false,arguments:null);
        channel.QueueBind(Queue,Exchange,RoutingKey);
        channel.ConfirmSelect();
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var claimed=await ClaimBatchAsync(stoppingToken);
                if(claimed.Count==0){await Task.Delay(TimeSpan.FromSeconds(2),stoppingToken);continue;}
                foreach(var message in claimed)
                {
                    try
                    {
                        var properties=channel.CreateBasicProperties(); properties.Persistent=true;
                        properties.ContentType="application/json"; properties.MessageId=message.Id.ToString("D");
                        properties.Type=message.EventType; properties.Timestamp=new AmqpTimestamp(message.OccurredAtUtc.ToUnixTimeSeconds());
                        channel.BasicPublish(Exchange,message.EventType,mandatory:true,basicProperties:properties,body:System.Text.Encoding.UTF8.GetBytes(message.Payload));
                        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(10));
                        await CompleteAsync(message.Id,true,null,stoppingToken);
                    }
                    catch(Exception exception) when(!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogError(exception,"Failed to publish outbox message {OutboxMessageId}",message.Id);
                        await CompleteAsync(message.Id,false,exception,stoppingToken);
                    }
                }
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception exception){logger.LogError(exception,"Outbox publishing cycle failed");await Task.Delay(TimeSpan.FromSeconds(5),stoppingToken);}
        }
    }
    private async Task<List<OutboxMessage>> ClaimBatchAsync(CancellationToken token)
    {
        await using var scope=scopeFactory.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await using var transaction=await db.Database.BeginTransactionAsync(token);
        var now=DateTimeOffset.UtcNow;
        var messages=await db.OutboxMessages.FromSqlInterpolated($"""
            SELECT TOP (20) * FROM [OutboxMessages] WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE [PublishedAtUtc] IS NULL AND [DeadLetteredAtUtc] IS NULL
              AND [NextAttemptAtUtc] <= {now}
              AND ([LeaseExpiresAtUtc] IS NULL OR [LeaseExpiresAtUtc] < {now})
            ORDER BY [OccurredAtUtc], [Id]
            """).ToListAsync(token);
        var lease=Guid.NewGuid();
        foreach(var message in messages)message.AcquireLease(lease,now.AddMinutes(1));
        await db.SaveChangesAsync(token);
        await transaction.CommitAsync(token);
        return messages;
    }
    private async Task CompleteAsync(Guid id,bool published,Exception? exception,CancellationToken token)
    {
        await using var scope=scopeFactory.CreateAsyncScope();
        var db=scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var message=await db.OutboxMessages.SingleAsync(x=>x.Id==id,token);
        if(message.PublishedAtUtc is not null || message.DeadLetteredAtUtc is not null)return;
        if(published)message.MarkPublished(DateTimeOffset.UtcNow);
        else message.RecordFailure(DateTimeOffset.UtcNow,exception!);
        await db.SaveChangesAsync(token);
    }
}