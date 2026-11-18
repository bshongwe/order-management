using OrderManagement.Application.Customers;
using OrderManagement.Application.Orders;
using OrderManagement.Domain.Customers;
using OrderManagement.Domain.Orders;
using Xunit;
namespace OrderManagement.UnitTests.Orders;
public sealed class CreateOrderHandlerTests
{
 [Fact] public async Task HandleAsync_WithValidCustomerAndItems_PersistsServerCalculatedTotal()
 {
  var customer=Customer.Create("Example","example@example.com","ZA"); var repository=new InMemoryOrderRepository();
  var handler=new CreateOrderHandler(repository,new StubCustomerRepository(customer));
  var result=await handler.HandleAsync(new CreateOrderCommand(customer.Id,"zar",[new CreateOrderLineItemCommand("SKU-1",2,12.50m)]));
  var order=Assert.Single(repository.Orders); Assert.Equal(order.Id,result.Id); Assert.Equal(customer.Id,result.CustomerId);
  Assert.Equal("ZAR",result.CurrencyCode); Assert.Equal(25m,result.TotalAmount); Assert.Equal(25m,order.TotalAmount);
 }
 [Fact] public async Task HandleAsync_WhenCustomerDoesNotExist_ThrowsAndDoesNotPersist()
 {
  var repository=new InMemoryOrderRepository(); var handler=new CreateOrderHandler(repository,new StubCustomerRepository(null));
  await Assert.ThrowsAsync<CustomerNotFoundException>(()=>handler.HandleAsync(new CreateOrderCommand(Guid.NewGuid(),"ZAR",[new CreateOrderLineItemCommand("SKU",1,1m)])));
  Assert.Empty(repository.Orders);
 }
 [Fact] public async Task HandleAsync_WhenCurrencyDoesNotMatchCountry_ThrowsAndDoesNotPersist()
 {
  var customer=Customer.Create("Example","example@example.com","ZA"); var repository=new InMemoryOrderRepository();
  var handler=new CreateOrderHandler(repository,new StubCustomerRepository(customer));
  await Assert.ThrowsAsync<UnsupportedOrderCurrencyException>(()=>handler.HandleAsync(new CreateOrderCommand(customer.Id,"USD",[new CreateOrderLineItemCommand("SKU",1,1m)])));
  Assert.Empty(repository.Orders);
 }
 private sealed class StubCustomerRepository(Customer? customer):ICustomerRepository
 {
  public Task AddAsync(Customer value,CancellationToken token)=>throw new NotSupportedException();
  public Task<CustomerDetails?> GetByIdAsync(Guid id,CancellationToken token)=>Task.FromResult(customer is not null&&customer.Id==id?new CustomerDetails(customer.Id,customer.Name,customer.Email,customer.CountryCode,customer.CreatedAtUtc):null);
  public Task<CustomerPage> GetPageAsync(string? search,int page,int pageSize,CancellationToken token)=>throw new NotSupportedException();
 }
 private sealed class InMemoryOrderRepository:IOrderRepository
 {
  public List<Order> Orders {get;}=[];
  public Task AddAsync(Order order,CancellationToken token){Orders.Add(order);return Task.CompletedTask;}
  public Task<Order?> GetByIdForUpdateAsync(Guid id,CancellationToken token)=>Task.FromResult(Orders.SingleOrDefault(x=>x.Id==id));
  public Task SaveChangesAsync(CancellationToken token)=>Task.CompletedTask;
  public Task<OrderDetails?> GetByIdAsync(Guid id,CancellationToken token)=>Task.FromResult<OrderDetails?>(null);
  public Task<OrderPage> GetPageAsync(int page,int pageSize,CancellationToken token)=>Task.FromResult(new OrderPage([],page,pageSize,0,0));
 }
}