using System;
using Polly;

namespace PollyDemo
{
    public class BulkheadDemo
    {
        public static BulkheadDemo Default = new BulkheadDemo();

        private void Invoke()
        {
        }

        public void Run()
        {
            var policy = Policy.Bulkhead(2, context =>
            {
                Console.WriteLine("It's Bulkhead, throw BulkheadRejectedException.");
            });
            try
            {
                Console.WriteLine(DateTime.Now.ToString() + "-Run BulkheadDemo...");
                policy.Execute(Invoke);
            }
            catch (Exception ex)
            {
                Console.WriteLine(DateTime.Now.ToString() + "-Run BulkheadDemo" + ": Exception->" + ex.Message);
            }
        }
    }
}
