# 004 Microservices Infrastructure Notes

This folder contains local infrastructure assets for the runnable tutorial.

Current contents:

- `postgres-init/001-create-databases.sql`

Purpose:

- create the service-owned PostgreSQL databases used by the composed runtime

The databases are created for:

- `micro_identity`
- `micro_catalog`
- `micro_orders`
- `micro_inventory`
- `micro_payments`
- `micro_fulfillment`
- `micro_notifications`
- `micro_operations_query`

These assets are intended for local tutorial startup only. They are not a production infrastructure definition.
