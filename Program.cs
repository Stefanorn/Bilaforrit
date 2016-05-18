using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using System.Net;

using Microsoft.Win32;
using System.Net.Mail;

namespace Bilakaupaforrit
{
    public class CarInfo
    {
        public string carName { get; set; }
        public string URLToCar { get; set; }
        public string Engine { get; set; }
        public int kilometers { get; set; }
        public int year { get; set; }
        public int price { get; set; }
    }

    class Program
    {
        public static int connectionCounter = 0;
        public static List<string> siteID = new List<string>();

        public static List<CarInfo> CarDataBase = new List<CarInfo>();

        static void Main(string[] args)
        {

            int priceRange = 600000;
            int drivenYearLimit = 17000;
            int ageLimit = 2005;


            GetDataFromBland(priceRange);

            while (!ReadDataFromFile())
            {
                System.Threading.Thread.Sleep(50);
            }
            Console.WriteLine("Búin að lesa úr skrá");

            foreach (string item in siteID)
            {
                for (int i = 0; i < 15; i++)
                {
                    GetDataFromWebBilasolur(@"http://www.bilasolur.is/SearchResults.aspx?page=" + (i + 1) + "&id=" + item);
                }
            }

            while (!connectToBland)
            {
                System.Threading.Thread.Sleep(150);
            }
            Console.WriteLine("Búinn að tengjast Bland.is");

            while (siteID.Count * 15 != connectionCounter)
            {
                System.Threading.Thread.Sleep(150);
            }
            Console.WriteLine("Búinn að tengjast bilasolur.is");

            while (openThreadCounter != closeThreadCounter)
            {
                System.Threading.Thread.Sleep(150);

            }
            System.Threading.Thread.Sleep(150);
            Console.WriteLine("Búin að sækja upplýsingar um bíla á bland.is " + openThreadCounter +" / "+ closeThreadCounter);
            List<CarInfo> GoodCars = new List<CarInfo>();

            int sdfi = CarDataBase.Count;
            
            foreach (CarInfo car in CarDataBase)
            {
                if (car.year >= ageLimit)
                {
                    if (car.price <= priceRange && car.price >= 10000)
                    {
                        int drivenPerYear = car.kilometers / (DateTime.Now.Year - ageLimit);
                        if (drivenPerYear <= drivenYearLimit)
                        {
                            Console.WriteLine("Nafn Bíls " + car.carName + " Bíil árgerð " + car.year + " verð " + car.price + " keyrður á ári " + drivenPerYear );
                            GoodCars.Add(car);
                        }
                    }
                }
            }


            //Láta forritið sækja frá fleirri síðum á bland

            //Finna út hvernig ég læt forritið bíða eftir að það sé búið að ná í allt frá bland

            //Senda inn driven by year limit og age limit yfir á bland
            //

            // Finna út hvernig ég læt forritið keyta sem bacground þjónustu




            // Hugsamlega gera annað forrit sem kallar á þetta forrit á enhverjum gefnum tíma, 
            // láta þá þettaforrit bara lokast þegar það er búið að keyra. 


            // Láta Forritið sækja frá bland.is

            Console.Read();
        }

