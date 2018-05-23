using System;
using System.IO;

namespace RSPO
{
	public partial class Application
	{
		// Global application configuration.
		static public string DESIGN_DIR = Path.Combine("design-studio_one-page-template", "build");
		static public string TEMPLATE_LOCATION = null;
		static public string STATIC_DIR = null;
		static public bool USE_TEMPLATE_CACHE = false; // Испоьовать кэш шабонов ?
		static string DB_CONNECTION_STRING = "type=embedded;storesdirectory=./;storename=RSPO";

        static public string APPLICATION_NAME = "Рекомендательная система по рынку недвижимости Иркутской области на основе технологий .NET";

		public static void InitializeEntityContext()
		{
			Context = new MyEntityContext(DB_CONNECTION_STRING);
		}


		public static void InitializeTemplating()
		{
			string basePath =
			Path.GetDirectoryName(
				Path.GetDirectoryName(
					Path.GetDirectoryName(
						Path.GetDirectoryName(
							System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase))));

			if (Type.GetType("Mono.Runtime") != null)
			{
				basePath = basePath.Replace("file:", "");
			}
			else
			{
				basePath = basePath.Replace("file:\\", "");
			}

			TEMPLATE_LOCATION = Path.Combine(basePath, DESIGN_DIR);
			Console.WriteLine("Templates are at " + TEMPLATE_LOCATION);
			STATIC_DIR = Path.Combine(TEMPLATE_LOCATION, "static");
		}
	}
}
