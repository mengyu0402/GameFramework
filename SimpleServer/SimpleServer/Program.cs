﻿using SimpleServer.Services;
            
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace SimpleServer
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			Service.Run();
		}
	}
}
