using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace HelloWorldFunction
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            /*string name = req.Query["name"];*/
            /*string test = req.Query["test"];*/

            string firstVal = req.Query["firstVal"];
            string secondVal = req.Query["secondVal"];


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            /*name = name ?? data?.name;*/
            /*test = test ?? data?.test;*/

            // ?? returns the value to the left if its not null - Otherwise the value to the right of the double ternary operator is evaluated
            firstVal = firstVal ?? data?.firstVal;
            secondVal = secondVal ?? data?.secondVal;

            

            /*string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}.";

            if (!string.IsNullOrEmpty(test))
            {
                responseMessage += $"This second part is a test of the {test}";
            }*/

            int responseMessage = Int16.Parse(firstVal) + Int16.Parse(secondVal);


            return new OkObjectResult(responseMessage);
        }
    }
}
