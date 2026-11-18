const baseUrl = import.meta.env.VITE_API_BASE_URL ?? "";
export type Page<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number };
export type Customer = { id: string; name: string; email: string; countryCode: string; createdAtUtc: string };
export type Order = { id: string; customerReference: string; status: string; createdAtUtc: string; customerId?: string; currencyCode?: string; totalAmount?: number };
export type OrderDetails = Order & { lineItems?: { productSku: string; quantity: number; unitPrice: number; lineTotal: number }[] };
export async function api<T>(path: string, token: string, init: RequestInit = {}): Promise<T> {
 const response = await fetch(`${baseUrl}${path}`, { ...init, headers: { Accept:"application/json", ...(init.body?{"Content-Type":"application/json"}:{}), ...(token?{Authorization:`Bearer ${token}`}:{}), ...init.headers } });
 if (!response.ok) {
  const p = await response.json().catch(()=>null) as {title?:string;detail?:string;errors?:Record<string,string[]>}|null;
  const validation=p?.errors?Object.values(p.errors).flat().join(" "):"";
  throw new Error(validation||p?.detail||p?.title||`Request failed (${response.status})`);
 }
 if(response.status===204)return undefined as T;
 return response.json() as Promise<T>;
}
