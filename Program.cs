/*
 *Домашнее задание.
 *Источник: Microsoft Teams.
 *
 *Язык: C Sharp (C#) v7.3.
 *Среда: Microsoft Visual Studio 2019 v16.9.4.
 *Платформа: .NET Framework v4.7.2.
 *API: console.
 *Изменение: 08.05.2021.
 *Защита: 17.05.2021.
 *
 *Вариант: отсутствует.
 *Задание: написать службу Windows - Chat Server. Получает строки от клиента и рассылает их другим подключенным клиентам.
 *
 *Примечание:
 *1. Установка службы: C:\Windows\Microsoft.NET\Framework64\v4.x\>installutil.exe <путь к сборке>.
 *2. Включение-выключение службы: Диспетчер задач -> Службы -> ChatServerServies -> ПКМ -> Включить службу (или Выключить службу).
 *2. Удаление службы: C:\Windows\Microsoft.NET\Framework64\v4.x\>installutil.exe /u <имя сборки>.exe.
 *3. Дополнительная информация:
 *а) https://metanit.com/sharp/tutorial/21.2.php
 *б) https://metanit.com/sharp/tutorial/21.1.php
 *в) https://docs.microsoft.com/ru-ru/dotnet/framework/windows-services/walkthrough-creating-a-windows-service-application-in-the-component-designer
 *г) https://docs.microsoft.com/ru-ru/dotnet/framework/windows-services/introduction-to-windows-service-applications
 *д) https://habr.com/ru/company/pc-administrator/blog/421019/
 *4. Диспетчер управления службами (от англ. Service Control Manager, SCM) - системный процесс, который отвечает за взаимодействие со
 *   всеми службами в ОС Windows.
 *5. Сокетом (от англ. Socket – разъем) называется WinAPI для обеспечения обмена данными между процессами.
 *   Процессы при таком обмене могут исполняться как на одном компьютере, так и на разных с помощью сети. Сокет является абстрактным
 *   объектом, представляющим из себя конечную точку соединения (IP-адрес и порт).
 *6. ref - тип данных. Передача по ссылке - по адресу переменной в памяти (передается сама переменная, а не ее копия).
 *7. enum - тип данных перечисление (набор логически связанных констант, аналог структуры). При не явной инициализации константам
 *   присваиваются значения из диапазона [0, +oo] с шагом +1 (поддерживаются только целочисленные типы: byte, int, short, long). При
 *   явной инициализации начинаться может с любого числа с любым шагом.
 *   Передача по значению (копия переменной).
 *8. [StructLayout] - определяет тип данных переменных из структуры ServiceStatus.
 *9. => - лямбда-оператор используется для отделения входных параметров с левой стороны от тела лямбда-выражения с правой стороны.
 *10. if (clients.Where (x => x.Client.RemoteEndPoint.ToString () == client.Client.RemoteEndPoint.ToString ()).FirstOrDefault () ==
 *   null) - следует читать так:
 *а) x.Client.RemoteEndPoint - создается экземпляр класса TcpClient x, с конечной точкой по умолчанию.
 *б) x.Client.RemoteEndPoint == client.Client.RemoteEndPoint - если форматы конечных точек x и ожидающего TCP-клиента одинаковы, то
 *   присваиваем эту строку экземпляру класса x (которая находится слева от лямбда-оператора).
 *в) clients.Where (x).FirstOrDefault () == null - сравнение конечной точки ожидающего TCP-клиента со всем списком clients. Если
 *   конечная точка (IP-адрес и порт) в списке clients не встречается (null означает не инициализировано, а не нуль), то условие
 *   if выполняется.
 */

using System.ServiceProcess;

namespace ChatServer
{

	static class Program
	{

		/// <summary>
		/// Главная точка входа для приложения.
		/// </summary>
		static void Main ()
		{
			ServiceBase [] ServicesToRun;

            // Инициализация массива базовых классов ServicesToRun (можно запускать нескольких служб).
			ServicesToRun = new ServiceBase []
			{
				new ChatServerService ()
			};

            // Регистрация исполняемого файла ServicesToRun для службы с помощью SCM.
			ServiceBase.Run (ServicesToRun);
		}
	}
}
