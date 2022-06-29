<div align="center">
<img src="./R6sLogo.png" height="300">
</div>

# ✏️ Contributors
- Dillon Franke (@dillonfranke)
- Michael Maturi (@a-marionette)

# ⚓ Route Sixty-Sink
***Route Sixty-Sink*** is an open source tool that enables defenders and security researchers alike to quickly identify vulnerabilities in any .NET assembly using automated source-to-sink analysis.

Identifying vulnerabilities within application binaries or source code is often a long and tedious process. To help with this, **source-to-sink analysis** is a form of data flow analysis that attempts to identify user input that is passed as the argument of a dangerous function call (a “sink”).

By enumerating a list of sinks, identifying them within an application, and backtracking them to user-controlled input, source-to-sink analysis can identify high fidelity vulnerabilities.

# ❓ What Does Route Sixty-Sink Solve?

While effective, proper source-to-sink analysis is a time consuming and manual process that is often infeasible due:

- **Complex Input Tracing:** Identifying an application’s inputs can be difficult, especially in web applications where MVC architectures are used. ***Route Sixty-Sink*** handles a wide variety of routing and input parsing scenarios to automate this process.
- **Application Size:** Large C# applications quickly become infeasible to obtain full code coverage using manual analysis. ***Route Sixty-Sink*** automates this process to allow analysis of most programs within seconds.
- **Nested Sinks:** Sinks may be overlooked that are hiding within interfaces, extended classes, or a series of nested function calls. ***Route Sixty-Sink*** identifies these sinks by creating a call graph of all classes and method calls and then recursively following them.

# 💪  How Does it Work?

***Route Sixty-Sink*** traces the flow of user input through any .NET assembly and determines whether it is passed as an argument to a dangerous function call (a “sink”). ***Route Sixty-Sink*** does this using two main modules:

1. ***RouteFinder***, which enumerates API routes in ASP Net Core MVC and classic ASP page web applications.
2. ***SinkFinder***, which takes an entry point and creates a call graph of all classes and method calls. Then, it queries strings, method calls, and class names for “sinks”.

By tying these two pieces of functionality together, ***Route Sixty-Sink*** quickly identifies high fidelity vulnerabilities that would be difficult to discover using black box or manual static analysis approaches.

# ⛑️ Installation, Usage, and Examples

For usage see the [Wiki Page](https://github.com/mandiant/route-sixty-sink/wiki) page.
