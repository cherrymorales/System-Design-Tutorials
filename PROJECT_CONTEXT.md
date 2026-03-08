# System Design Tutorials - Project Context

## Purpose

This repository is a practical learning library for modern system design patterns used in real software projects.

The goal is to document common architectural approaches in a way that is easy to understand, easy to compare, and easy to turn into working example applications.

## What We Want To Achieve

- Build a collection of system design tutorials that are modern, practical, and adaptable to different business domains.
- Explain each system design clearly: what it is, when to use it, when not to use it, and what tradeoffs it brings.
- Provide an example project idea for each system design so the design can be understood in a realistic context.
- Prefer React for frontend and .NET Core for backend by default.
- Recommend different technologies when they are a better fit for a specific system design.
- Prefer deployment in a single container whenever that keeps the architecture simple and realistic.
- Allow multiple containers only when the architecture genuinely benefits from separation.

## Project Principles

1. Keep the designs modern.
2. Keep the guidance practical rather than academic.
3. Optimize for adaptability across industries and product types.
4. Document tradeoffs honestly.
5. Keep the folder structure consistent so new tutorials are easy to add.

## Initial Scope

The project starts with five widely used architectural styles:

1. Layered Monolith
2. Modular Monolith
3. Client-Server SPA + API
4. Microservices
5. Event-Driven Architecture

## Default Technology Direction

- Frontend default: React
- Backend default: ASP.NET Core
- Container preference: single container when practical
- Database suggestions: PostgreSQL or SQL Server for transactional systems, depending on the design
- Messaging suggestions: RabbitMQ, Azure Service Bus, or Kafka when eventing is required

Technology choices should stay flexible. Each tutorial may recommend a better stack when the system design calls for it.

## Repository Structure

Each system design tutorial should live in its own folder under `tutorials/`.

Recommended structure:

```text
tutorials/
  system-design-name/
    README.md
```

Each tutorial document should cover:

- What the design is
- Core characteristics
- When it is best used
- When it is not the best fit
- Strengths and tradeoffs
- Recommended technology stack
- Deployment approach
- Example project idea

## How This Context Document Should Be Used

This file is the shared reference point for future work in this repository.

When adding a new tutorial or example implementation, use this document to keep the work aligned with:

- the learning purpose of the repository
- the preferred technology defaults
- the modern and adaptable design philosophy
- the single-container-first deployment preference

## Success Criteria

This project is successful if a developer can open the repository, pick a tutorial, and quickly understand:

- what the architecture is
- when it makes sense
- what stack is suitable
- how it might be deployed
- what kind of product it is good for
