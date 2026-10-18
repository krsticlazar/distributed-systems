import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Glasanje extends Remote {
    boolean glasaj(String biracId, int kandidatId) throws RemoteException;
    Rezultat vratiRezultat(int kandidatId) throws RemoteException;
}
