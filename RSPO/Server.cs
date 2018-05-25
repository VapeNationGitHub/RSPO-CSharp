using System;
using System.Collections.Generic;
using Nancy;
using SharpTAL;
using System.Linq.Expressions;
using System.IO;
using BrightstarDB.EntityFramework;
using System.Linq; // Этого не хватало.
using System.Globalization;
using System.Net;
using System.Text;

namespace RSPO
{
    public class WebModule : NancyModule
    {
        private CookieContainer cookies;

        public WebModule() : base()
        {
            Get["/"] = parameters =>
            {
                ApplicationModel appModel = new ApplicationModel(Application.APPLICATION_NAME);
                return Render("index.pt", context: appModel, view: new ApplicationView(appModel));

            };

            Get["/objs"] = parameters => // Это страница сайта с квартирами.
            {
                ObjectList objList = new ObjectList();
                // Надо отлаживать в монодевелоп...
                ObjectListView objView = new ObjectListView(objList);
                return Render("objlist.pt", context: objList, view: objView);
            };

            Get["/offers"] = parameters =>
            {
                OfferList model = new OfferList();
                OfferListView view = new OfferListView(model);
                return Render("offerlist.pt", context: model, view: view);
            };

            Get["/agents"] = parameters =>
            {
                AgentList model = new AgentList();
                AgentListView view = new AgentListView(model);
                return Render("agentlist.pt", context: model, view: view);
            }; // Why it is emplty???

            Get["/hello/{Name}"] = parameters =>
            {
                IAgent testModel = Application.Context.Agents.Create();
                testModel.Name = parameters.Name;
                return View["hello.html", testModel];
            };
        }

        public static void Main(string[] args)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8888/offers");
            request.CookieContainer = new CookieContainer();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            foreach (Cookie cook in response.Cookies)
            {
                Console.WriteLine("Cookie:");
                Console.WriteLine("{0} = {1}", cook.Name, cook.Value);
                Console.WriteLine("Domain: {0}", cook.Domain);
                Console.WriteLine("Path: {0}", cook.Path);
                Console.WriteLine("Port: {0}", cook.Port);
                Console.WriteLine("Secure: {0}", cook.Secure);

                Console.WriteLine("When issued: {0}", cook.TimeStamp);
                Console.WriteLine("Expires: {0} (expired? {1})", cook.Expires, cook.Expired);
                Console.WriteLine("Don't save: {0}", cook.Discard);
                Console.WriteLine("Comment: {0}", cook.Comment);
                Console.WriteLine("Uri for comments: {0}", cook.CommentUri);
                Console.WriteLine("Version: RFC {0}", cook.Version == 1 ? "2109" : "2965");
                Console.WriteLine("String: {0}", cook.ToString());

                StreamWriter textFile = new StreamWriter(@"C:\test\textfile.txt");
                textFile.WriteLine(response.Cookies);
                textFile.Close();

                Console.ReadLine();
            }
        }

        
        private CookieContainer POST(string Url, string Data) //Возвращаемый тип не строка
        {
            WebRequest req = WebRequest.Create(Url);
            req.Method = "POST";
            req.Timeout = 100000;
            req.ContentType = "http://127.0.0.1:8888/offers";
            byte[] sentData = Encoding.GetEncoding(1251).GetBytes(Data);
            req.ContentLength = sentData.Length;
            Stream sendStream = req.GetRequestStream();
            sendStream.Write(sentData, 0, sentData.Length);
            sendStream.Close();
            var reqs = (HttpWebRequest)WebRequest.Create(Url);
            HttpWebResponse res = (HttpWebResponse)req.GetResponse();

            StreamWriter textFile = new StreamWriter(@"C:\test\textfile.txt");
            textFile.WriteLine(res);
            textFile.Close();

            return cookies;

        }
        


        public string Render(string templateFile,
							 object context = null,  // Model
							 Request request = null, // Request
							 object view = null)       // View
		{
			string templateString = "";
			Template template = null;

			bool gotCache = false;

