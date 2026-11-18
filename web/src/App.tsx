import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { api, type Customer, type Order, type OrderDetails, type Page } from "./api";
const countries=["AO","BW","KM","CD","SZ","LS","MG","MW","MU","MZ","NA","SC","ZA","TZ","ZM","ZW"];
const currencies=["AOA","BWP","KMF","CDF","SZL","LSL","MGA","MWK","MUR","MZN","NAD","SCR","ZAR","TZS","ZMW","ZWL","USD"];
export default function App(){
 const [token,setToken]=useState(""),[orders,setOrders]=useState<Order[]>([]),[customers,setCustomers]=useState<Customer[]>([]);
 const [selected,setSelected]=useState<OrderDetails|null>(null),[error,setError]=useState(""),[busy,setBusy]=useState(false),[tab,setTab]=useState<"orders"|"customers">("orders");
 const [customerForm,setCustomerForm]=useState({name:"",email:"",countryCode:"ZA"});
 const [orderForm,setOrderForm]=useState({customerId:"",currencyCode:"ZAR",productSku:"",quantity:"1",unitPrice:"0.00"});
 const load=useCallback(async()=>{
  if(!token.trim())return;setBusy(true);setError("");
  try{const [o,c]=await Promise.all([api<Page<Order>>("/api/orders?page=1&pageSize=50",token),api<Page<Customer>>("/api/customers?page=1&pageSize=100",token)]);setOrders(o.items);setCustomers(c.items);
   if(selected){const latest=await api<OrderDetails>(`/api/orders/${selected.id}`,token);setSelected(latest);}
  }catch(e){setError(e instanceof Error?e.message:"Unable to load data.");}finally{setBusy(false);}
 },[token,selected?.id]);
 useEffect(()=>{void load();},[load]);
 async function openOrder(id:string){try{setSelected(await api<OrderDetails>(`/api/orders/${id}`,token));setError("");}catch(e){setError(e instanceof Error?e.message:"Unable to load order.");}}
 async function submitCustomer(e:FormEvent){e.preventDefault();setBusy(true);setError("");try{await api<Customer>("/api/customers",token,{method:"POST",body:JSON.stringify(customerForm)});setCustomerForm({name:"",email:"",countryCode:"ZA"});await load();}catch(e){setError(e instanceof Error?e.message:"Customer creation failed.");}finally{setBusy(false);}}
 async function submitOrder(e:FormEvent){e.preventDefault();setBusy(true);setError("");try{const o=await api<Order>("/api/orders",token,{method:"POST",body:JSON.stringify({customerId:orderForm.customerId,currencyCode:orderForm.currencyCode,items:[{productSku:orderForm.productSku,quantity:Number(orderForm.quantity),unitPrice:Number(orderForm.unitPrice)}]})});setOrderForm({...orderForm,productSku:"",quantity:"1",unitPrice:"0.00"});await load();await openOrder(o.id);}catch(e){setError(e instanceof Error?e.message:"Order creation failed.");}finally{setBusy(false);}}
 async function transition(action:"submit"|"confirm"|"process"|"fulfill"|"cancel"){if(!selected)return;setBusy(true);setError("");try{const o=await api<OrderDetails>(`/api/orders/${selected.id}/${action}`,token,{method:"POST",headers:{"Idempotency-Key":crypto.randomUUID()}});setSelected(o);await load();}catch(e){setError(e instanceof Error?e.message:"Order update failed.");}finally{setBusy(false);}}
 const status=(s:string)=>`status status-${s.toLowerCase()}`;
 return <main className="shell">
  <header className="topbar"><div><span className="eyebrow">OPERATIONS CONSOLE</span><h1>SADC Order Management</h1><p>Customers, orders and fulfilment in one place.</p></div><span className="live-dot">API workspace</span></header>
  <section className="auth panel"><label htmlFor="token">Microsoft Entra access token</label><input id="token" type="password" autoComplete="off" value={token} onChange={e=>setToken(e.target.value)} placeholder="Paste token with Orders.Read and Orders.Write permissions"/><button onClick={()=>void load()} disabled={!token.trim()||busy}>Connect / refresh</button><small>Token is held in memory only; it is not persisted in browser storage.</small></section>
  {error&&<div className="alert" role="alert">{error}</div>}
  <section className="metrics"><article className="metric panel"><span>Orders loaded</span><strong>{orders.length}</strong></article><article className="metric panel"><span>Customers loaded</span><strong>{customers.length}</strong></article><article className="metric panel"><span>Pending orders</span><strong>{orders.filter(o=>o.status==="Draft"||o.status==="Submitted").length}</strong></article></section>
  <nav className="tabs"><button className={tab==="orders"?"active":""} onClick={()=>setTab("orders")}>Orders</button><button className={tab==="customers"?"active":""} onClick={()=>setTab("customers")}>Customers</button></nav>
  {tab==="orders"?<section className="workspace">
   <div className="panel"><div className="section-heading"><div><span className="eyebrow">ORDER REGISTER</span><h2>Recent orders</h2></div><button className="secondary" onClick={()=>void load()} disabled={busy}>Refresh</button></div>
    <div className="table-wrap"><table><thead><tr><th>Reference</th><th>Created</th><th>Status</th><th>Total</th></tr></thead><tbody>{orders.map(o=><tr key={o.id} className={selected?.id===o.id?"selected":""} onClick={()=>void openOrder(o.id)}><td><button className="text-button">{o.customerReference}</button></td><td>{new Date(o.createdAtUtc).toLocaleDateString()}</td><td><span className={status(o.status)}>{o.status}</span></td><td>{o.totalAmount==null?"—":`${o.currencyCode} ${o.totalAmount.toFixed(2)}`}</td></tr>)}{!orders.length&&<tr><td colSpan={4} className="empty">Connect with an access token to load orders.</td></tr>}</tbody></table></div>
   </div>
   <div className="panel"><span className="eyebrow">CREATE</span><h2>New order</h2><form className="form" onSubmit={submitOrder}>
    <label>Customer<select required value={orderForm.customerId} onChange={e=>setOrderForm({...orderForm,customerId:e.target.value})}><option value="">Choose customer</option>{customers.map(c=><option key={c.id} value={c.id}>{c.name} ({c.countryCode})</option>)}</select></label>
    <label>Currency<select value={orderForm.currencyCode} onChange={e=>setOrderForm({...orderForm,currencyCode:e.target.value})}>{currencies.map(c=><option key={c}>{c}</option>)}</select></label>
    <label>Product SKU<input required maxLength={64} value={orderForm.productSku} onChange={e=>setOrderForm({...orderForm,productSku:e.target.value})}/></label>
    <div className="two"><label>Quantity<input type="number" min="1" required value={orderForm.quantity} onChange={e=>setOrderForm({...orderForm,quantity:e.target.value})}/></label><label>Unit price<input type="number" min="0" step="0.01" required value={orderForm.unitPrice} onChange={e=>setOrderForm({...orderForm,unitPrice:e.target.value})}/></label></div>
    <button disabled={busy||!token.trim()||!customers.length}>Create order</button></form></div>
   {selected&&<div className="panel detail"><div className="section-heading"><div><span className="eyebrow">ORDER DETAIL</span><h2>{selected.customerReference}</h2></div><span className={status(selected.status)}>{selected.status}</span></div><p className="muted">Created {new Date(selected.createdAtUtc).toLocaleString()}</p>
    <div className="table-wrap"><table><thead><tr><th>SKU</th><th>Qty</th><th>Unit price</th><th>Line total</th></tr></thead><tbody>{selected.lineItems?.map((i,n)=><tr key={n}><td>{i.productSku}</td><td>{i.quantity}</td><td>{i.unitPrice.toFixed(2)}</td><td>{i.lineTotal.toFixed(2)}</td></tr>)}</tbody></table></div>
    <div className="actions">{(["submit","confirm","process","fulfill","cancel"] as const).map(a=><button key={a} className={a==="cancel"?"danger":"secondary"} disabled={busy} onClick={()=>void transition(a)}>{a[0].toUpperCase()+a.slice(1)}</button>)}</div>
   </div>}
  </section>:<section className="workspace customers">
   <div className="panel"><div className="section-heading"><div><span className="eyebrow">CUSTOMER DIRECTORY</span><h2>Customers</h2></div><button className="secondary" onClick={()=>void load()} disabled={busy}>Refresh</button></div>
    <div className="table-wrap"><table><thead><tr><th>Name</th><th>Email</th><th>Country</th></tr></thead><tbody>{customers.map(c=><tr key={c.id}><td>{c.name}</td><td>{c.email}</td><td>{c.countryCode}</td></tr>)}{!customers.length&&<tr><td colSpan={3} className="empty">No customers loaded.</td></tr>}</tbody></table></div>
   </div>
   <div className="panel"><span className="eyebrow">CUSTOMER ONBOARDING</span><h2>New customer</h2><form className="form" onSubmit={submitCustomer}><label>Full name<input required maxLength={200} value={customerForm.name} onChange={e=>setCustomerForm({...customerForm,name:e.target.value})}/></label><label>Email<input type="email" required maxLength={254} value={customerForm.email} onChange={e=>setCustomerForm({...customerForm,email:e.target.value})}/></label><label>Country<select value={customerForm.countryCode} onChange={e=>setCustomerForm({...customerForm,countryCode:e.target.value})}>{countries.map(c=><option key={c}>{c}</option>)}</select></label><button disabled={busy||!token.trim()}>Create customer</button></form></div>
  </section>}
  <footer>Order Management · Server-calculated totals · Idempotent lifecycle actions</footer>
 </main>;
}
