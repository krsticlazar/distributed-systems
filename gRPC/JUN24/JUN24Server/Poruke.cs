namespace JUN24Server;

public class Poruke
{
    public List<MessageItem> Lista { get; set; }
    public int SledeciId { get; set; }

    private static Poruke? instanca;

    private Poruke()
    {
        Lista = new List<MessageItem>();
        SledeciId = 1;
    }

    public static Poruke Instanca()
    {
        if (instanca == null)
        {
            instanca = new Poruke();
        }

        return instanca;
    }
}

