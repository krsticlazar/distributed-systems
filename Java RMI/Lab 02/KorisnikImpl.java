import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

/**
 * Serverska implementacija jednog korisnika.
 *
 * Svaki korisnik je poseban udaljeni objekat koji cuva svoje stanje i svoje
 * tarife. Kada klijent dobije referencu na korisnika, radi direktno nad ovim
 * objektom.
 */
public class KorisnikImpl extends UnicastRemoteObject implements Korisnik {
    // Broj telefona identifikuje korisnika u sistemu.
    private final String broj;
    // Trenutni preostali minuti.
    private int minuti;
    // Trenutni preostali broj poruka.
    private int poruke;
    // Trenutni internet u MB.
    private int internet;
    // Cena jednog dodatog minuta.
    private final int minutiTarifa;
    // Cena jedne dodatne poruke.
    private final int porukeTarifa;
    // Cena jednog dodatog MB interneta.
    private final int internetTarifa;
    // Ukupan racun koji se uvecava pri svakoj uplati.
    private float racun;

    /**
     * Konstruktor priprema korisnika sa pocetnim stanjem i odgovarajucim
     * tarifama.
     */
    public KorisnikImpl(
            String broj,
            int minuti,
            int poruke,
            int internet,
            int minutiTarifa,
            int porukeTarifa,
            int internetTarifa) throws RemoteException {
        super();
        this.broj = broj;
        this.minuti = minuti;
        this.poruke = poruke;
        this.internet = internet;
        this.minutiTarifa = minutiTarifa;
        this.porukeTarifa = porukeTarifa;
        this.internetTarifa = internetTarifa;
        this.racun = 0.0f;
    }

    @Override
    public synchronized void uplatiMinute(int minuti) throws RemoteException {
        // Zastita od besmislenog ili pogresnog unosa.
        if (minuti <= 0) {
            return;
        }

        // Azuriramo i raspolozive minute i racun korisnika.
        this.minuti += minuti;
        racun += minuti * minutiTarifa;
    }

    @Override
    public synchronized void uplatiPoruke(int poruke) throws RemoteException {
        if (poruke <= 0) {
            return;
        }

        // Logika je ista kao kod minuta, samo nad drugim resursom.
        this.poruke += poruke;
        racun += poruke * porukeTarifa;
    }

    @Override
    public synchronized void uplatiInternet(int internet) throws RemoteException {
        if (internet <= 0) {
            return;
        }

        // Dodajemo internet i odmah obracunavamo trosak po tarifi.
        this.internet += internet;
        racun += internet * internetTarifa;
    }

    @Override
    public synchronized Stanje vratiStanje() throws RemoteException {
        // Vracamo snapshot stanja, ne direktan pristup internim poljima.
        return new StanjeImpl(broj, minuti, poruke, internet, racun);
    }
}
