# Infrastructure Notes

This tutorial uses:

- PostgreSQL for asset records, projections, notifications, and outbox state
- RabbitMQ for asynchronous event fan-out between the API and worker consumers

Local container ports:

- API: `8085`
- PostgreSQL: `5437`
- RabbitMQ AMQP: `5674`
- RabbitMQ management UI: `15674`

The frontend runs separately through Vite during development and proxies API traffic to the ASP.NET Core API.
