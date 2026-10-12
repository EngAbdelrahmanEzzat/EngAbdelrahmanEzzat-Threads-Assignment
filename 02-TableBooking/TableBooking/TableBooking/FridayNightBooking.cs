namespace TableBooking;

public sealed class FridayNightBooking
{
    public int TablesLeft { get; private set; } = 5;
    public int Booked { get; private set; }
    private readonly Lock _sync = new();
    public async Task BookAsync(string customerName)
    {
         await Task.Delay(20);
        lock(_sync)
        {
            if (TablesLeft <= 0)
            { 
                return;
            }
            TablesLeft--;
            Booked++;
            Console.WriteLine($"{customerName} booked a table. Left={TablesLeft}");
        }
       
    }
}
