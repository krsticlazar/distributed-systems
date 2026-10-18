namespace KOL24Server;

public class Zadaci
{
    public List<TaskItem> Lista { get; set; }
    public int SledeciId { get; set; }

    private static Zadaci? instanca;

    private Zadaci()
    {
        Lista = new List<TaskItem>();
        SledeciId = 1;
    }

    public static Zadaci Instanca()
    {
        if (instanca == null)
        {
            instanca = new Zadaci();
        }

        return instanca;
    }
}

