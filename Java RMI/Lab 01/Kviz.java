import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Kviz extends Remote {
    void pocetak() throws RemoteException;
    Pitanje vratiPitanje() throws RemoteException;
    void odgovori(String odg) throws RemoteException;
    int vratiBrojPoena() throws RemoteException;
}
