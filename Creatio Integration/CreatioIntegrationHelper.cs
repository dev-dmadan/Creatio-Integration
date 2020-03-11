using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace Creatio_Integration
{
    class CreatioIntegrationHelper
    {
        static HttpClient client = new HttpClient();
		CookieContainer cookies = new CookieContainer();
		private string BPMCSRF = String.Empty;
		private string CookiesFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "cookies.dat");
		private int LoginAttempts = 3;

		private string UserName_;
		protected string UserName
		{
			get
			{
				return UserName_ ?? "Supervisor";
			}
		}

		private string UserPassword_;
		protected string UserPassword
		{
			get
			{
				return UserPassword_ ?? "Supervisor";
			}
		}
		private string CreatioUrl_;
		protected string CreatioUrl
		{
			get
			{
				return CreatioUrl_;
			}
		}

		public CreatioIntegrationHelper(string userName, string userPassword, string creatioUrl)
		{
			UserName_ = userName;
			UserPassword_ = userPassword;
			CreatioUrl_ = creatioUrl;

			Console.WriteLine("Creatio Integration Running...");
		}

		public HttpClientResponse Request(string Method, string Url, dynamic Data = null, Dictionary<string, string> Headers = null)
		{
			HttpClientResponse result = new HttpClientResponse();
			int attempts = 0;

			Console.WriteLine("Request running...");
			while (attempts < LoginAttempts)
			{
				ReadCookies();
				Console.WriteLine("Request Attempt: {0}", attempts+1);

				var Headers_ = new Dictionary<string, string>() {
					{ "BPMCSRF", BPMCSRF }
				};
				Dictionary<string, string> temp_ = new Dictionary<string, string>();
				if (Headers != null && Headers.Count > 0)
				{
					temp_ = Headers_.Concat(Headers).GroupBy(d => d.Key).ToDictionary(d => d.Key, d => d.First().Value);
				}

				var response = HttpClientRequest(Method, Url, Data, (temp_.Count > 0 ? temp_ : Headers_)).GetAwaiter().GetResult();
				if (!response.IsSuccessStatusCode)
				{
					attempts++;
					login();
				}
				else
				{
					WriteCookies();
					result = response;
					break;
				}
			}

			return result;
		}

		private void login()
		{
			Console.WriteLine("Login running...");

			string uri = $"{CreatioUrl}/ServiceModel/AuthService.svc/Login";
			var data = new {
				UserName = UserName,
				UserPassword = UserPassword
			};
			var response = HttpClientRequest("POST", uri, data, 
				new Dictionary<string, string> {
					{"Accept", "application/json"}
				}
			).GetAwaiter().GetResult();

			WriteCookies();
		}

		private async Task<HttpClientResponse> HttpClientRequest(string Method, string Url, dynamic Data = null, Dictionary<string, string> Headers = null)
		{
			var uri = new Uri(Url);
			HttpClientHandler handler = new HttpClientHandler();
			handler.CookieContainer = cookies;

			var response = new HttpClientResponse();
			HttpResponseMessage result = null;
			client = new HttpClient(handler);
			client.BaseAddress = uri;

			string dataPostString = JsonConvert.SerializeObject(Data);
			HttpContent dataPost = new StringContent(dataPostString, Encoding.UTF8, "application/json");

			if (Headers != null && Headers.Count > 0)
			{
				foreach(var header in Headers)
				{
					if(header.Key.ToLower() == "accept")
					{
						client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(header.Value));
					}
					else
					{
						client.DefaultRequestHeaders.Add(header.Key, header.Value);
					}
				}
			}

			try
			{
				switch (Method.ToLower())
				{
					case "get":
						result = await client.GetAsync(Url);
						break;

					case "post":
						result = await client.PostAsync(Url, dataPost);
						break;

					case "put":
						result = await client.PutAsync(Url, dataPost);
						break;

					case "delete":
						result = await client.DeleteAsync(Url);
						break;

					default:
						break;
				}

				response.ResponseBody = await result.Content.ReadAsStringAsync();
				response.ResponseCookies = cookies.GetCookies(uri).Cast<Cookie>();
				response.ResponseHeaders = result.Headers;
				response.StatusCode = result.StatusCode.ToString();
				response.IsSuccessStatusCode = result.IsSuccessStatusCode;
				response.Success = true;
			}
			catch (HttpRequestException e)
			{
				response.Error = e.Message;
			}

			return response;
		}

		private void ReadCookies()
		{
			Console.WriteLine("Read Cookies from file...");

			try
			{
				using (Stream stream = File.Open(CookiesFilePath, FileMode.Open))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					cookies = (CookieContainer)formatter.Deserialize(stream);
					CookieCollection cookieCollection = cookies.GetCookies(new Uri($"{CreatioUrl}/ServiceModel/AuthService.svc/Login"));
					BPMCSRF = cookieCollection["BPMCSRF"].Value;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Read Cookies failed: {0}", e.Message);
			}
		}

		private void WriteCookies()
		{
			Console.WriteLine("Write Cookies to file...");

			using (Stream stream = File.Create(CookiesFilePath))
			{
				try
				{
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(stream, cookies);
				}
				catch (Exception e)
				{
					Console.WriteLine("Write cookies failed: {0}", e.Message);
				}
			}
		}
	}

	class HttpClientResponse
	{
		public bool Success { get; set; }
		public bool IsSuccessStatusCode { get; set; }
		public string Error { get; set; }
		public string StatusCode { get; set; }
		public string ResponseBody { get; set; }
		public IEnumerable<Cookie> ResponseCookies { get; set; }
		public HttpResponseHeaders ResponseHeaders { get; set; }
	}
}
