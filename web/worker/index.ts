/**
 * OpenPC web — Cloudflare Worker (cenário 3).
 *
 * Serve o SPA Angular (dist/web/browser via Workers Static Assets) e faz
 * proxy de /api/* e /images/* (fotos do MinIO servidas pelo Caddy da VPS)
 * para a origem backend (env.API_ORIGIN). O front usa caminhos relativos,
 * então o Worker decide o destino:
 *   - /api/*, /images/*  -> API_ORIGIN (mesma path e query)
 *   - demais            -> assets estáticos (fallback SPA via not_found_handling)
 *
 * Config: web/wrangler.jsonc — `run_worker_first: ["/api/*", "/images/*"]`
 * garante que essas rotas passam pelo Worker; o resto é servido dos assets.
 */

export interface Env {
  /** Origem da API, SEM /api — ex: https://openpc.example.com ou http://localhost:5080 */
  API_ORIGIN: string;
  ASSETS: {
    fetch(request: Request | string): Promise<Response>;
  };
}

export default {
  async fetch(request: Request, env: Env): Promise<Response> {
    const url = new URL(request.url);
    if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/images/')) {
      return proxyOrigin(request, env);
    }
    return env.ASSETS.fetch(request);
  },
};

async function proxyOrigin(request: Request, env: Env): Promise<Response> {
  const url = new URL(request.url);
  const target = new URL(env.API_ORIGIN);
  target.pathname = url.pathname;
  target.search = url.search;

  const headers = new Headers(request.headers);
  headers.delete('host');
  headers.set('x-forwarded-host', url.host);
  headers.set('x-forwarded-proto', url.protocol.slice(0, -1));

  const init: RequestInit = {
    method: request.method,
    headers,
    redirect: 'manual',
  };
  if (request.method !== 'GET' && request.method !== 'HEAD' && request.body) {
    init.body = request.body;
  }
  return fetch(new Request(target, init));
}
