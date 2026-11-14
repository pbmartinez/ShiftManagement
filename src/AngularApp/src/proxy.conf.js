// Dev only: the browser talks to the Angular dev server, which forwards /api to the Api.
// One origin for the browser, so no CORS is needed. In production the Api serves the built app itself.
const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7005';

module.exports = [
  {
    context: ['/api'],
    target,
    secure: false
  }
];
