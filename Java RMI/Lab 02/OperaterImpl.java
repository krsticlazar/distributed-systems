import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.HashMap;
import java.util.Map;

/**
 * Serverska implementacija mobilnog operatera.
 *
 * Operater cuva mapu svih korisnika i po broju telefona vraca odgovarajuci
 * udaljeni objekat tipa {@code Korisnik}.
 */
public class OperaterImpl extends UnicastRemoteObject implements Operater {
    // Mapa omogucava brzo pronalazenje korisnika po broju telefona.
    private final Map<String, Korisnik> korisnici;

    /**
     * Konstruktor pravi praznu bazu i odmah je puni test korisnicima.
     */
    public OperaterImpl() throws RemoteException {
        super();
        korisnici = new HashMap<>();
        inicijalizujKorisnike();
    }

    /**
     * U malom laboratorijskom primeru "baza" je obicna mapa u memoriji.
     */
    private void inicijalizujKorisnike() throws RemoteException {
        dodajKorisnika("060123456", 100, 50, 1024, 5, 2, 1);
        dodajKorisnika("060223456", 120, 70, 2048, 4, 2, 1);
        dodajKorisnika("060323456", 200, 100, 3072, 4, 1, 1);
        dodajKorisnika("060423456", 80, 40, 1024, 6, 2, 2);
        dodajKorisnika("060523456", 150, 150, 4096, 3, 1, 1);
        dodajKorisnika("061123456", 90, 60, 1536, 5, 2, 1);
        dodajKorisnika("061223456", 110, 80, 2048, 5, 1, 1);
        dodajKorisnika("061323456", 130, 90, 1024, 4, 2, 2);
        dodajKorisnika("062123456", 160, 120, 5120, 3, 1, 1);
        dodajKorisnika("063123456", 70, 30, 512, 6, 3, 2);
    }

    /**
     * Pomocna metoda da inicijalizacija baze ostane citljiva.
     */
    private void dodajKorisnika(
            String broj,
            int minuti,
            int poruke,
            int internet,
            int minutiTarifa,
            int porukeTarifa,
            int internetTarifa) throws RemoteException {
        korisnici.put(
                broj,
                new KorisnikImpl(broj, minuti, poruke, internet, minutiTarifa, porukeTarifa, internetTarifa));
    }

    @Override
    public synchronized Korisnik vratiKorisnika(String broj) throws RemoteException {
        // Ako korisnik ne postoji, klijent ce dobiti null i sam ce ispisati poruku.
        return korisnici.get(broj);
    }
}
