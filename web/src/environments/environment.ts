export const environment = {
  production: true,
  // In production this is served behind the same host/nginx as the app itself
  // (see nginx.conf, which proxies /api to the api container) — relative, not absolute.
  apiBaseUrl: '/api',
};
