import java.rmi.Remote;
import java.rmi.RemoteException;

/**
 * Udaljeni interfejs koji predstavlja jednog korisnika eBank sistema.
 *
 * Preko ovog interfejsa klijent moze da:
 * - trazi stanje racuna
 * - prebaci novac sa dinarskog na devizni racun
 * - prebaci novac sa deviznog na dinarski racun
 */
public interface Korisnik extends Remote {
    /**
     * Vraca trenutno stanje oba racuna.
     */
    Stanje vratiStanje() throws RemoteException;

    /**
     * Skida novac sa dinarskog racuna i preracunava ga u devize.
     */
    void transferDinarskiNaDevizni(float iznos, float kurs) throws RemoteException;

    /**
     * Skida novac sa deviznog racuna i preracunava ga u dinare.
     */
    void transferDevizniNaDinarski(float iznos, float kurs) throws RemoteException;
}
