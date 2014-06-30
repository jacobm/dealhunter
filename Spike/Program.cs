using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace Spike
{
    class Program
    {
        static void Main(string[] args)
        {
            var connection = new NpgsqlConnection("User ID=scraper;Password=dingo;Host=localhost;Port=5432;Database=scrape;Pooling=true;");
        }
    }
}
