using System;
using System.Collections.Generic;                                                      // Требуется для класса List. //
using System.Linq;                                                                     // Требуется для функции Where (класса List). //
using System.Net;                                                                      // Требуется для класса IPAddress. //
using System.Net.Sockets;                                                              // Требуется для классов TcpClient и TcpListener. //
using System.Runtime.InteropServices;                                                  // Требуется для классов StructLayout, LayoutKind, DllImport и SetLastError. //
using System.ServiceProcess;                                                           // Требуется для базового класса ServiceBase. //
using System.Threading.Tasks;                                                          // Требуется для класса Task. //

namespace ChatServer
{

    // Перечисление ServiceState необходимо для инициализации объекта
    // dwCurrentState (в структуре ServiceStatus).
    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,                                                  // 0x00000001 = 0x1 - код присваиваемый, каждой операции (присваиваются по умолчанию). //
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    // Структура ServiceStatus определяет состояние службы.
    [StructLayout (LayoutKind.Sequential)]                                             // Sequential - поля структуры располагаются последовательно (в том порядке, в котором они экспортируются в память). //
    public struct ServiceStatus
    {
        public int dwServiceType;                                                      // Используется самой службой. //
        public ServiceState dwCurrentState;                                            // Объявление перечисления ServiceState. //
        public int dwControlsAccepted;                                                 // Используется самой службой. //
        public int dwWin32ExitCode;                                                    // Используется самой службой. //
        public int dwServiceSpecificExitCode;                                          // Используется самой службой. //
        public int dwCheckPoint;                                                       // Используется самой службой. Контрольная точка (служба его периодически увеличивает на +1, чтобы сообщить о своем прогрессе во время длительного запуска, остановки, паузы или продолжения работы). //
        public int dwWaitHint;                                                         // Пауза (в милисекундах, по умолчанию 30 сек). Дается для того чтобы служба выполнила другую оперцию (отложенного запуска, остановки, приостановки или продолжения). Если dwCheckPoint в этот период не увеличится, то ОС считает, что произошла ошибка и закроет службу. //
    };

    public partial class ChatServerService : ServiceBase
    {

        private Task serverTask;                                                       // Объект serverTask будет выполняться async. //
        private bool work;                                                             // Флаг работы программы. //
        private const int MaxBufferLenght = 10000;                                     // Буфер сообщения до 10000 байт. //
        private const int MaxClientNumber = 20;                                        // Количество подключеннных клиентов (до 20 шт.). //

        // Изменение статуса службы в "Диспетчере задач".
        [DllImport ("advapi32.dll", SetLastError = true)]                              // advapi32.dll - Win32 API. //
        private static extern bool SetServiceStatus (IntPtr handle, ref ServiceStatus serviceStatus);     // Статус определяется с помощью структуры ServiceStatus. //

        // Инициализация конструктора по умолчанию.
        public ChatServerService ()
        {
            InitializeComponent ();
        }

