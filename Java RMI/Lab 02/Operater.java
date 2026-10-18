import java.rmi.Remote;
import java.rmi.RemoteException;

/**
 * Glavni udaljeni interfejs aplikacije.
 *
 * Operater ima ulogu "ulazne tacke" u sistem. Klijent prvo dobija operatera,
 * a zatim preko njega trazi konkretnog korisnika po broju telefona.
 */
public interface Operater extends Remote {
    /**
     * Vraca udaljenu referencu na korisnika sa zadatim brojem telefona.
     */
    Korisnik vratiKorisnika(String broj) throws RemoteException;
}
