﻿using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using ThreadPool;
using ThreadPool.MyTask;

namespace ThreadPoolTest
{
  [TestFixture]
  public class ThreadPoolTests
  {
    private const int ThreadPoolSize = 4;

    [Test]
    public void AddOneTaskTest()
    {
      using (var tp = new MyThreadPool(ThreadPoolSize))
      {
        using (var task = new MyTask<int>(() => 2 + 2))
        {
          tp.Enqueue(task);
          Assert.AreEqual(4, task.Result);
        }
      }
    }

    [Test]
    public void DisposeThreadPoolTwiceTest()
    {
      var tp = new MyThreadPool(ThreadPoolSize);
      tp.Dispose();
      Assert.Throws<ObjectDisposedException>(() => tp.Dispose());
    }

    [Test]
    public void AddNullTaskTest()
    {
      using (var tp = new MyThreadPool(ThreadPoolSize))
      {
        Assert.Throws<ArgumentNullException>(() => tp.Enqueue((IMyTask<int>)null));
      }
    }


    [Test]
    public void AddMoreTasksThanThreadPoolSize()
    {
      const int tasksCount = 8;
      using (var tp = new MyThreadPool(ThreadPoolSize))
      {
        var tasks = new List<MyTask<int>>();
        for (var i = 0; i < tasksCount; ++i)
        {
          var task = new MyTask<int>(() =>
          {
            Thread.Sleep(200);
            return 2 + 2;
          });
          tp.Enqueue(task);
          tasks.Add(task);
        }

        tasks.ForEach(task =>
        {
          Assert.AreEqual(4, task.Result);
          task.Dispose();
        });
      }
    }

    [Test]
    public void AddTasksInParallelTest()
    {
      const int parallelThreadsCount = 40;
      using (var tp = new MyThreadPool(ThreadPoolSize))
      {
        var threads = new List<Thread>();
        for (var i = 0; i < parallelThreadsCount; i++)
        {
          var thread = new Thread(() =>
          {
            var j = i;
            using (var task = new MyTask<int>(() => j))
            {
              tp.Enqueue(task);
              Assert.AreEqual(j, task.Result);
            }
          });
          threads.Add(thread);
          thread.Start();
        }

        threads.ForEach(thread => thread.Join());
      }
    }

    [Test]
    public void AddTasksAndCheckThreadsCount()
    {
      using (var tp = new MyThreadPool(ThreadPoolSize))
      {
        using (var task1 = new MyTask<int>(() =>
        {
          Thread.Sleep(500);
          return 2 + 2;
        }))
        using (var task2 = new MyTask<int>(() =>
        {
          Thread.Sleep(500);
          return 2 + 2;
        }))
        {
          tp.Enqueue(task1);
          tp.Enqueue(task2);
          Assert.AreEqual(ThreadPoolSize, tp.Size);
          Assert.AreEqual(4, task1.Result);
          Assert.AreEqual(4, task2.Result);
        }
      }
    }

    [Test]
    public void ContinueWithTest()
    {
      using (var tp = new MyThreadPool(ThreadPoolSize))
      {
        using (var taskA = new MyTask<string>(() => "A"))
        using (var taskB = taskA.ContinueWith(a =>
        {
          Thread.Sleep(2000);
          return $"{a}B";
        }))
        using (var taskC = taskB.ContinueWith(ab =>
        {
          Thread.Sleep(1000);
          return $"{ab}C";
        }))
        using (var taskD = taskB.ContinueWith(ab =>
        {
          Thread.Sleep(1000);
          return $"{ab}D";
        }))
        {
          tp.Enqueue(taskD);
          tp.Enqueue(taskC);
          tp.Enqueue(taskB);
          tp.Enqueue(taskA);
          Assert.AreEqual("ABC", taskC.Result);
          Assert.AreEqual("ABD", taskD.Result);
        }
      }
    }
  }
}
