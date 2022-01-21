# Kafka.TestFramework
An in-memory test framework for Kafka clients which can be used to subscribe on request messages and respond with response messages. The test server can be used in-memory or connected to clients over TCP.

[![Continuous Delivery](https://github.com/Fresa/Kafka.TestFramework/actions/workflows/ci.yml/badge.svg)](https://github.com/Fresa/Kafka.TestFramework/actions/workflows/ci.yml)

## Download
https://www.nuget.org/packages/kafka.testframework

## Getting Started
The test framework can be used in-memory or by setting up a TCP socket that the kafka client can connect to. See the [`integration tests`](https://github.com/Fresa/Kafka.TestFramework/blob/master/tests/Kafka.TestFramework.Tests).

### v2.x
Now supports [Kafka.Protocol](https://github.com/Fresa/Kafka.Protocol) v2.x. 