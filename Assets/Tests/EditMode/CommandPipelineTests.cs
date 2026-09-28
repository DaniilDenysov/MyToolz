using System;
using System.Collections.Generic;
using MyToolz.DesignPatterns.Command;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyToolz.Tests.EditMode
{
    public class CommandPipelineTests
    {
        private sealed class RecordingCommand : ICommand
        {
            private readonly List<string> log;
            private readonly string name;
            private readonly Action onExecute;

            public RecordingCommand(List<string> log, string name, Action onExecute = null)
            {
                this.log = log;
                this.name = name;
                this.onExecute = onExecute;
            }

            public void Execute()
            {
                log.Add(name);
                onExecute?.Invoke();
            }
        }

        private sealed class LongCommand : ICompletableCommand, ICancellableCommand
        {
            public bool IsFinished { get; set; }
            public bool Executed { get; private set; }
            public bool Cancelled { get; private set; }
            public void Execute() => Executed = true;
            public void Cancel() => Cancelled = true;
        }

        private sealed class ThrowingCommand : ICommand
        {
            public void Execute() => throw new InvalidOperationException("boom");
        }

        [Test]
        public void SynchronousCommands_AllRun_WithDefaultCapacityOfOne()
        {
            var log = new List<string>();
            var pipeline = new CommandPipeline<ICommand>();

            pipeline.Enqueue(new RecordingCommand(log, "a"));
            pipeline.Enqueue(new RecordingCommand(log, "b"));
            pipeline.Enqueue(new RecordingCommand(log, "c"));

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, log, "a plain command releases its slot when Execute returns");
            Assert.IsEmpty(pipeline.ExecutingCommands);
        }

        [Test]
        public void CompletableCommand_HoldsItsSlot_UntilFinished()
        {
            var log = new List<string>();
            var pipeline = new CommandPipeline<ICommand>();
            var longCommand = new LongCommand();

            pipeline.Enqueue(longCommand);
            pipeline.Enqueue(new RecordingCommand(log, "next"));
            Assert.IsEmpty(log, "the slot is still taken");

            longCommand.IsFinished = true;
            pipeline.Update();

            CollectionAssert.AreEqual(new[] { "next" }, log);
        }

        [Test]
        public void Complete_ReleasesTheSlotImmediately()
        {
            var log = new List<string>();
            var pipeline = new CommandPipeline<ICommand>();
            var longCommand = new LongCommand();
            pipeline.Enqueue(longCommand);
            pipeline.Enqueue(new RecordingCommand(log, "next"));

            Assert.IsTrue(pipeline.Complete(longCommand));

            CollectionAssert.AreEqual(new[] { "next" }, log);
        }

        [Test]
        public void ReentrantEnqueue_DoesNotExceedCallStackSize()
        {
            var pipeline = new CommandPipeline<ICommand>(1, 8);
            var inner = new LongCommand();
            var outer = new LongCommand();
            int maxConcurrent = 0;
            var starter = new RecordingCommand(new List<string>(), "starter", () =>
            {
                pipeline.Enqueue(inner);
                maxConcurrent = Math.Max(maxConcurrent, pipeline.ExecutingCommands.Count);
            });

            pipeline.Enqueue(outer);
            outer.IsFinished = true;
            pipeline.Enqueue(starter);
            pipeline.Update();

            Assert.LessOrEqual(maxConcurrent, 1, "the slot is reserved before Execute runs");
            Assert.IsTrue(inner.Executed, "work enqueued during Execute still runs once a slot frees up");
        }

        [Test]
        public void ThrowingCommand_ReleasesItsSlot()
        {
            var log = new List<string>();
            var pipeline = new CommandPipeline<ICommand>();
            LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("boom"));

            pipeline.Enqueue(new ThrowingCommand());
            pipeline.Enqueue(new RecordingCommand(log, "after"));

            CollectionAssert.AreEqual(new[] { "after" }, log);
        }

        [Test]
        public void Clear_CancelsExecutingCommands_AndDropsQueued()
        {
            var log = new List<string>();
            var pipeline = new CommandPipeline<ICommand>();
            var longCommand = new LongCommand();
            pipeline.Enqueue(longCommand);
            pipeline.Enqueue(new RecordingCommand(log, "queued"));

            pipeline.Clear();
            pipeline.Update();

            Assert.IsTrue(longCommand.Cancelled);
            Assert.IsEmpty(log);
            Assert.IsEmpty(pipeline.ExecutingCommands);
        }

        [Test]
        public void FullQueue_DropsTheOldestQueuedCommand()
        {
            var log = new List<string>();
            var pipeline = new CommandPipeline<ICommand>(1, 2);
            var blocker = new LongCommand();
            pipeline.Enqueue(blocker);
            pipeline.Enqueue(new RecordingCommand(log, "a"));
            pipeline.Enqueue(new RecordingCommand(log, "b"));
            pipeline.Enqueue(new RecordingCommand(log, "c"));

            blocker.IsFinished = true;
            pipeline.Update();

            CollectionAssert.AreEqual(new[] { "b", "c" }, log);
        }
    }
}
