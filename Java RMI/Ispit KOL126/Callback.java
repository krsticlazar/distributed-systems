import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Callback extends Remote {
    void priceChanged(int id, double newPrice) throws RemoteException;
}
