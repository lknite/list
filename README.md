# list #

## Summary ##
Distributed list processing

## What is it ##
(implementation in progress)

When working through a list of 1000 items, if power is lost, you need to be able
to pick back up where you left off.  In order to do this you need to keep track
as you progress through items, making a note when you start and when you finish.

Turns out, if you implement list processing with a service and a mutex you can
have multiple clients help to process a list through to completion.

list works by managing the allocation of blocks of work to be processed, as well
as tracking completed work.  list is not the first to implement distributed
processing, but it is an ideal implementation for simple use cases.


## Features ##
- an auth microservice to gather user claims via oidc and generate an api_key
- storing api_key and claims using a kubernetes custom resource definition
- retrieving user claims with an api_key via rest api and websocket access