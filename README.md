# CloudPatternSamples

Simple Implementations in C# of some things that are often needed at high-volume cloud services

- Queued Async Execution with an Transaction Speed throttle (Used to control pressure and traffic shape load inside your app so you don't get throttled by an external service.
- Software LB (Spread the load over multiple instances of a cloud service, but using their native c# clients, no http LB here - use the native SDK and lb through that)! 

- HttpBench (Ultra simple load generation utility sample, used to benchmark throughput for a simulated profanityfilter service.)
- Demo Host, brings these samples together and uses the Microsoft Azure Cognitive Services Content Moderator Service as a target.