			if (Application.USE_TEMPLATE_CACHE)
			{
				gotCache = Application.templateCache.TryGetValue(templateFile, out template);
			}

            // В режиме отладки иногда удобно, если при каждом запросе шаблон заново верстается.
            // Не надо сервак перезапускать при изменении шаблона.

			if (!gotCache)
			{
				string filePath = Path.Combine(Application.TEMPLATE_LOCATION, templateFile);
				string tempPath123 = Path.Combine(Application.TEMPLATE_LOCATION, "_!123!_").Replace("_!123!_", "");
				Console.WriteLine("Template Path:" + filePath);
				templateString = File.ReadAllText(filePath);
				templateString = templateString.Replace("@TEMPLATEDIR@", tempPath123);  // Подстановка местораположения шаблонов в текст шаблона.
				template = new Template(templateString);

                if (Application.USE_TEMPLATE_CACHE)
                {
                    Application.templateCache.Add(templateFile, template);
                }
			}

			var dict = new Dictionary<string, object>();

			if (request == null)
			{
				request = this.Request;
				dict.Add("request", request);
			}
			if (context != null)
			{
				dict.Add("model", context);
			}

			dict.Add("view", view);

			return template.Render(dict);
		}
	}

	public partial class Application
	{
		public static Dictionary<string, Template> templateCache = new Dictionary<string, Template>();
	} // TODO: Навожу порядок в monodevelop...

	// Это общая часть машей надстройки (микрофрейморк MVVM - MVC)
	// Это у нас абстрация модели. Здесь мы работаем с объектами, получаемыми из базы данных
	public interface IObjectList<T> where T : class // Интерфес списков объектов недвижимости
	{
		int Size { get; }                      // колиество объектов
		ICollection<T> Objects { get; }       // список объектов
		void SetFilter(Func<T, bool> filter);// Условие формирования списка.
		void SetLimits(int start, int size); // Диапазон объектов для вывода на экран
		void Update();                       // Обновить список согласно условиям.
	}

	public class EntityList<T> : IObjectList<T> where T : class
		// Это список сущностей. Реализовано при помои обобщенного программирования.
		// <T>. Вместо T подставляется тип или интерфейс элемента.
	{
		public static int DEFAULT_SIZE = 50; // 50 из 5200 прим.
		private Func<T, bool> filter = null;
		private int start = 0, size = DEFAULT_SIZE;

		public EntityList(Func<T, bool> filter = null, bool update = true)
		{
			SetFilter(filter);
			if (update) Update();
		}

		public class BadQueryException : Exception
		{
			public BadQueryException(string msg) : base(msg) { }
		}

		public ICollection<T> Objects
		{
			get
			{
				ICollection<T> res = objectQuery.Skip(start).Take(size).ToList();
				if (res == null) throw new BadQueryException("query returned null"); // Для отслаживания плохих запросов
				return res; // Хотя мне не нравиться так.... но посмотрим.
			}
		}

		private IEnumerable<T> objectQuery = null;

		public int Size
		{
			get
			{
				return objectQuery.Count();
			}
		}

		public void SetFilter(Func<T, bool> filter = null)
		{
			if (filter == null) filter = x => true; // Функция (лямбда) с одним аргументом x,
													// .... возвращает истину, т.е. все записи.
			this.filter = filter;
		}

		public void SetLimits(int start = 0, int size = 50)
		{
			this.start = start;
			this.size = size;
		}

		public void Update()
		{
			// Здесь сделаем запрос и присвоим objects список - результат запроса.
			MyEntityContext ctx = Application.Context;

			objectQuery = ctx.EntitySet<T>().Where(filter);
		}
	}

	public class View<T> where T : class // жесть прошла.
	{
		public string Title = "A Default page!";
		public T context;
		public View(T context)
		{
			this.context = context;
		}

		protected View() { }
	}


	public class EntityListView<T> : View<T> where T : class
	{
		public EntityListView(T context) : base(context)
		{

		}
	}
}
// 123 