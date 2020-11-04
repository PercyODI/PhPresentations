# Host Application Script

## Prelims

Ok, quick poll; how many of you work with dotnet core worker services

> Poll: `/polly "Do you work with dotnet core worker services?" "Yes" "No"`

## Basic Worker

> Set `01-BasicWorker` as Startup Project

This is a basic first stab at a worker services. While the application hasn't been cancelled, keep working. You can see that the work here is just a sleep for a random amount of time. Let's run it and see what it looks like.

> Run the project

Looks to be running fine. We are receiving logs showing that it is doing the work and incrementing the counter.

> Attempt to Ctrl-C * 2 to try and stop the app

I've tried to issue a stop to the application. And we can see in the app logs that the application said it was going to stop, but it just keeps working. What gives?

> Poll: `/polly "Why won't the application stop?" "The console doesn't work like a Windows Service stop" "The while loop doesn't check the CancellationToken correctly" "The application didn't actually start all the way" "It has high priority work that takes precedence"`

The correct answer is 3: The application didn't actually start all the way!

Let me explain. The ExecuteAsync process is a Task that returns a running task that represents the work that needs to be done in this worker. An application can have several different workers. When the application starts, it will fire of each worker's ExecuteAsync, and then hold on to the task to listen to changes.

However, this implementation is actually synchronous! Because we have no `await`'s in here, and haven't wrapped it in a `Task.Run()`, the application never finishes "starting" this worker. Because the application doesn't finish the start-up process, it isn't prepared to stop. Likely, the CancellationToken isn't set-up properly, or the application is still running a main thread trying to start up (which we don't let it do).

The solution is simple: make the application asynchronous!

> Set `02-BasicWorkerWithAsync` as Startup Project

The main difference between the last project and this project is line 31. Instead of using a `Thread.Sleep()`, instead we are using an `await Task.Delay()`. Now the method runs synchronously until it its the `await` keyword. Then it will return the Task to the application, and continue doing the work in its own thread.

Let's run it and see what happens.