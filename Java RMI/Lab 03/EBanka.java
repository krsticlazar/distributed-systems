import java.rmi.Remote;
import java.rmi.RemoteException;

/**
 * Glavni udaljeni interfejs eBank sistema.
 *
 * Kao i kod mobilnog operatera, klijent prvo dobija ovaj glavni objekat, a
 * zatim preko njega trazi konkretnog korisnika po jedinstvenom broju.
 */
public interface EBanka extends Remote {
    /**
     * Vraca udaljenu referencu na korisnika sa zadatim JBK identifikatorom.
     */
    Korisnik vratiKorisnika(String jbk) throws RemoteException;
}
