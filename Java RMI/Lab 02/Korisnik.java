import java.rmi.Remote;
import java.rmi.RemoteException;

/**
 * Udaljeni interfejs koji predstavlja jednog korisnika mobilnog operatera.
 *
 * Kada klijent od operatera dobije odgovarajuceg korisnika, nad njim poziva
 * metode za dopunu i za proveru stanja.
 */
public interface Korisnik extends Remote {
    /**
     * Dodaje minute korisniku i povecava racun po zadatoj tarifi.
     */
    void uplatiMinute(int minuti) throws RemoteException;

    /**
     * Dodaje poruke korisniku i povecava racun po zadatoj tarifi.
     */
    void uplatiPoruke(int poruke) throws RemoteException;

    /**
     * Dodaje internet korisniku i povecava racun po zadatoj tarifi.
     */
    void uplatiInternet(int internet) throws RemoteException;

    /**
     * Vraca trenutno stanje korisnika kao serijalizovan objekat.
     */
    Stanje vratiStanje() throws RemoteException;
}