        // Основной блок программы.
        private void ServerWork ()
        {
            List <TcpClient> clients = new List <TcpClient> ();                        // Объявление списка clients - TCP-клиентов. //
            TcpListener listener = new TcpListener (IPAddress.Any, 1366);              // Объявление объекта listener для прослушивания (весь диапазон IP-адресов, порт 1366). //
            try
            {
                listener.Start ();                                                     // Начало прослушивание порта 1366 на сервере. //

                while (work)                                                           // В случае work == false (в функции OnStop), SCM останавливает работу службы. //
                {

                    // Проверка TCP-клиента ожидающего подключение.
                    while (listener.Pending () && clients.Count < MaxClientNumber)     // Pending - ожидающий подключение, количество подключенных TCP-клиентов не превышает 20 шт. //
                    {

                        // Подключение ожидающего клиента (async).
                        var client = listener.AcceptTcpClientAsync ().Result;

                        // Проверка конечной точки вновь подключенного TCP-клиента.
                        if (clients.Where (x => x.Client.RemoteEndPoint.ToString () ==
                            client.Client.RemoteEndPoint.ToString ()).FirstOrDefault () == null)
                            clients.Add (client);                                      // Если TCP-клиент в списке clients не обнаружен, то добавляется в список. //
                        else
                            client.Client.Disconnect (false);                          // Иначе отсодинение ожидающего TCP-клиента с запретом на повторное использование сокета (конечной точки). //
                    }

                    // Обмен сообщениями между сервером и подключенными TCP-клиентами
                    // (по очередно).
                    for (int i = clients.Count - 1; i >= 0; i--)
                    {
                        var client = clients [i];                                      // Подключение TCP-клиента с индексом из списка clients. //

                        if (client.Connected)
                        {
                            byte [] buffer = new byte [MaxBufferLenght];
                            int size = 0;

                            // Если TCP-клиент отправил сообщение.
                            while (client.Available > 0)                               // Количество байт сообщения больше нуля. //
                            {
                                var lenght = Math.Min (1024, client.Available);
                                size += Math.Min (1024, client.Available);

                                // Проверка на переполнение буфера сообщений.
                                if (size > MaxBufferLenght)
                                {
                                    clients.RemoveAt (i);                              // Если буфер переполнен, то TCP-клиент удаляется из списка clients. //
                                    client.Client.Disconnect (false);                  // TCP-клиенту запрещается повторное использование сокета (конечной точки). //
                                }
                                client.GetStream ().Read (buffer, size, lenght);       // Иначе, фрагмент полученного сообщения копируется в буфер buffer. //
                            }

                            // Если буфер не переполнен, то отправляем  сообщение всем
                            // остальным подключенным TCP-клиентам.
                            if (size < MaxBufferLenght)
                            {

                                foreach (var toClient in clients)                      // Поиск каждого подключенного TCP-клиента из списка clients. //
                                {

                                    if (client != toClient)                            // Если найденный TCP-клиент не является TCP-клиентом-отправителем сообщения (текущим подключенным TCP-клиентом). //
                                    {
                                        var sended = 0;

                                        while (sended < size)                          // То, ему отправляется полученное сообщение от TCP-клиента-отправителя (текущего подключеннего TCP-клиента). //
                                        {
                                            var lenght = Math.Min (1024, size - sended);
                                            toClient.GetStream ().Write (buffer, sended, lenght);
                                            sended += lenght;
                                        }
                                    }
                                }
                            }
                        }
                        else
                            clients.RemoveAt (i);                                      // Если найденный TCP-клиент из списка clients не подключен, то он из списка clients удаляется. //
                    }
                }

                listener.Stop ();                                                      // Если work == false, то прослушиевание порта останавливается. //
                foreach (var client in clients)                                        // И все TCP-клиенты из списка clients отсоединяютс с запретом на повторное использование сокета (конечной точки). //
                    client.Client.Disconnect (false);     
            }

            catch (Exception ex)
            {
                Console.WriteLine (ex.ToString ());
            }

            finally
            {
                Console.ReadLine ();
            }
        }

        // Запуск. Точка входа в службу.
        protected override void OnStart (string [] args)
        {

            // Механизм опроса (запуск службы).
            ServiceStatus serviceStatus = new ServiceStatus ();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;         // Полю dwCurrentState присваивается код 0x2 (ожидание запуска). //
            serviceStatus.dwWaitHint = 100000;                                         // Пауза 100 сек. //
            work = true;
            serverTask = Task.Run (() => ServerWork ());                               // Async объект serverTask вызывает функцию ServerWork. //

            // Механизм опроса (служба запущена).
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;               // Полю dwCurrentState присваивается код 0x4. //
            SetServiceStatus (this.ServiceHandle, ref serviceStatus);                  // Передача статуса службы (работает) в Win32 API (advapi32.dll). //
        }

        // Остановка.
        protected override void OnStop ()
        {

            // Механизм опроса (служба останавливается).
            ServiceStatus serviceStatus = new ServiceStatus ();

            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;          // Полю dwCurrentState присваивается код 0x3 (ожидание остановки). //
            serviceStatus.dwWaitHint = 100000;                                         // Пауза 100 сек. //

            SetServiceStatus (this.ServiceHandle, ref serviceStatus);                  // Передача статуса службы (ожидание остановки) в Win32 API. //
            work = false;
            serverTask.Wait ();                                                        // Ожидание async объект serverTask. //

            // Механизм опроса (служба остановлена).
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;               // Полю dwCurrentState присваивается код 0x1 (остановлен). //
            SetServiceStatus (this.ServiceHandle, ref serviceStatus);                  // Передача статуса службы (остановлена) в Win32 API. //
        }

        // Возобновление после приостановки.
        protected override void OnContinue ()
        {
            OnStart (new string [0]);
        }
    }
}