        static bool ReadDataFromFile()
        {
            try
            {
                using (StreamReader sr = new StreamReader(@"C:\Bíladót\siteID.txt"))
                {
                    while (!sr.EndOfStream)
                    {
                        siteID.Add(sr.ReadLine());
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return true;
        }

        async static void GetDataFromWebBilasolur(string url)
        {

            using (HttpClient client = new HttpClient())
            {

                using (HttpResponseMessage resp = await client.GetAsync(url))
                {
                    using (HttpContent content = resp.Content)
                    {
                        string answare = await content.ReadAsStringAsync();
                        connectionCounter++;
                        CaptureCarsAndAddToDataBase(answare);
                    }
                }
            }
        }



        private static bool connectToBland = false;
        private static int openThreadCounter = 0;
        async static void GetDataFromBland(int maxPrice)
        {
            using (HttpClient client = new HttpClient())
            {
                for (int i = 1; i < 10; i++)
                {
                    using (HttpResponseMessage resp = await client.GetAsync("https://bland.is/classified/?categoryId=17&sub="+i))
                    {
                        using (HttpContent content = resp.Content)
                        {
                            string answare = await content.ReadAsStringAsync();

                            MatchCollection mc = Regex.Matches(answare, @"a href=..til-solu.farartaeki.bilar(.*?)(\d{7,10})(?:.|\n)*?<div class=.priceTime.>(?:.|\n)*?(.*)<.p>");

                            foreach (Match m in mc)
                            {
                                int price;
                                bool result = int.TryParse(Regex.Replace(m.Groups[3].ToString(), @"\D", ""), out price);

                                if (!result)
                                {
                                    break;
                                }

                                if (price < maxPrice)
                                {
                                    openThreadCounter++;
                                    CarInfo tempCar = new CarInfo();
                                    tempCar.carName = Regex.Replace(m.Groups[1].ToString(), @"\W", " ");
                                    tempCar.price = price;
                                    tempCar.URLToCar = "https://bland.is/til-solu/farartaeki/bilar/" + m.Groups[2].ToString();
                                    CaptureCarsFromBland(tempCar.URLToCar, tempCar);
                                }
                            }
                        }
                    }
                }
            }
            connectToBland = true;
        }

        private static int closeThreadCounter = 0;
        async private static void CaptureCarsFromBland(string link, CarInfo tempCar)
        {
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage resp = await client.GetAsync(link))
                {
                    using (HttpContent content = resp.Content)
                    {
                        string htmlContent = await content.ReadAsStringAsync();

                        string yearPattern = @"Ár<.td>(?:\n|.)*?<td>(\d{4})";
                        string drivenPatern = @"Akstur<.td>(?:\n|.)*?<td>(\d{1,3})";

                                              

                        Match m = Regex.Match(htmlContent, yearPattern, RegexOptions.None, new TimeSpan(0,0,3));

                        if (m.Success)
                        {
                            tempCar.year += int.Parse(m.Groups[1].ToString());
                        }

                        m = Regex.Match(htmlContent, drivenPatern);
                        if (m.Success)
                        {
                            tempCar.kilometers += int.Parse(m.Groups[1].ToString()) * 1000;
                        }
                        CarDataBase.Add(tempCar);
                    }
                }
            }
            closeThreadCounter++;
        }

        static void CaptureCarsAndAddToDataBase(string htmlData)
        {
            string pattern = @"<a href=.(CarDetails\.aspx.*?)>.*?.carmake.>(\w*)<\/span>(.*?)<\/a><\/div>.*?Verð <b>(\d{1,4}).*?<.b>.*?Árgerð <b>(\d{4}).*?Akstur <b>(\d{1,4}).*?<.b>";

            MatchCollection mc = Regex.Matches(htmlData, pattern, RegexOptions.None, new TimeSpan(0,0,3));
            foreach (Match m in mc)
            {
                CarInfo tempData = new CarInfo();
                tempData.carName = m.Groups[2] + " " + m.Groups[3];
                tempData.kilometers = int.Parse(m.Groups[6].ToString()) * 1000;
                tempData.year = int.Parse(m.Groups[5].ToString());
                tempData.price = int.Parse(m.Groups[4].ToString()) * 1000;
                Regex.Replace(m.Groups[1].ToString(), @"amp;|\\", "");
                tempData.URLToCar = "http://www.bilasolur.is/" + m.Groups[1].ToString();
                CarDataBase.Add(tempData);
            }
        }


        //Ekki í notkun
        private static string getExternalIp()
        {
            try
            {
                string externalIP;
                externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
                externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}"))
                             .Matches(externalIP)[0].ToString();
                return externalIP;
            }
            catch { return null; }
        }
        static void SendEmail()
        {
            string from = "lokoko@nothotmoili.com";
            string to = "stefanorn92@gmail.com";
            string subject = "Bílar sem ég hef áhuga á að skoða";
            string message = "";
            MailMessage mail = new MailMessage(from, to, subject, message);
            mail.IsBodyHtml = true;

            SmtpClient client = new SmtpClient();
            client.Port = 25;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.UseDefaultCredentials = false;
            client.Host = getExternalIp();
            client.Timeout = 5000;
            try
            {
                client.Send(mail);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            Console.Read();
            Console.WriteLine("Tölvupóstur var sendur");
        }
    }
}
