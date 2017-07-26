using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Poller
{
    class Program
    {
        static void Main(string[] args)
        {
            // AUTH_SESSION_ID & JSESSIONID obtained by logging into the RCMP's GCKEY site
            var AUTH_SESSION_ID = "";
            var JSESSIONID = "";
            // Your Twilio Account SID
            var accountSid = "";
            // Your Auth Token from twilio.com/console
            var authToken = "";
            // Your Twilio Phone Number (From Number) include +1 and area code (ie +14161234123)
            var FromNumber = "";
            // Phone number to text, include +1 and area code (ie +14161231234)
            var ToNumber = "";
            //Interval to refresh at in ms
            var interval = 240000;
            // Interval randomization value (maximum) this is added to above interval (between 0 and this)
            var intervalRnd = 120000;
            // Application search fields
            var surName = "";
            var givenName = "";
            var dateOfBirth = ""; // yyyy-mm-dd
            var cityOrPassword = "";




            Random rnd1 = new Random();
            TwilioClient.Init(accountSid, authToken);
            DateTime lastSMS = DateTime.Now;
            bool status = false;
            while (!status)
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("keyedSurName", surName),
                    new KeyValuePair<string, string>("keyedGivenName", givenName),
                    new KeyValuePair<string, string>("keyedDateOfBirth", dateOfBirth),
                    new KeyValuePair<string, string>("keyedPassword", cityOrPassword),
                    new KeyValuePair<string, string>("confirmTermsCheckbox", "on"),
                    new KeyValuePair<string, string>("continue", "Continue")
                });
                var cookieContainer = new CookieContainer();
                using (var handler = new HttpClientHandler() {CookieContainer = cookieContainer})
                {
                    var httpClient = new HttpClient(handler);
                    httpClient.BaseAddress = new Uri("https://www.services.rcmp-grc.gc.ca/");
                    cookieContainer.Add(httpClient.BaseAddress, new Cookie("ipeReferer", "eCFISIWS"));
                    cookieContainer.Add(httpClient.BaseAddress,
                        new Cookie("AUTH_SESSION_ID", AUTH_SESSION_ID));
                    cookieContainer.Add(httpClient.BaseAddress, new Cookie("_gc_lang", "eng"));
                    cookieContainer.Add(httpClient.BaseAddress,
                        new Cookie("JSESSIONID", JSESSIONID));
                    cookieContainer.Add(httpClient.BaseAddress, new Cookie("arp_scroll_position", "511"));
                    httpClient.DefaultRequestHeaders.Add("Referer",
                        "https://www.services.rcmp-grc.gc.ca/eCFISIWS/processNonLicenceEntry.do");
                    httpClient.DefaultRequestHeaders.Add("User-Agent",
                        "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/59.0.3071.115 Safari/537.36");
                    httpClient.DefaultRequestHeaders.Add("Origin", "https://www.services.rcmp-grc.gc.ca");

                    var postResult = httpClient.PostAsync("/eCFISIWS/processNonLicenceEntry.do", content);
                    postResult.Wait();
                    var postContent = postResult.Result.Content.ReadAsStringAsync();
                    postContent.Wait();
                    var postContentString = postContent.Result;

                    var doc = new HtmlDocument();
                    doc.LoadHtml(postContentString);
                    try
                    {
                        var form =
                            doc.DocumentNode.SelectNodes("//main//section//div//div//p")[2].InnerText.Trim()
                                .Replace("Current Status:\r\n\t\t\t\t\t\t", "");
                        System.Console.Clear();
                        System.Console.WriteLine(form);
                        System.Console.WriteLine("Last Update: " + DateTime.Now);
                        if (form !=
                            "Your application has completed initial processing.  It is now in progress.  Please check the status again at a later date.")
                        {

                            var message2 = MessageResource.Create(
                                to: new PhoneNumber(ToNumber),
                                from: new PhoneNumber(FromNumber),
                                body: "New Status at " + DateTime.Now + ": " + form);
                            status = true;
                            System.Console.WriteLine("Waiting for input - ");
                            System.Console.ReadKey();
                        }
                        if (DateTime.Now.Subtract(lastSMS).Hours >= 1)
                        {
                            var message2 = MessageResource.Create(
                            to: new PhoneNumber(ToNumber),
                            from: new PhoneNumber(FromNumber),
                            body: "In Progress " + DateTime.Now + ": " + form);
                            lastSMS = DateTime.Now;
                        }
                    }
                    catch
                    {
                        var message2 = MessageResource.Create(
                        to: new PhoneNumber(ToNumber),
                        from: new PhoneNumber(FromNumber),
                        body: "Session Expired @ " + DateTime.Now);
                        return;
                    }
                }

                int wait = interval + rnd1.Next(intervalRnd);
                System.Console.WriteLine("Next Refresh: "+DateTime.Now.Add(new TimeSpan(0,0,0,0,wait)));
                System.Threading.Thread.Sleep(wait);
                System.Console.Clear();
                System.Console.WriteLine("Refreshing...");
            }
        }
    }
}
