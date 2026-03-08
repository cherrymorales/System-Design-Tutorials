# Client-Server SPA + API

## What It Is

This architecture separates the user interface from the backend by using a single-page application frontend and an API backend.

The client handles presentation and user interaction, while the server exposes business capabilities through HTTP APIs.

## Best Used When

- building modern web applications with rich user experiences
- frontend and backend concerns should evolve somewhat independently
- the product needs web-first delivery with clear API contracts

## Not Ideal When

- the application is mostly static content
- real-time event-heavy workflows dominate the entire design
- SEO-heavy public marketing pages are the main requirement without an app-like experience

## Why It Is Common

This is one of the most common industry patterns for business applications because it supports modern frontend development without requiring a fully distributed backend.

## Recommended Technology

- Frontend: React
- Backend: ASP.NET Core Web API
- Database: PostgreSQL
- Hosting: single container for backend plus static frontend files when practical, or separate frontend/backend containers when needed

## Single-Container Guidance

This can often stay in a single container if the React app is built into static assets and served by the ASP.NET Core application.

## Example Project

**Project idea:** Project management and team collaboration web app

Why it fits:

- interactive dashboards
- forms and workflows
- role-based access
- API-friendly domain

## Suggested Solution Shape

- React frontend with routing and component-based UI
- ASP.NET Core REST API
- JWT or cookie-based authentication
- PostgreSQL for transactional data

## Tradeoffs

- clear separation between UI and server logic
- good developer experience for frontend teams
- easy to support future mobile clients through the same API
- requires careful API design to avoid chatty client-server communication
