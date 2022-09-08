module EventStream

open System
open Amqp

type EventMessage =
    | Connected
    | ConnectionFailed of Fault: Exception
    | FrameReceived of Frame

let private eventStream = Event<EventMessage>()
let EventStream = eventStream.Publish

let publish message =
    eventStream.Trigger message
