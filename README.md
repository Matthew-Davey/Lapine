# Lapine
A C# client library for the Advanced Message Queueing Protocol (AMQP) v0.9.1

![.NET Core](https://github.com/Matthew-Davey/Lapine/workflows/.NET%20Core/badge.svg?branch=develop)

## Project Status (2020-10-14)
Lapine is in the early stages of development and is **definitely not** suitable for use in your project(s) yet.

## Implementation Status

#### Protocol
- [x] Basic framing
- [x] Method framing - Connection class
- [x] Method framing - Channel class
- [x] Method framing - Exchange class
- [x] Method framing - Queue class
- [x] Method framing - Basic class
- [x] Method framing - Transaction class
- [ ] Header framing
- [ ] Content framing
- [x] Heartbeat framing

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
- [ ] Flow
- [x] Close

#### Exchange
- [x] Declare
- [ ] Delete

#### Queue
- [ ] Declare
- [ ] Bind
- [ ] Unbind
- [ ] Purge
- [ ] Delete

#### Basic
- [ ] Publish
- [ ] Qos
- [ ] Get
- [ ] Consume
- [ ] Deliver
- [ ] Ack
- [ ] Reject
- [ ] Return
- [ ] Cancel
- [ ] Recover

#### Transaction
- [ ] Select
- [ ] Commit
- [ ] Rollback
