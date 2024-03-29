﻿# Ollama connector

## Summary
This project allow the use of [Ollama](https://github.com/jmorganca/ollama) as a service with REST API. We base our component from https://github.com/awaescher/OllamaSharp.

## Files
* [Ollama Connector](src/OllamaConnector.cs) is the main component for connect with ollama.
* [Ollama Connector Configuration](src/OllamaConnectorConfiguration.cs) is the configuration class for [Ollama Connector](src/OllamaConnector.cs).
* [Ollama Api Client](src/API/OllamaApiClient.cs) and [Ollama Tasks](src/API/OllamaTasks.cs) are based on [OllamaSharp](https://github.com/awaescher/OllamaSharp).
* [Models folder](src/Models/) and [Streamer folder](src/Streamer/) are copies off [OllamaSharp](https://github.com/awaescher/OllamaSharp).

## Curent issues

## Future works

## Example
        static void testOllama(Pipeline p)
        {
            OllamaConectorConfiguration config = new OllamaConectorConfiguration();
            OllamaConnector ollama = new OllamaConnector(p, config);
            KeyboardReader.KeyboardReader reader = new KeyboardReader.KeyboardReader(p);

            reader.Out.PipeTo(ollama.In);
            ollama.Out.Do((m, e) => { Console.WriteLine($"{(e.CreationTime - e.OriginatingTime).TotalSeconds} \n {m}"); });
        }