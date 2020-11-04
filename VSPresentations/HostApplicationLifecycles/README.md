# Host Application Script

Just so everyone is aware, I am going to be posting polls in #ops-knowledge-drops

I would highly recommend participating in the polls! Even if you don't know, make an educated guess!

Brennan will be monitoring for correct answers, and whoever gets the most correct answers will be able to redeem at least 5 bonus stars from Brennan.

## Prelims

Ok, quick poll; how many of you work with dotnet core worker services

> Poll: `/polly "Do you work with dotnet core worker services?" "Yes" "No"`

## Basic Worker

This is a basic first stab at a worker services. While the application hasn't been cancelled, keep working. You can see that the work here is just a sleep for a random amount of time. Let's run it and see what it looks like.

> Run the project

Looks to be running fine. We are receiving logs showing that it is doing the work and incrementing the counter.

> Attempt to Ctrl-C * 2 to try and stop the app

I've tried to issue a stop to the application. And we can see in the app logs that the application said it was going to stop, but it just keeps working. What gives?

> Poll: `/polly "Why won't the application stop?" "The console doesn't work like a Windows Service stop" "The while loop doesn't check the CancellationToken correctly" "The application didn't actually start all the way" "It has high priority work that takes precedence"`

The correct answer is 3: The application didn't actually start all the way!

Let me explain. The ExecuteAsync process is a Task that returns a running task that represents the work that needs to be done in this worker. An application can have several different workers. When the application starts, it will fire of each worker's ExecuteAsync, and then hold on to the task to listen to changes.

However, this implementation is actually synchronous! Because we have no `await`'s in here, and haven't wrapped it in a `Task.Run()`, the application never finishes "starting" this worker. Because the application doesn't finish the start-up process, it isn't prepared to stop. Likely, the CancellationToken isn't set-up properly, or the application is still running a main thread trying to start up (which we don't let it finish).

The solution is simple: make the application asynchronous!

## 02 Basic Worker with Async

> Open 02-BasicWorkWithAsync.Worker

The main difference between the last project and this project is line 31. Instead of using a `Thread.Sleep()`, instead we are using an `await Task.Delay()`. Now the method runs synchronously until it hits the `await` keyword. Then it will return the Task to the application, and continue doing the work in its own thread.

Let's run it and see what happens.

> Run Project

Ok, everything seems to be running as expected. Now let's try and stop it.

> Ctrl-C * 2

That seems more like it! If we take a look back over the logs, we'll see a really important line: `Application started. Press Ctrl+C to shut down.`

This log means that the application was able to successfully start, and can be shut down correctly.

If you have worked with the default template provided by Visual Studio or the Microsoft documentation, you may be wondering why I don't give `Task.Delay()` the CancellationToken. Let's add it back in the way the template does it.

> Add `stoppingToken` to the `Task.Delay()` on line 30

> Poll: `/polly "Why don't I add the CancellationToken to Task.Delay?" "It is the wrong token and doesn't work as expected" "It throws an exception" "It doesn't cancel correctly because it is after the await"`

Let's try it and see

> Run project. Right after a "Starting to work on", Ctrl-C * 2

The application appears to have stopped cleanly. No errors were thrown. But if we look, we did not finish the work that we started. If this were a rabbit message or a Camunda External Task, we could be leaving it in a bad state; they think it is being worked on, but we dropped it.

What happens if we wrap the Task.Delay in a try/catch block?

> Copy the following code into the while loop

```cs
try
{
    _logger.LogInformation($"Starting work on {workId}");
    await Task.Delay(rand.Next(5_000, 6_500), stoppingToken);
    _logger.LogInformation($"Finished work on {workId++}");
}
catch (Exception e)
{
    _logger.LogError($"There was an error! {e}");
}
```

Let's run it again, and try and stop it

> Run project. Right after a "Starting to work on", Ctrl-C * 2

See! It did throw an exception! If the CancellationToken is cancelled while in a Task.Delay is waiting, it will immediately throw an exception. This is why we didn't finish the work that was started; the exception bubbled up to the application, who was ok with a TaskCanceledException.

If the CancellationToken isn't in the Task.Delay(), then it just waits, finishes the work, then the while loop detects the cancellation and quits. Much more graceful.

## 03 Exception Thrown

Ok, we've seen how to make the execute asynchronous so that it stops. What happens if an exception is thrown while work is being done.

> Open 03-ExceptionThrown.Worker

This work is similar to what we were doing before, but now there is a time bomb on the work id 5. It throws some exception. What do you think is going to happen?

> Poll: `/polly "What will happen when the exception is thrown?" "The worker stops, but the application keeps running" "The application stops" "Work id 5 doesn't finish, but the rest of the work continues" "Visual Studio hits an Exception Breakpoint"`

