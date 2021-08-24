# Lapine
A C# client library for RabbitMQ 3.7+

![.NET Core](https://github.com/Matthew-Davey/Lapine/workflows/.NET%20Core/badge.svg?branch=develop)

## Project Status (2021-08-24)
Lapine is in the early stages of development and is **definitely not** suitable for use in your project(s) yet.

## Implementation Status (AMQP Protocol)

#### Protocol
- [x] Basic framing
- [x] Method framing - Connection class
- [x] Method framing - Channel class
- [x] Method framing - Exchange class
- [x] Method framing - Queue class
- [x] Method framing - Basic class
- [x] Method framing - Transaction class
- [x] Content Header framing
- [x] Content Body framing
- [x] Heartbeat framing
- [ ] Error handling

#### Connection
- [x] Protocol negotiation
- [x] PLAIN authentication
- [ ] AMQPLAIN authentication
- [ ] EXTERNAL authentication
- [x] Connection tuning
- [x] Heartbeating
- [x] Cluster node selection
- [ ] Automatic reconnection

#### Channel
- [x] Open
- [x] Close

#### Exchange
- [x] Declare
- [x] Delete

#### Queue
- [x] Declare
- [x] Bind
- [x] Unbind
- [x] Purge
- [x] Delete

#### Basic
- [x] Publish
  - [x] Mandatory
  - [x] Immediate (Unsupported in RabbitMQ)
  - [x] Large messages (> MaxFrameSize)
- [x] Qos
- [x] Get
  - [x] Empty messages (content header only)
  - [x] Large messages (> MaxFrameSize)
- [x] Consume
- [x] Deliver
  - [x] Empty messages (content header only)
  - [x] Large messages (> MaxFrameSize)
  - [x] Concurrent message processing
- [x] Ack
- [x] Reject
- [ ] Cancel
- [ ] Return
  - [ ] Empty messages (context header only)
  - [ ] Large messages (> MaxFrameSize)
- [ ] Recover

#### Transaction
- [ ] Select
- [ ] Commit
- [ ] Rollback

#### RabbitMQ Extensions
- [x] Publisher Confirms
- [ ] Consumer Cancel
- [ ] Consumer Prefetch
- [ ] Consumer Priorities
- [ ] Direct Reply-to
- [ ] Blocked Connections
- [x] Basic Nack
- [ ] Exchange To Exchange Binding
- [ ] Alternate Exchanges
- [ ] Sender Routing
- [ ] TTL
- [ ] Dead Lettering
- [ ] Queue Length Limits
- [ ] Priority Queues
- [ ] Auth Failure
- [ ] Quorum Queues
- [ ] Streams (via AMQP)

## Implementation Status (Streams Protocol)
TODO
