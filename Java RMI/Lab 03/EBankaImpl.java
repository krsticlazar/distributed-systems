import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.HashMap;
import java.util.Map;

/**
 * Serverska implementacija eBank sistema.
 *
 * Ova klasa glumi bazu korisnika i na osnovu JBK vraca odgovarajuci udaljeni
 * objekat tipa {@code Korisnik}.
 */
public class EBankaImpl extends UnicastRemoteObject implements EBanka {
    // Mapa svih poznatih korisnika.
    private final Map<String, Korisnik> korisnici;

    /**
     * Pri pokretanju servera formira se inicijalni skup korisnika.
     */
    public EBankaImpl() throws RemoteException {
        super();
        korisnici = new HashMap<>();
        inicijalizujKorisnike();
    }

    /**
     * U zadatku nema prave baze podataka, pa korisnike drzimo u memoriji.
     */
    private void inicijalizujKorisnike() throws RemoteException {
        dodajKorisnika("JBK123456", 10000.0f, 100.0f);
        dodajKorisnika("JBK223456", 12000.0f, 250.0f);
        dodajKorisnika("JBK323456", 8500.0f, 80.0f);
        dodajKorisnika("JBK423456", 15000.0f, 300.0f);
        dodajKorisnika("JBK523456", 9200.0f, 120.0f);
        dodajKorisnika("JBK623456", 11000.0f, 90.0f);
        dodajKorisnika("JBK723456", 13500.0f, 400.0f);
        dodajKorisnika("JBK823456", 7600.0f, 60.0f);
        dodajKorisnika("JBK923456", 14200.0f, 180.0f);
        dodajKorisnika("JBK103456", 9800.0f, 75.0f);
    }

    /**
     * Pomocna metoda koja smanjuje ponavljanje pri inicijalizaciji korisnika.
     */
    private void dodajKorisnika(String jbk, float iznosDinarski, float iznosDevizni) throws RemoteException {
        korisnici.put(jbk, new KorisnikImpl(jbk, iznosDinarski, iznosDevizni));
    }

    @Override
    public synchronized Korisnik vratiKorisnika(String jbk) throws RemoteException {
        // Ako klijent posalje nepoznat JBK, dobice null.
        return korisnici.get(jbk);
    }
}
