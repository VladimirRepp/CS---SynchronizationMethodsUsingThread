#define USEDOTNET9

using System;
using System.Threading;
using System.Threading.Lock; // начиная с .Net 9; В .Net Framework не доступен!

namespace Sample_10
{
    // Примеры объектов синхронизации 
    internal class Program
    {
        private static int _globalCounter; // некоторый общий ресурс

        private static Mutex _mutex;

        static void Main(string[] args)
        {
            //Sample1_Mutex();
            //Sample2_Semaphore();
            //Sample3_Event();
            Sample5_Volatile();

            Console.ReadLine();
        }

        #region === Examples of Mutex ===
        private static void PrintWithMutex()
        {
            _mutex.WaitOne(); // без этого, потоки идут хаотично 

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"{Thread.CurrentThread.Name} _globalCounter = {_globalCounter}");
                _globalCounter++;

                Thread.Sleep(100);
            }

            _mutex.ReleaseMutex();
        }

        /// <summary>
        /// Mutex - гарантирует, что только один поток или процесс может получить мъютекс в любой момент времени.
        /// </summary>
        private static void Sample1_Mutex()
        {
            _mutex = new Mutex();

            for(int i = 0; i < 5; i++)
            {
                Thread myThread = new Thread(PrintWithMutex);
                myThread.Name = $"Поток №{i}";
                myThread.Start();
            }
        }
        #endregion

        #region === Examples of Semaphore ===

        /// <summary>
        /// Semaphore - позволяет ограничить количество потоков, которые имеют доступ к ресурсу.
        /// 
        /// Пример задачи: 
        /// Есть некоторое количество читателей и органичение на 
        /// одновременное посещение читалей в читательском зале.
        /// </summary>
        private static void Sample2_Semaphore()
        {
           Reader[] readers = new Reader[10];

            for (int i = 0; i < readers.Length; i++)
            {
                readers[i] = new Reader(i + 1);
                readers[i].Start();
            }
            
            foreach (var reader in readers)
            {
                reader.Thread.Join();
            }   
        }

        private class Reader
        {
            private static Semaphore SEMAPHORE = new Semaphore(3, 3);
            private Thread _thread;
            private int _counOfReadingsPerThisReader = 3; // количество чтений одного читателя
            private Random _rand;
            
            public Thread Thread => _thread;
            
             public Reader(int id)
              {
                  _thread = new Thread(Read);
                  _thread.Name = $"Читатель {id}";
                 _rand = new Random(id);
              }
        
              public void Start()
              {
                  _thread.Start();
              }
            
              private void Read()
              {
                while(_counOfReadingsPerThisReader > 0)
                {
                    SEMAPHORE.WaitOne();

                    Console.WriteLine($"{Thread.CurrentThread.Name} - входит в библиотеку!");
                    Console.WriteLine($"{Thread.CurrentThread.Name} - читает!");
                    Thread.Sleep(_rand.Next(500, 1000)); // имитация чтения

                    Console.WriteLine($"{Thread.CurrentThread.Name} - покидает библиотеку!");

                    SEMAPHORE.Release();

                    _counOfReadingsPerThisReader--;
                    Thread.Sleep(_rand.Next(500, 1000)); // имитация что ушли
                }
            }
        }

        #endregion

        #region === Examples of Events === 

        /// <summary>
        /// События применяются когда один поток ожидает появления некоторого события в другом потоке. 
        /// Как только такое событие возникает, один поток уведомляет второй, тем самым позволяя возобновить 
        /// работы второго потока. 
        /// 
        /// Существуют несколько событий, например:
        /// * ManualResetEvent - сброс события инициируем самостоятельно 
        /// * ManualResetEventSlim - используем меньше памяти в сравнении с ManualResetEvent
        /// * AutoResetEvent - сброс события происходит автоматически самостоятельно 
        /// * CountdownEvent - ожидает, пока счетчик не достигнет указанного значения
        /// 
        /// Пример использования события ManualResetEvent:
        /// </summary>
        class MyThread
        {
              private Thread myThread;

              private ManualResetEvent _mre;
              private Random _rand;
            
              public Thread MyThreadInstance => myThread;
            
              public MyThread(string name, ManualResetEvent mre)
              {
                  _mre = mre;
            
                  _rand = new Random();
            
                  myThread = new Thread(Run);
                  myThread.Name = name;
              }
            
              public void Start()
              {
                  myThread.Start();
              }
            
              private void Run()
              {
                  Console.WriteLine("Начало работы - " + Thread.CurrentThread.Name);
            
                  for(int i = 0; i < 10; i++)
                  {
                      Console.WriteLine("Работает - " + Thread.CurrentThread.Name);
                      Thread.Sleep(_rand.Next(500, 1000));
                  }
            
                  Console.WriteLine("Завершена работа - " + Thread.CurrentThread.Name);
            
                  _mre.Set(); // установка события - сигнал основному потоку что работа завершена
              }
        }

        private static void Sample3_Event()
        {
            ManualResetEvent manualResetEvent = new ManualResetEvent(false); // false - событие первоначально не уведомляется 

            MyThread myThread1 = new MyThread("Поток с событием №1", manualResetEvent);
            myThread1.Start()

            Console.WriteLine("Основной поток ожидает событие №1...");
            manualResetEvent.WaitOne();

            Console.WriteLine("Основной поток получил уведомление события №1!");
            manualResetEvent.Reset(); // сброс в false, так как нового уведомления еще не было

            myThread1 = new MyThread("Поток с событием №2", manualResetEvent);

            Console.WriteLine("Основной поток ожидает событие №2...");
            manualResetEvent.WaitOne();

            Console.WriteLine("Основной поток получил уведомление события №2!");
        
            Console.ReadLine();
        }

        #endregion

        #region === Examples of critical sections (Lock and Monitor) ===
        /// === Lock ===

