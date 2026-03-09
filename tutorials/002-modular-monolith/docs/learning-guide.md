# Modular Monolith Learning Guide

## Purpose Of This Guide

This document explains what a learner should take away from the modular monolith tutorial.

The goal is to understand not only the example wholesale platform, but also why stronger internal module boundaries can be the right next step before microservices.

## Learning Objectives

After studying this tutorial, the learner should be able to:

- define a modular monolith in practical terms
- explain how it differs from both a layered monolith and microservices
- identify what makes a business capability a real module boundary
- explain data ownership inside one shared database
- describe how cross-module workflows should be coordinated without distributed infrastructure
- explain how a modular monolith should be tested at multiple levels rather than only with unit tests
- recognize when this architecture should stay a monolith and when it may evolve later

## What To Focus On

### 1. One Deployable Unit Can Still Have Strong Ownership

The key lesson is that deployment and ownership are not the same thing.

A system can still be one application while having strong internal ownership boundaries.

### 2. Modules Are About Business Capability, Not Folder Decoration

A real module is not just a folder name.

A module should have:

- owned data
- owned business rules
- clear responsibilities
- explicit integration boundaries

If a module cannot explain what it owns, it is probably not a real module yet.

### 3. Shared Database Does Not Mean Shared Ownership

One of the most important lessons in a modular monolith is that one relational database can still support strong ownership.

The rule is not “one module per database” by default.

The real rule is:

- one module owns its writes
- other modules do not mutate that module’s data directly

### 4. Internal Contracts Matter

This architecture is valuable because it teaches how modules collaborate without becoming services.

Modules should interact through explicit in-process contracts, orchestration, or approved read models, not through hidden persistence shortcuts.

### 5. Do Not Jump To Microservices Too Early

This tutorial is designed to show that many systems need stronger modularity before they need distributed deployment.

If teams skip this step, they often export poor boundaries into a more expensive architecture.

### 6. Tests Are Part Of The Architecture

This tutorial treats testing as part of design, not just implementation verification.

A modular monolith should prove:

- domain rules inside each module
- cross-module contracts
- host-level API behavior
- role-based access behavior
- the main user workflows from the frontend and end-to-end path

## Common Misunderstandings

### A Modular Monolith Is Just A Layered Monolith With More Folders

Incorrect. A modular monolith introduces business ownership boundaries in addition to technical layering.

### Every Module Should Be A Future Microservice

Incorrect. A module should be independently understandable and owned. It does not automatically need to become a service.

### One Database Means There Are No Real Boundaries

Incorrect. Boundaries come from data ownership and write rules, not only from infrastructure isolation.

### Reporting Should Be Able To Do Anything Because It Reads Everything

Incorrect. Reporting can aggregate across modules, but it should not become a backdoor for operational writes.

## What This Tutorial Teaches Better Than A Toy Example

The wholesale operations scenario exposes real modular concerns:

- customers, orders, inventory, and billing are related but not the same responsibility
- order workflows cross module boundaries
- billing must stay distinct from orders even though they are connected
- reporting needs cross-module visibility without owning operational behavior

This makes modular design choices concrete rather than theoretical.

## Comparison Lens

### Modular Monolith Vs Layered Monolith

- layered monolith emphasizes technical layers
- modular monolith adds strong business-module ownership on top of technical layers
- modular monolith is often the next step when the system becomes broader and more complex

### Modular Monolith Vs Microservices

- modular monolith keeps one deployment unit and in-process communication
- microservices add independent deployment, network boundaries, and operational overhead
- a modular monolith is often the better preparation step before any service split

### Modular Monolith Vs Event-Driven Architecture

- modular monolith handles V1 collaboration in-process
- event-driven architecture becomes more valuable when asynchronous reaction between systems becomes a real requirement
- this tutorial does not need that complexity for the initial design

## Review Questions

- Why is this wholesale platform a better fit for a modular monolith than for microservices right now?
- What makes `Orders` and `Billing` separate modules instead of one large transactional module?
- Why can one database still support module ownership?
- What kinds of cross-module interaction are acceptable in V1?
- What warning signs would justify evolving this design later?
- What test level should prove `Orders -> Inventory -> Billing` behavior?

## Practical Exercise

A useful learning exercise is to map a cross-module workflow without turning it into a service design.

Suggested workflow:

1. a sales coordinator submits an order
2. `Orders` validates the request
3. `Orders` requests a reservation from `Inventory`
4. `Inventory` either accepts or rejects the reservation
5. `Orders` updates its state based on the response
6. `Billing` later creates the invoice through an explicit contract
7. `Reporting` reads the result without owning any of the writes

If the learner can explain who owns each decision and each data write, they understand the modular monolith idea well.

The learner should also be able to explain which test level proves each step of that workflow.

## Completion Check

The learner should consider this tutorial understood when they can:

- describe the architecture without defaulting to microservices language
- explain why module ownership matters inside one application
- identify which module owns which data and workflows
- explain how this architecture balances growth with operational simplicity
- explain why better modularity often comes before any service extraction
- explain why a good modular monolith test suite must cover module boundaries, not just internal methods
