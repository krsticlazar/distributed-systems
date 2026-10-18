import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Callback extends Remote {
    void resultChanged(int matchId) throws RemoteException;
}
