using OrderProcessing.Phase1;
using OrderProcessing.Phase1.Repositories;
using OrderProcessing.Phase2;
using OrderProcessing.Phase3;
using OrderProcessing.Phase4;
using OrderProcessing.Shared;
using System.Diagnostics;

public class Program
{

    public static async Task Main()
    {
        Console.WriteLine("Enter coice");
        string? choice = Console.ReadLine();
        switch (choice)
        {
            case "1":
                await new Phase1Runner().RunAsync();
              
                break;

            case "2":
                await new Phase2Runner().RunAsync();
               
                break;

            case "3":
                await Phase3Runner.RunComparisonAsync();
                break;

            case "4":
                await Phase4Runner.RunAllAsync();
                break;
        }
    }


      



}





