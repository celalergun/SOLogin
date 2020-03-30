using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SOLogin
{
    public class SOLoginWorker : BackgroundService
    {
        private readonly ILogger<SOLoginWorker> _logger;

        public SOLoginWorker(ILogger<SOLoginWorker> logger)
        {
            _logger = logger;
        }


        public void FeedBack(string s)
        {
            _logger.LogInformation($"{DateTime.Now.ToShortTimeString()} -> {s}");
        }

        Random rnd;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            FeedBack("Do not close this window");
            rnd = new Random();
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!await TryLoginAsync())
                {
                    // if we cannot login to the SO site, wait a little and try again
                    // sometimes SO goes to a maintenance mode
                    await Task.Delay(1000 + (rnd.Next(500, 1000)));
                    continue;
                }
                for (int i = 0; i < 8 /*hours*/ * 60 /*minutes*/ * 60 /*seconds*/; i++)
                {
                    // I don't know if there is a counter measure to detect the bots on SO site
                    // but I wanted to make sure the operation is not exactly periodic
                    // (Old habits die hard)
                    await Task.Delay(1000 + (rnd.Next(500, 1000)));

                    // we're not waiting in one "Task.Delay" call, 
                    // instead we wait a little bit and then check if we got a cancel request
                    if (stoppingToken.IsCancellationRequested)
                        break;
                    Console.Write(TimeSpan.FromSeconds(i) + "     \r");
                    if (Console.KeyAvailable)
                    {
                        var c = Console.ReadKey();
                        break;
                    }
                }
            }
            await Task.Run(() => Console.WriteLine("Bitti"));
            return;
        }

        private async Task<bool> TryLoginAsync()
        {
            BasicBrowser b = new BasicBrowser();
            b.Get("https://stackoverflow.com/users/login");
            b.FormElements["email"] = String20191223153459();
            b.FormElements["password"] = String20191223153606();
            string response = b.Post("https://stackoverflow.com/users/login");

            if (response.Contains("Are you a human being"))
            {
                FeedBack("Unable to login due to captcha request. Waiting...");
                await Task.Delay(60000);
                return false;
            }


            string profil = b.Get("https://stackoverflow.com/users/334690/celal-ergün?tab=profile");

            bool basarili = false;

            Regex r = new Regex("Last seen <(\"[^\"]*?\"|'[^']*?'|[^'\">])*>\\d+ \\w+ ago");
            var m = r.Match(profil);
            if (m.Success)
            {
                Console.WriteLine(m.Value);
                basarili = true;
            }


            Regex r0 = new Regex("Last seen just now");
            var m0 = r0.Match(profil);
            if (m0.Success)
            {
                Console.WriteLine(m0.Value);
                basarili = true;
            }
            return basarili;
        }
        private string String20191223153459()
        {
            return "youremail@whatever.com";
        }

        private string String20191223153606()
        {
            return "Password-2020";
        }
    }
}
