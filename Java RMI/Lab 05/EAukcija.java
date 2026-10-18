import java.rmi.Remote;
import java.rmi.RemoteException;

public interface EAukcija extends Remote {                            // Glavni remote servis aukcije.
    Eksponat vratiEksponat(String idEksponata) throws RemoteException;   // Vraca remote referencu na trazeni eksponat.
}
