# Event-Driven Architecture Learning Guide

## Learning Objectives

By the end of this tutorial, the learner should be able to:

- explain what event-driven architecture is beyond the phrase "publish and subscribe"
- identify where asynchronous fan-out is useful
- distinguish event-driven design from microservices and from simple background jobs
- describe why idempotency, retries, and dead-letter handling matter
- design a read model or status projection for operator visibility

## Concepts To Focus On

### 1. Event As A Business Signal

An event represents something that already happened.

Focus on event names that reflect facts:

- `AssetUploadCompleted`
- `MetadataExtracted`
- `TranscodeCompleted`

Avoid treating events as vague technical messages without clear business meaning.

### 2. Producer And Consumer Independence

In this architecture, the producer should not need to know how many consumers exist or how each consumer works.

That decoupling is a primary reason to use events.

### 3. Fan-Out

One event can drive several downstream reactions:

- update processing status
- generate thumbnails
- trigger transcoding
- notify operations users

This is one of the clearest benefits of the pattern.

### 4. Eventual Consistency

The system does not become "wrong" because all work is not visible immediately.

Instead, the architecture requires explicit handling of in-progress, delayed, failed, and retried states.

### 5. Idempotency

Consumers must be safe when an event is delivered more than once.

This is a required design property, not an optional hardening feature.

## What To Observe In This Tutorial

When reading the tutorial, pay attention to:

- which component owns the initial transactional write
- which event is emitted from that write
- which consumers react independently
- how operator-visible status is built from background outcomes
- where retry and dead-letter behavior is defined

## Common Misunderstandings

### "Event-driven means everything must be a microservice"

False.

You can build an event-driven system with one API and several workers. The key characteristic is the event-based coordination model.

### "Events remove the need for workflow design"

False.

Events reduce direct coupling, but you still need explicit thinking about success, failure, retries, ordering, and observability.

### "Asynchronous means faster for users in every case"

Not automatically.

Asynchronous processing improves responsiveness for long-running work, but it also introduces delayed visibility and more operational complexity.

### "If messaging works locally, reliability is solved"

False.

The real challenge is duplicate delivery, temporary failures, poison messages, and replay-safe handling.

## Compare It To Other Tutorials

Compare `005` with:

- `004-microservices`
  `004` focuses on service ownership and distributed workflows across separate services. `005` focuses on events as the coordination model, even if the deployment shape is simpler.

- `010-web-queue-worker`
  `010` is usually one producer handing work to one worker path. `005` is broader and emphasizes multiple independent reactions to the same event stream.

- `006-cqrs`
  `006` separates write and read concerns. `005` separates producers and consumers through asynchronous events.

## Review Questions

- What makes an event different from a command?
- Why is idempotency required in event-driven systems?
- When is a status projection necessary?
- What problems appear when events arrive late or more than once?
- Why is request-response thinking insufficient for this architecture?

## Practical Exercise

Take the asset pipeline in this tutorial and extend it mentally with a new requirement:

- after `TranscodeCompleted`, also trigger compliance watermarking for premium content only

Ask:

- should the producer change?
- should an existing consumer change?
- should a new consumer subscribe?
- how would retries and operator visibility work?

If you can answer those clearly, the event-driven boundary is becoming understandable.

## Completion Criteria

The learner has understood this tutorial when they can:

- model a workflow as events and reactions
- explain the difference between producer truth and projected status
- identify failure and retry requirements without prompting
- justify when this architecture is better than a synchronous CRUD design
