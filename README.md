# Kafka.TestFramework
An in-memory test framework for Kafka clients which can be used to subscribe on request messages and respond with response messages. The test server can be used in-memory or connected to clients over TCP.

[![Build status](https://ci.appveyor.com/api/projects/status/3by56qq57a8or5a2?svg=true)](https://ci.appveyor.com/project/Fresa/kafka-testframework)

[![Build history](https://buildstats.info/appveyor/chart/Fresa/kafka-testframework)](https://ci.appveyor.com/project/Fresa/kafka-testframework/history)

## Download
https://www.nuget.org/packages/kafka.testframework

## Getting Started
The test framework can be used in-memory or by setting up a TCP socket that the kafka client can connect to. See the [`integration tests`](https://github.com/Fresa/Kafka.TestFramework/blob/master/tests/Kafka.TestFramework.Tests).
