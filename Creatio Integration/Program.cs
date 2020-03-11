using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Creatio_Integration
{
    class Program
    {
        static void Main(string[] args)
        {
            string userName = "Supervisor";
            string userPassword = "Supervisor";
            string url = "http://localhost:8082";

            CreatioIntegrationHelper creatio = new CreatioIntegrationHelper(userName, userPassword, url);

            string endpoint = $"{url}/0/rest/PeriodicalEditionWebService/GetTotalPlannedIssues";
            var request = creatio.Request("POST", endpoint, new { 
                Code = "123"
            });

            Console.WriteLine("Response: ");
            Console.WriteLine("Success: {0}", request.Success);
            Console.WriteLine("Status Code: {0}", request.StatusCode);
            Console.WriteLine("Error: {0}", request.Error);
            Console.WriteLine("Body: {0}", request.ResponseBody);
            Console.WriteLine();
            Console.WriteLine("Header: {0}", request.ResponseHeaders);
            Console.WriteLine();
            Console.WriteLine("Cookies: {0}", request.ResponseCookies);
        }
    }
}