#if USEDOTNET9 // если используем .Net 9 и выше

        private static Lock _lockObj; // объект заглужка для синхронизации доступа

        /// <summary>
        /// Класс Lock - синхронизирует некоторую критическую секцию для предотвращения гонки за ресурсами. 
        /// </summary>
        /// 
        private static void Sample4_CriticalSection_Lock() 
        {
            _globalCounter = 0;
            _lockObj = new();

            for(int i = 0; i < 5; i++)
            {
                Thread thread = new Thread(Print_Lock);
                thread.Name = $"Поток №{i}";
                thread.Start();
            }
        }

        private static void Print_Lock()
        {
            lock (_lockObj) // начало критической секции 
            {
                for(int i = 0; i < 5; i++)
                {
                    Console.WriteLine($"{Thread.CurrentThread.Name}: {_globalCounter++}");
                    Thread.Sleep(100);
                }
            }               // окончание критичекской секции
        }

        private static void Print_LockEnter()
        {
            _lockObj.Enter();  // начало критической секции 

            try
            {
                for (int i = 0; i < 5; i++)
                {
                    Console.WriteLine($"{Thread.CurrentThread.Name}: {_globalCounter++}");
                    Thread.Sleep(100);
                }
            }
            finally
            {
                _lockObj.Exit();// окончание критичекской секции
            }             
        }
     
        /// <summary>
        /// TryEnter() вернет:
        /// true - если текущий поток единственный, который удерживает блокировку.
        /// false - иначе.
        /// </summary>
        private static void Print_LockTryEnter()
        {
            if( _lockObj.TryEnter())  // начало критической секции 
            { 
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Console.WriteLine($"{Thread.CurrentThread.Name}: {_globalCounter++}");
                        Thread.Sleep(100);
                    }
                }
                finally
                {
                    _lockObj.Exit();// окончание критичекской секции
                }
            }
        }

        /// <summary>
        /// Является рекомендуемым относительно Microsoft. Также осуществляет вход в критическую 
        /// секцию. Если один поток захватил критическую секцию, остальные ждут ее освобождения. 
        /// </summary>
        private static void Print_LockEnterScope()
        {
            if (_lockObj.EnterScope())  // начало критической секции 
            {
                try
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Console.WriteLine($"{Thread.CurrentThread.Name}: {_globalCounter++}");
                        Thread.Sleep(100);
                    }
                }
                finally
                {
                    _lockObj.Exit();// окончание критичекской секции
                }
            }
        }
#endif

        /// === Monitor ===

        private static object _locker; // объект заглушка

        /// <summary>
        /// Фактически конструкция оператора lock инкапсулирует в сибе синтаксис использования мониторов. 
        /// Эквивалентный пример с монитором:
        /// </summary>
        private static void Sample4_CriticalSection_Monitor()
        {
            _globalCounter = 0;
            _locker = new object();

            for (int i = 0; i < 5; i++)
            {
                Thread thread = new Thread(Print_Monitor);
                thread.Name = $"Поток №{i}";
                thread.Start();
            }
        }

        private static void Print_Monitor()
        {
            bool acquiredLock = false; // блокируем?

            try
            {
                Monitor.Enter(_locker, ref acquiredLock); // начало критической секции

                for (int i = 0; i < 5; i++)
                {
                    Console.WriteLine($"{Thread.CurrentThread.Name}: {_globalCounter++}");
                    Thread.Sleep(100);
                }
            }
            finally
            {
                if(acquiredLock) // если заняли - освободи
                    Monitor.Exit(_locker);// окончание критичекской секции
            }
        }
        #endregion

        #region === Examples of volatile Fields ===
        /// <summary>
        /// Ключевое слово volatile означает, что поле может изменить несколько потоков, выполняемых одновременно. 
        /// Поле, которое отмечено volatile исключается из некоторых типов оптимизации. 
        /// </summary>
        private static void Sample5_Volatile()
        {
            Worker worker = new Worker();
            Thread workerThread = new Thread(worker.DoWork);

            workerThread.Start();
            Console.WriteLine("Main Thread: Начало workerThread ...");

            while (!workerThread.IsAlive) ;
            
            Thread.Sleep(500);
            worker.RequesStop();

            workerThread.Join();

            Console.WriteLine("Main Thread: workerThread завершен!");
        }

        /// <summary>
        /// Пример использования переменной volatile
        /// </summary>
        private class VolatileTest
        {
            public volatile int SharedStorage;

            public VolatileTest(int num)
            {
                SharedStorage = num;
            }
        }

        /// <summary>
        /// Пример использования переменной volatile
        /// </summary>
        private class Worker
        {
            private volatile bool _shouldStop;

            public void DoWork()
            {
                bool isWork = false;
                while (!_shouldStop)
                {
                    isWork = !isWork; // симуляция работы
                }

                Console.WriteLine($"Worker.DoWork ({Thread.CurrentThread.Name}): завершил работу!");
            }

            public void RequesStop()
            {
                _shouldStop = true;
            }
        }

        #endregion
    }
}