Ok, lets run it and find out.

> Run the project

If you guessed option 1, you were correct!

The application does not stop when the worker stops. An application can be comprised of multiple workers, remember. So one failing shouldn't mean it stops. However, in practice at VU, applications tend to only have one worker! What does this mean?

Well, when this is run as a Windows Service, it appears to be run, even if the worker has stopped. Bard saw this with their Camunda External Task workers; we had to monitor Camunda to see if it had stopped processing, and restart the application. It would be much better if the application stops when the worker stops. This way Windows Service can monitor and restart failed applications, and any monitoring tools we have can be used reliably.

## 04 Host Application Stopping

Let's take a look at the changes required to stop the host

> Open 04-HostApplicationStopping.Worker

The core code is the same, but I've wrapped the entire thing in a try/catch block. You'll see that if an Exception is thrown, then we call StopApplication on HostApplicationLifetime. HostApplicationLifetime is being injected in the constructor, and lets us access the application that the working is running in.

Let's run this, and see what happens:

> Run project

That's what I want to see! Now when the exception is thrown and the worker stops, the application stops with it.

## 05 Watch for Threads

Ok, now let's add in some complexity. Most of our applications aren't a single unit of work in a while loop.

> Open 05-WatchForThreads.Worker

Ok, let's look at what's going on here.

First, instead of a single unit of work in a while loop, we spin up 4 `Consumer`'s and start their `DoTheWork` method. Inside of the `DoTheWork` method, we can see that the actual work being down is similar. I've increased the numbers by 1000 so we can see the difference between the threads, but the concept is the same.

Consumers have the same time bomb that the previous project had; after they do 5 units of work, they throw an exception.

Back in the `ExecuteAsync`, we collect the 4 consumers work as Tasks in a variable called tasks, then we set the execute thread to watch for a cancellation. Every second it will check if a cancellation has been requested yet.

Finally, we have the code still wrapped in a try/catch block to stop the application when an exception is thrown.

> Poll: `/polly "What is going to happen?" "The application runs until all the workers hit their exception, then it stops" "The workers keep restarting after they hit their exceptions to stay at 4 consumers" "The Consumers all stop when they hit their exception, but the application is still running" "The math for the time bomb is incorrect and won't thrown an exception"`

Let's run it and find out.

> Run the project

Looks like all four consumers hit their time bombs, stopped processing, but the application is still running. Why?

It's because the ExecuteAsync is not watching the Tasks, but instead only cares about the CancellationToken. We can change that by replacing the while loop with a `await Task.WhenAll(tasks.ToArray());`

> Replace the while loop in ExecuteAsync with `await Task.WhenAll(tasks.ToArray());`

This means the worker will stop when the Tasks are all no longer in a `Running` state, whether that is Finished or Faulted.

Let's run it and see what happens:

> Run the project

That's what I would expect. When the Consumers all throw their faults, the `await Task.WhenAll` finishes, throws an exception, which get's caught and stops the application. We could move on and add some self-healing mechanisms in here, but for now we're happy with it.

Ok, what about the opposite? What happens if I run the project, and then stop the application while the consumers are processing?

> Run the project, and then Ctrl-C * 2 to kill it

Now it looks like the problem we saw before we made the method asynchronous. The stop is requested, but the Consumers don't know about it, so they just keep on going until they hit their time bombs.

Well, we removed the CancellationToken away from the work, and now there is no good way to tell the Consumer to finish it's current work, then stop. So the simple solution here is to return that responsibility to the Consumer were the work is happening.

To do that, we'll have `DoTheWork` take a CancellationToken.

Then we'll give the method the stoppingToken passed in to ExecuteAsync

Finally, we'll change the while loop in `DoTheWork` to work until a cancellation is requested.

Now let's run it and see

> Run the project, and then Ctrl-C * 2 to kill it

Much better! We can see the Consumers taking the time to finish their work, and once all of them are done, the application stops.

## Shutdown Timeouts

One last thing; When the application issues a shutdown/cancellation, it has a master timeout that it is willing to wait. By default, that timeout is 5 seconds, which in my experience is not enough time for all the consumers to finish. 

If you want to configure how long the application is willing to wait, you need this

> Open 05-WatchForThreads.Program

```cs
services.Configure<HostOptions>(o =>
{
    o.ShutdownTimeout = TimeSpan.FromMinutes(5);
});
```

This gives us 5 minutes, but you can set it to whatever you feel comfortable with. Keep in mind that if a Consumer is stuck for some reason, you'll have to wait the entire shutdown timeout. This applies for Stopping/Restarting a Windows Service and when deploying new code.

## Questions?